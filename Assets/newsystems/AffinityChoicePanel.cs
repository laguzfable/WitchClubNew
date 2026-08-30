using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 一個角色選項。圖直接在 BranchMapUI 的 Inspector 上拖。
/// </summary>
[Serializable]
public class AffinityChoiceOption
{
    [Tooltip("顯示在立繪下方的名字。留空＝不顯示文字，只有圖")]
    public string displayName;

    [Tooltip("選到她時要設滿的好感度變數，例如 affinity_Eup")]
    public string variableName;

    [Tooltip("這一格要用的立繪。留空的話會用該角色的代表色畫一個色塊佔位，\n" +
             "所以圖還沒切好也能先測流程。")]
    public Sprite portrait;
}

/// <summary>
/// 蝕之聖典進節點前的「星塵開場 → 選女角 → 進節點」。
///
/// ★ 為什麼要有這個 ★
/// 聖典是給玩家收結局用的，可是好感不夠就做不了夜晚儀式（見 RitualGate），
/// 沒儀式就沒符文，沒符文就走不到分歧——等於要玩家把同一段養成再做一遍，
/// 跟聖典存在的理由互相矛盾。偷偷幫玩家把好感灌滿也不行：這款遊戲的玩家
/// 就是會去研究數值怎麼跑的人，暗改只會讓系統看起來在騙人。
/// 所以改成明著問，而且讓星塵來問——它本來就是操弄記憶與輪迴的那個角色
/// （見 yellow05），由它來重寫「這一輪你陪的是誰」，設定上站得住腳。
///
/// ★ UI 是程式生的，不是 prefab ★
/// 專案裡的 AffinityChangeNotifier／AffinityDebugOverlay 都是這個做法。
/// 好處是不用維護一份 prefab 的階層——你只要在 BranchMapUI 上把圖和台詞填進去。
/// 版面（大小、間距、位置）全部是下面那幾個常數，要調就改那裡。
/// </summary>
public class AffinityChoicePanel : MonoBehaviour
{
    // ── 版面 ──（以 1920x1080 為基準）
    const float CardHeight = 620f;    // 立繪高度，寬度依原圖比例算
    const float CardGap = 40f;        // 立繪之間的間距
    const float TitleY = 0.87f;       // 標題的垂直位置（畫面比例）
    const float HoverScale = 1.06f;   // 滑鼠移上去放大多少
    const float BoxHeight = 240f;     // 星塵對話框高度
    const float FadeTime = 0.35f;     // 立繪淡入時間
    const float PlaceholderRatio = 0.5f; // 還沒拉圖時的色塊寬高比

    /// <summary>還沒拉圖時的佔位色：照四條線的顏色，測起來一眼認得出誰是誰。</summary>
    static Color PlaceholderColor (string variableName)
    {
        switch (variableName)
        {
            case "affinity_Eup": return new Color(0.30f, 0.55f, 0.90f); // 藍
            case "affinity_Mel": return new Color(0.80f, 0.25f, 0.30f); // 紅
            case "affinity_Ved": return new Color(0.35f, 0.70f, 0.40f); // 綠
            case "affinity_Nel": return new Color(0.90f, 0.75f, 0.30f); // 黃
            case "affinity_Syb": return new Color(0.55f, 0.35f, 0.70f); // 紫（西碧兒沒有自己的線色）
            default: return new Color(0.5f, 0.5f, 0.5f);
        }
    }

    /// <summary>開這個面板要用的全部資料。參數太多了，包成一包比較好讀。</summary>
    public class Request
    {
        public IEnumerable<AffinityChoiceOption> Options;
        public string Title;
        public string SkipLabel;

        /// <summary>星塵要講的話，一句一行，點畫面推進。空的話直接跳到選人。</summary>
        public IEnumerable<string> Lines;

        /// <summary>對話框上的說話者名字。也是 Naninovel 模式下的角色 ID。</summary>
        public string SpeakerName;

        /// <summary>
        /// true＝把台詞交給 Naninovel 播（真的對話框、真的背景、能 Auto/Skip/記進回顧）。
        /// false＝用這支自己畫的簡易對話框（引擎沒起來時的退路）。
        /// </summary>
        public bool UseNaninovelDialogue;

        /// <summary>Naninovel 模式要切的背景 ID，例如 star1。留空＝不動背景。</summary>
        public string Background;

        /// <summary>Naninovel 模式要不要把角色叫出來（@char）。留空＝只有聲音沒有立繪。</summary>
        public string CharacterId;
    }

    static AffinityChoicePanel instance;

    Font cachedFont;
    Action<AffinityChoiceOption> onPicked;
    bool answered;

    RectTransform root;
    GameObject talkLayer;
    GameObject chooseLayer;
    Text talkText;
    List<string> lines;
    int lineIndex;

    /// <summary>
    /// 開面板。玩家選了角色（或按跳過）之後呼叫 callback，跳過時傳 null。
    /// 沒設圖的項目會用代表色的色塊佔位；一個選項都沒有時直接當跳過，不擋路。
    /// </summary>
    public static void Show (Request request, Action<AffinityChoiceOption> callback)
    {
        var usable = new List<AffinityChoiceOption>();
        if (request != null && request.Options != null)
            foreach (var o in request.Options)
                if (o != null && !string.IsNullOrEmpty(o.variableName))
                    usable.Add(o);

        // 沒有任何選項才放棄（那是設定錯了）。
        // 只是還沒拉圖的話照樣開得起來，立繪會用該角色的代表色當佔位色塊。
        if (usable.Count == 0)
        {
            Debug.Log("[AffinityChoicePanel] 一個選項都沒有，略過面板");
            if (callback != null) callback(null);
            return;
        }

        if (instance != null) Destroy(instance.gameObject);

        var go = new GameObject("AffinityChoicePanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AffinityChoicePanel>();
        instance.onPicked = callback;
        instance.Build(request, usable);
    }

    void Build (Request request, List<AffinityChoiceOption> options)
    {
        // ── Canvas（蓋在所有東西上面，含 Naninovel 的 UI）──
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        root = canvasGO.GetComponent<RectTransform>();

        BuildChooseLayer(request, options);

        lines = new List<string>();
        if (request != null && request.Lines != null)
            foreach (var l in request.Lines)
                if (!string.IsNullOrEmpty(l)) lines.Add(l);

        if (lines.Count == 0)
        {
            ShowChoices();
            return;
        }

        // 台詞交給 Naninovel：玩家看到的是遊戲本來的對話框、背景、字體，
        // Auto／Skip／回顧也都照常運作，跟主線是同一套體驗。
        if (request.UseNaninovelDialogue && Engine.Initialized)
        {
            chooseLayer.SetActive(false);
            PlayNaninovelLinesAsync(request).Forget();
            return;
        }

        // 退路：引擎還沒起來（或你刻意關掉）時，用自己畫的簡易對話框。
        chooseLayer.SetActive(false);
        BuildTalkLayer(request.SpeakerName);
        ShowLine(0);
    }

    /// <summary>選人那一層登場（含壓暗的底）。</summary>
    void ShowChoices ()
    {
        chooseLayer.SetActive(true);
        FadeIn().Forget();
    }

    // ============================================================
    //  第一段Ａ：交給 Naninovel 播
    // ============================================================

    /// <summary>
    /// 把台詞組成一小段劇本丟給引擎執行。用 ScriptPlaylist 而不是 IScriptPlayer，
    /// 是因為前者只執行這幾行、不會動到主線的播放位置（跟 Naninovel 內建的
    /// PlayScript 元件播 scriptText 是同一個做法）。
    /// </summary>
    async UniTaskVoid PlayNaninovelLinesAsync (Request request)
    {
        var speaker = string.IsNullOrEmpty(request.SpeakerName) ? "" : request.SpeakerName;
        var text = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(request.Background))
            text.AppendLine("@back " + request.Background + " time:0.5");

        if (!string.IsNullOrEmpty(request.CharacterId))
            text.AppendLine("@char " + request.CharacterId + " pos:50");

        foreach (var line in lines)
            text.AppendLine(string.IsNullOrEmpty(speaker) ? line : speaker + ": " + line);

        if (!string.IsNullOrEmpty(request.CharacterId))
            text.AppendLine("@hide " + request.CharacterId);

        try
        {
            var script = Script.FromScriptText("蝕之聖典 星塵開場", text.ToString());
            var playlist = new ScriptPlaylist(script);
            await PlayWaitingForInputAsync(playlist);
        }
        catch (Exception e)
        {
            // 播不出來也不能把玩家卡在這裡：直接跳到選人。
            Debug.LogWarning("[AffinityChoicePanel] Naninovel 台詞播放失敗，直接進選人：" + e.Message);
        }

        if (this == null) return; // 播到一半被關掉了
        ShowChoices();
    }

    /// <summary>
    /// 一句一句播，每句等玩家點過再繼續。
    ///
    /// 不能直接用 ScriptPlaylist.ExecuteAsync()：@print 的「等玩家點」其實只是
    /// 呼叫 ScriptPlayer.SetWaitingForInputEnabled(true) 把旗標打開，真正在等的是
    /// ScriptPlayer 的播放迴圈。我們沒有跑那個迴圈，所以整段會一口氣衝到底。
    /// 這裡就是把那段等待補回來。
    /// </summary>
    async UniTask PlayWaitingForInputAsync (ScriptPlaylist playlist)
    {
        var player = Engine.GetService<IScriptPlayer>();
        var config = Engine.GetConfiguration<ScriptPlayerConfiguration>();

        foreach (var command in playlist)
        {
            if (!command.ShouldExecute) continue;

            if (config.ShouldWait(command)) await command.ExecuteAsync();
            else command.ExecuteAsync().Forget();

            // 跳過模式下 Naninovel 根本不會把旗標打開，所以這個迴圈自然不會卡住。
            while (player != null && player.WaitingForInput)
            {
                if (this == null) return;   // 面板被關掉就別再等了
                await UniTask.Yield();
            }
        }
    }

    // ============================================================
    //  第一段：星塵講話
    // ============================================================

    void BuildTalkLayer (string speakerName)
    {
        talkLayer = new GameObject("Talk", typeof(RectTransform));
        talkLayer.transform.SetParent(root, false);
        Stretch(talkLayer.GetComponent<RectTransform>());

        // 整面都能點，點一下推進一句。這裡不做打字機效果——
        // 玩家已經看過主線了，這段是儀式感，不是要他慢慢讀。
        var catcher = NewImage("ClickCatcher", talkLayer.GetComponent<RectTransform>());
        catcher.color = new Color(0f, 0f, 0f, 0.82f);
        Stretch(catcher.rectTransform);
        var advance = catcher.gameObject.AddComponent<Button>();
        advance.targetGraphic = catcher;
        advance.transition = Selectable.Transition.None;
        advance.onClick.AddListener(delegate { NextLine(); });

        var box = NewImage("Box", talkLayer.GetComponent<RectTransform>());
        box.color = new Color(0.05f, 0.03f, 0.09f, 0.88f);
        box.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        box.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        box.rectTransform.sizeDelta = new Vector2(1500, BoxHeight);
        box.rectTransform.anchoredPosition = new Vector2(0f, BoxHeight / 2f + 90f);
        box.raycastTarget = false;

        if (!string.IsNullOrEmpty(speakerName))
        {
            var name = NewText("Speaker", box.rectTransform, speakerName, 32);
            name.color = new Color(0.75f, 0.85f, 1f);
            name.alignment = TextAnchor.MiddleLeft;
            name.rectTransform.anchorMin = new Vector2(0f, 1f);
            name.rectTransform.anchorMax = new Vector2(0f, 1f);
            name.rectTransform.pivot = new Vector2(0f, 1f);
            name.rectTransform.sizeDelta = new Vector2(500, 50);
            name.rectTransform.anchoredPosition = new Vector2(48f, -26f);
        }

        talkText = NewText("Line", box.rectTransform, "", 34);
        talkText.alignment = TextAnchor.UpperLeft;
        talkText.horizontalOverflow = HorizontalWrapMode.Wrap;
        Stretch(talkText.rectTransform);
        talkText.rectTransform.offsetMin = new Vector2(48f, 40f);
        talkText.rectTransform.offsetMax = new Vector2(-48f, -84f);

        var hint = NewText("Hint", talkLayer.GetComponent<RectTransform>(), "▼", 26);
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        hint.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint.rectTransform.sizeDelta = new Vector2(60, 40);
        hint.rectTransform.anchoredPosition = new Vector2(700f, 70f);
    }

    void ShowLine (int index)
    {
        lineIndex = index;
        if (talkText != null) talkText.text = lines[index];
    }

    void NextLine ()
    {
        if (lineIndex + 1 < lines.Count)
        {
            ShowLine(lineIndex + 1);
            return;
        }

        // 講完了 → 換成選人
        if (talkLayer != null) Destroy(talkLayer);
        ShowChoices();
    }

    async UniTaskVoid FadeIn ()
    {
        var group = chooseLayer.GetComponent<CanvasGroup>();
        if (group == null) return;

        var t = 0f;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / FadeTime);
            await UniTask.Yield();
        }
        group.alpha = 1f;
    }

    // ============================================================
    //  第二段：選女角
    // ============================================================

    void BuildChooseLayer (Request request, List<AffinityChoiceOption> options)
    {
        chooseLayer = new GameObject("Choose", typeof(RectTransform), typeof(CanvasGroup));
        chooseLayer.transform.SetParent(root, false);
        Stretch(chooseLayer.GetComponent<RectTransform>());
        var layer = chooseLayer.GetComponent<RectTransform>();

        // 壓暗的底掛在這一層底下，才不會在星塵講話時擋住 Naninovel 的對話框。
        var dim = NewImage("Dim", layer);
        dim.color = new Color(0f, 0f, 0f, 0.82f);
        Stretch(dim.rectTransform);

        var title = request != null ? request.Title : null;
        if (!string.IsNullOrEmpty(title))
        {
            var titleText = NewText("Title", layer, title, 44);
            titleText.rectTransform.anchorMin = new Vector2(0.5f, TitleY);
            titleText.rectTransform.anchorMax = new Vector2(0.5f, TitleY);
            titleText.rectTransform.sizeDelta = new Vector2(1600, 90);
            titleText.rectTransform.anchoredPosition = Vector2.zero;
        }

        // 先算每張的寬度（依原圖比例），才知道整排多寬、要從哪裡開始排。
        var widths = new float[options.Count];
        var total = 0f;
        for (var i = 0; i < options.Count; i++)
        {
            var portrait = options[i].portrait;
            if (portrait != null && portrait.rect.height > 0f)
                widths[i] = CardHeight * (portrait.rect.width / portrait.rect.height);
            else
                widths[i] = CardHeight * PlaceholderRatio; // 還沒拉圖：用你那張圖的框比例
            total += widths[i];
        }
        total += CardGap * (options.Count - 1);

        var x = -total / 2f;
        for (var i = 0; i < options.Count; i++)
        {
            CreateCard(layer, options[i], x + widths[i] / 2f, widths[i]);
            x += widths[i] + CardGap;
        }

        // 留一條退路：玩家可能只想看這一格原本的樣子，不想動好感。
        var skipLabel = request != null ? request.SkipLabel : null;
        if (!string.IsNullOrEmpty(skipLabel))
        {
            var skip = NewText("Skip", layer, skipLabel, 28);
            skip.color = new Color(1f, 1f, 1f, 0.65f);
            skip.rectTransform.anchorMin = new Vector2(0.5f, 0.08f);
            skip.rectTransform.anchorMax = new Vector2(0.5f, 0.08f);
            skip.rectTransform.sizeDelta = new Vector2(600, 60);
            skip.rectTransform.anchoredPosition = Vector2.zero;

            var button = skip.gameObject.AddComponent<Button>();
            button.targetGraphic = skip;
            button.onClick.AddListener(delegate { Pick(null); });
        }
    }

    void CreateCard (RectTransform parent, AffinityChoiceOption option, float centerX, float width)
    {
        var image = NewImage("Card_" + option.variableName, parent);
        if (option.portrait != null)
        {
            image.sprite = option.portrait;
            image.preserveAspect = true;
        }
        else
        {
            // 佔位：純色塊 + 名字。圖拉進來之後這段就再也不會跑到。
            image.color = PlaceholderColor(option.variableName);
        }

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, CardHeight);
        rect.anchoredPosition = new Vector2(centerX, -30f);

        var picked = option;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(delegate { Pick(picked); });

        // 滑鼠移上去放大一點。用 EventTrigger 比自己實作 IPointerEnterHandler 省事，
        // 這個面板也不需要鍵盤／手把導航。
        var trigger = image.gameObject.AddComponent<EventTrigger>();
        AddHover(trigger, EventTriggerType.PointerEnter, delegate { rect.localScale = Vector3.one * HoverScale; });
        AddHover(trigger, EventTriggerType.PointerExit, delegate { rect.localScale = Vector3.one; });

        // 有圖時 displayName 留空＝只顯示圖；沒圖時一定要有字，不然認不出是誰。
        var caption = option.displayName;
        if (string.IsNullOrEmpty(caption))
        {
            if (option.portrait != null) return;
            caption = option.variableName;
        }

        var label = NewText("Name_" + option.variableName, parent, caption, 30);
        label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(width + CardGap, 50);
        label.rectTransform.anchoredPosition = new Vector2(centerX, -30f - CardHeight / 2f - 34f);
    }

    static void AddHover (EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(delegate { action(); });
        trigger.triggers.Add(entry);
    }

    void Pick (AffinityChoiceOption option)
    {
        if (answered) return; // 連點兩下不要送兩次
        answered = true;

        Debug.Log("[AffinityChoicePanel] 玩家選了：" +
                  (option == null ? "跳過" : option.displayName + " / " + option.variableName));

        var callback = onPicked;
        onPicked = null;

        if (instance == this) instance = null;
        Destroy(gameObject);

        if (callback != null) callback(option);
    }

    // ============================================================
    //  UI 小工具
    // ============================================================

    static void Stretch (RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Image NewImage (string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    Text NewText (string name, RectTransform parent, string content, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.font = GetFont();
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.text = content;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    /// <summary>
    /// 字型的來源跟 AffinityChangeNotifier 一樣：借對話框正在用的 Font。
    /// 內建 Arial 沒有中文字，直接用會變成豆腐。
    /// </summary>
    Font GetFont ()
    {
        if (cachedFont != null) return cachedFont;

        if (Engine.Initialized)
        {
            var printerManager = Engine.GetService<ITextPrinterManager>();
            if (printerManager != null && !string.IsNullOrEmpty(printerManager.DefaultPrinterId) &&
                printerManager.ActorExists(printerManager.DefaultPrinterId))
            {
                var actor = printerManager.GetActor(printerManager.DefaultPrinterId) as Component;
                var liveText = actor != null ? actor.GetComponentInChildren<Text>(true) : null;
                if (liveText != null && liveText.font != null)
                    return cachedFont = liveText.font;
            }
        }

        foreach (var anyText in Resources.FindObjectsOfTypeAll<Text>())
            if (anyText.font != null && anyText.gameObject.scene.IsValid())
                return cachedFont = anyText.font;

        return cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
