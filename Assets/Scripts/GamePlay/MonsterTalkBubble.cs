// Assets/Scripts/GamePlay/MonsterTalkBubble.cs
//
// 戰鬥中怪物講話用的對話泡泡。
//
// 台詞寫在 MobData 的「台詞」欄位（開場／出手前／被打到／剩下不多／被打倒），
// 由 EnemyUnit 在對應時機呼叫 MonsterTalkBubble.Say()。
//
// UI 是程式當場搭的，不用改 CombatScene——戰鬥場景本來就沒有這個東西，
// 而且用程式搭的話，之後怪物換位置、換解析度都不用重拉。

using UnityEngine;
using UnityEngine.UI;
using Naninovel;

public class MonsterTalkBubble : MonoBehaviour
{
    const float MinSeconds = 2.2f;      // 再短的句子也至少留這麼久
    const float PerCharSeconds = 0.09f;
    const float MaxSeconds = 6f;

    static MonsterTalkBubble instance;

    RectTransform panel;
    Text label;
    CanvasGroup group;
    Transform anchor;          // 跟著誰跑（怪物）
    float hideAt;
    Font cachedFont;

    /// <summary>講一句。anchor 是要跟著的對象（怪物的 Transform）。</summary>
    public static void Say (Transform anchor, string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        if (instance == null)
        {
            // 不用 DontDestroyOnLoad：泡泡是戰鬥場景的東西，
            // 跟著場景一起消失比較乾淨，也不會有殘留的 Canvas 跟到別的場景去。
            var go = new GameObject("MonsterTalkBubble");
            instance = go.AddComponent<MonsterTalkBubble>();
            instance.Build();
        }
        instance.ShowLine(anchor, line);
    }

    /// <summary>戰鬥結束或離開場景時收掉，免得留在畫面上。</summary>
    public static void Hide ()
    {
        if (instance != null) instance.hideAt = 0f;
    }

    void Build ()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;      // 蓋在戰鬥 UI 上面，但低於聖典選人面板（6000）

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 泡泡本體
        var panelGO = new GameObject("Bubble", typeof(RectTransform), typeof(Image),
                                     typeof(ContentSizeFitter), typeof(HorizontalLayoutGroup),
                                     typeof(CanvasGroup));
        panelGO.transform.SetParent(canvasGO.transform, false);
        panel = panelGO.GetComponent<RectTransform>();
        panel.pivot = new Vector2(0.5f, 0f);          // 底部中央對齊怪物頭頂

        var bg = panelGO.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.11f, 0.88f);
        bg.raycastTarget = false;                      // 別擋到玩家點卡片

        var fitter = panelGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = panelGO.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 10, 12);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        group = panelGO.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textGO.transform.SetParent(panelGO.transform, false);
        label = textGO.GetComponent<Text>();
        label.font = GetFont();
        label.fontSize = 30;
        label.color = new Color(0.97f, 0.94f, 1f);
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.raycastTarget = false;

        var le = textGO.GetComponent<LayoutElement>();
        le.preferredWidth = 520;      // 太長就換行，不要拉成一條橫幅
    }

    void ShowLine (Transform who, string line)
    {
        anchor = who;
        label.text = line;
        hideAt = Time.time + Mathf.Clamp(MinSeconds + line.Length * PerCharSeconds,
                                         MinSeconds, MaxSeconds);
        Follow();     // 先擺好位置再淡入，不然會從上一隻怪的位置飄過來
    }

    void LateUpdate ()
    {
        var showing = Time.time < hideAt && anchor != null;
        group.alpha = Mathf.MoveTowards(group.alpha, showing ? 1f : 0f, Time.deltaTime * 5f);
        if (showing) Follow();
    }

    /// <summary>貼在怪物頭頂上方。用 sprite 的實際範圍，不是 transform 原點——
    /// 怪物的錨點在腳底，直接用原點泡泡會蓋在牠身上。</summary>
    void Follow ()
    {
        var cam = Camera.main;
        if (cam == null || anchor == null) return;

        var top = anchor.position;
        var rend = anchor.GetComponentInChildren<SpriteRenderer>();
        if (rend != null) top = new Vector3(rend.bounds.center.x, rend.bounds.max.y, rend.bounds.center.z);

        var screen = cam.WorldToScreenPoint(top);
        var canvasRect = (RectTransform)panel.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen, null, out var local);

        // 往上讓一點，並且夾在畫面裡，免得貼著邊緣的怪把泡泡推出去
        var half = canvasRect.rect.size * 0.5f;
        local.y += 24f;
        local.x = Mathf.Clamp(local.x, -half.x + 300f, half.x - 300f);
        local.y = Mathf.Clamp(local.y, -half.y + 60f, half.y - 140f);
        panel.anchoredPosition = local;
    }

    /// <summary>內建 Arial 沒有中文字，直接用會變豆腐。跟 AffinityChoicePanel 一樣借現成的。</summary>
    Font GetFont ()
    {
        if (cachedFont != null) return cachedFont;

        foreach (var anyText in Resources.FindObjectsOfTypeAll<Text>())
            if (anyText.font != null && anyText.gameObject.scene.IsValid())
                return cachedFont = anyText.font;

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

        return cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
