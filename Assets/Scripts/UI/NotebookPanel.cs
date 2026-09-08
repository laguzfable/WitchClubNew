// Assets/Scripts/UI/NotebookPanel.cs
//
// 筆記本：玩家一路收集到的世界觀條目，分類瀏覽，沒解鎖的顯示成鎖著。
//
// ★ 資料放哪 ★
// 條目本文在 Assets/Naninovel/Resources/Naninovel/Text/Notes.txt，
// 走 Naninovel 的 managed text（跟 CharacterNames.txt 同一套）。好處有三：
//   ‧ Text 資料夾底下的文件是整包載入的，不用像腳本那樣註冊進 EditorResources
//   ‧ 翻譯直接放 Localization/en/Text/Notes.txt，跟現有流程一致
//   ‧ 內容改了不用重編程式
// 一行一條，格式 `代號: 標題|分類|內文`，內文裡的 <br> 會換行。
//
// ★ 解鎖狀態放哪 ★
// 用 Naninovel 的 IUnlockableManager，id 是 "Notes/代號"——跟 @unlock CG/cg01
// 完全同一套，所以存檔、跨周目保留、全域範圍那些都免費得到，不用自己碰 PlayerPrefs。
//
// ★ UI 是程式搭的 ★
// 比照 SceneControlBar／AffinityChoicePanel／MonsterTalkBubble：執行時當場生，
// 不用維護 prefab，換解析度也不用重拉。版面全在下面那幾個常數裡。

using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NotebookPanel : MonoBehaviour
{
    /// <summary>Notes.txt 的文件名，也是 managed text 的分類名。</summary>
    public const string Category = "Notes";
    /// <summary>解鎖 id 的前綴。劇本裡寫 @note id:xxx 實際解的是 "Notes/xxx"。</summary>
    public const string UnlockPrefix = "Notes/";

    // ── 版面（以 1920x1080 為基準）──
    const float PanelWidth = 1360f;
    const float PanelHeight = 780f;
    const float HeaderHeight = 56f;
    const float TabHeight = 56f;
    const float TabGap = 16f;
    const float ListWidth = 420f;
    const float RowHeight = 52f;
    const float Pad = 28f;

    // 內容區（列表與右頁）的起點與高度。所有子元件都從這兩個數字算位置，
    // 免得各自加加減減，改一個間距就要追四個地方。
    const float ContentTop = Pad + HeaderHeight + TabHeight + TabGap;
    const float ContentHeight = PanelHeight - ContentTop - Pad;
    const float ReaderLeft = Pad + ListWidth + 24f;
    const float ReaderWidth = PanelWidth - ReaderLeft - Pad;

    static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.82f);
    static readonly Color Paper = new Color(0.09f, 0.08f, 0.12f, 0.98f);
    static readonly Color Line = new Color(1f, 1f, 1f, 0.13f);
    static readonly Color Ink = new Color(0.92f, 0.90f, 0.96f);
    static readonly Color Dim = new Color(0.55f, 0.53f, 0.62f);
    static readonly Color Accent = new Color(0.73f, 0.55f, 0.91f);

    class Entry
    {
        public string Id;
        public string Title;
        public string Category;
        public string Body;
        public bool Unlocked;
    }

    static NotebookPanel instance;

    List<Entry> entries = new List<Entry>();
    List<string> categories = new List<string>();
    string activeCategory;
    Entry selected;

    Font font;
    RectTransform listContent;
    Text titleLabel, categoryLabel, bodyLabel, counterLabel;
    Transform tabRow;

    /// <summary>開啟筆記本。重複呼叫是安全的（已經開著就不重開）。</summary>
    public static void Open ()
    {
        if (instance != null) return;

        // 沒有 EventSystem 的場景按鈕會沒反應，跟 SceneControlBar 一樣順手補一個
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var go = new GameObject("NotebookPanel");
        instance = go.AddComponent<NotebookPanel>();
        instance.Build();
    }

    public static void Close ()
    {
        if (instance == null) return;
        Destroy(instance.gameObject);
        instance = null;
    }

    /// <summary>有沒有開著。給控制列決定按鈕要開還是關。</summary>
    public static bool IsOpen => instance != null;

    // ── 資料 ──────────────────────────────────────────────

    void LoadEntries ()
    {
        entries.Clear();

        var textManager = Engine.Initialized ? Engine.GetService<ITextManager>() : null;
        if (textManager == null)
        {
            Debug.LogWarning("[Notebook] 引擎還沒起來，讀不到 Notes.txt。");
            return;
        }

        var unlockables = Engine.GetService<IUnlockableManager>();

        foreach (var record in textManager.GetAllRecords(Category))
        {
            // 「標題|分類|內文」。少寫欄位不當錯誤處理，缺的用預設值補上，
            // 免得寫錯一行就整本筆記本開不起來。
            var parts = (record.Value ?? string.Empty).Split('|');
            var entry = new Entry
            {
                Id = record.Key,
                Title = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0].Trim() : record.Key,
                Category = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "未分類",
                Body = parts.Length > 2 ? parts[2].Replace("<br>", "\n").Trim() : string.Empty,
            };
            entry.Unlocked = unlockables?.ItemUnlocked(UnlockPrefix + entry.Id) ?? false;
            entries.Add(entry);
        }

        // 分類照第一次出現的順序排，這樣作者在 Notes.txt 裡的排法就是玩家看到的排法
        categories = entries.Select(e => e.Category).Distinct().ToList();
        if (activeCategory == null || !categories.Contains(activeCategory))
            activeCategory = categories.FirstOrDefault();
    }

    // ── 版面 ──────────────────────────────────────────────

    void Build ()
    {
        font = ResolveFont();
        NewItemTracker.BeginVisit(NewItemTracker.Notes);
        LoadEntries();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;              // 蓋過控制列（900），低於好感度除錯（999）
        // 一定要設成 ScaleWithScreenSize：預設的 ConstantPixelSize 是 1:1 像素，
        // 視窗比 1500x860 小的時候面板會整個超出畫面（左邊和上面被切掉）。
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // 背景：點空白處關掉
        var back = NewImage(transform, "Backdrop", Backdrop);
        Stretch(back.rectTransform);
        back.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        var panel = NewImage(transform, "Paper", Paper);
        var pr = panel.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        pr.anchoredPosition = Vector2.zero;
        // 面板本身也要吃掉點擊，否則點在紙上會穿過去關掉整本
        panel.gameObject.AddComponent<Button>().onClick.AddListener(() => { });

        BuildHeader(pr);
        BuildTabs(pr);
        BuildList(pr);
        BuildReader(pr);

        FitToScreen(pr);

        RefreshTabs();
        RefreshList();
        ShowEntry(null);
    }

    /// <summary>
    /// CanvasScaler 已經照 1920x1080 縮過一次，但視窗比例跟基準差太多時
    /// （例如很扁的視窗）面板還是會超出邊界。這裡再照畫布實際大小等比縮一次，
    /// 內部版面的座標完全不用動——縮的是整張紙。
    /// </summary>
    static void FitToScreen (RectTransform panel)
    {
        var canvasRect = panel.parent as RectTransform;
        if (canvasRect == null) return;

        var available = canvasRect.rect.size - new Vector2(Pad * 2f, Pad * 2f);
        if (available.x <= 0f || available.y <= 0f) return;

        var scale = Mathf.Min(1f, available.x / PanelWidth, available.y / PanelHeight);
        panel.localScale = new Vector3(scale, scale, 1f);
    }

    void BuildHeader (RectTransform parent)
    {
        var title = NewText(parent, "Header", "筆記本", 34, Ink, TextAnchor.MiddleLeft);
        var r = title.rectTransform;
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(400f, HeaderHeight);
        r.anchoredPosition = new Vector2(Pad, -Pad);

        counterLabel = NewText(parent, "Counter", string.Empty, 22, Dim, TextAnchor.MiddleRight);
        var cr = counterLabel.rectTransform;
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1f, 1f);
        cr.sizeDelta = new Vector2(360f, 48f);
        cr.anchoredPosition = new Vector2(-Pad - 70f, -Pad);

        var close = NewText(parent, "Close", "×", 40, Dim, TextAnchor.MiddleCenter);
        var xr = close.rectTransform;
        xr.anchorMin = xr.anchorMax = xr.pivot = new Vector2(1f, 1f);
        xr.sizeDelta = new Vector2(56f, 56f);
        xr.anchoredPosition = new Vector2(-Pad + 8f, -Pad + 4f);
        var closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.onClick.AddListener(Close);
    }

    void BuildTabs (RectTransform parent)
    {
        var row = new GameObject("Tabs", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var r = (RectTransform)row.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(PanelWidth - Pad * 2f, TabHeight);
        r.anchoredPosition = new Vector2(Pad, -(Pad + HeaderHeight));

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleLeft;

        tabRow = row.transform;
    }

    void BuildList (RectTransform parent)
    {
        var viewGO = NewImage(parent, "ListView", new Color(1f, 1f, 1f, 0.03f));
        var vr = viewGO.rectTransform;
        vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0f, 1f);
        vr.sizeDelta = new Vector2(ListWidth, ContentHeight);
        vr.anchoredPosition = new Vector2(Pad, -ContentTop);

        var scroll = viewGO.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        viewGO.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewGO.transform, false);
        listContent = (RectTransform)content.transform;
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 2f;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = listContent;
        scroll.viewport = vr;
    }

    void BuildReader (RectTransform parent)
    {
        categoryLabel = NewText(parent, "ReaderCategory", string.Empty, 20, Accent, TextAnchor.UpperLeft);
        PlaceInReader(categoryLabel.rectTransform, 0f, 28f);

        titleLabel = NewText(parent, "ReaderTitle", string.Empty, 32, Ink, TextAnchor.UpperLeft);
        PlaceInReader(titleLabel.rectTransform, 32f, 46f);

        bodyLabel = NewText(parent, "ReaderBody", string.Empty, 24, Ink, TextAnchor.UpperLeft);
        PlaceInReader(bodyLabel.rectTransform, 88f, ContentHeight - 88f);
        bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyLabel.verticalOverflow = VerticalWrapMode.Truncate;
        bodyLabel.lineSpacing = 1.35f;
    }

    /// <summary>右頁的元件一律錨在面板左上角，往下數 offsetY。</summary>
    static void PlaceInReader (RectTransform r, float offsetY, float height)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(ReaderWidth, height);
        r.anchoredPosition = new Vector2(ReaderLeft, -(ContentTop + offsetY));
    }

    // ── 內容 ──────────────────────────────────────────────

    void RefreshTabs ()
    {
        foreach (Transform child in tabRow) Destroy(child.gameObject);

        foreach (var category in categories)
        {
            var captured = category;
            var on = category == activeCategory;

            var go = NewImage(tabRow, "Tab_" + category,
                on ? new Color(0.73f, 0.55f, 0.91f, 0.18f) : new Color(1f, 1f, 1f, 0.05f));
            var le = go.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 150f;

            var label = NewText(go.rectTransform, "Label", category, 24, on ? Accent : Dim, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);

            go.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                activeCategory = captured;
                RefreshTabs();
                RefreshList();
                ShowEntry(null);
            });
        }
    }

    void RefreshList ()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        var shown = entries.Where(e => e.Category == activeCategory).ToList();

        foreach (var entry in shown)
        {
            var captured = entry;

            var row = NewImage(listContent, "Row_" + entry.Id, new Color(1f, 1f, 1f, 0.02f));
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;

            // 沒解鎖只留一排橫線，標題不顯示——標題本身就會爆雷
            var text = entry.Unlocked ? entry.Title : "－－－－";
            var label = NewText(row.rectTransform, "Label", text, 22,
                entry.Unlocked ? Ink : Dim, TextAnchor.MiddleLeft);
            var lr = label.rectTransform;
            Stretch(lr);
            lr.offsetMin = new Vector2(16f, 0f);
            lr.offsetMax = new Vector2(-64f, 0f);   // 右邊讓出 NEW 的位置

            // 已經拿到、但還沒點開過的標 NEW。點開之後就不會再標（見 ShowEntry）。
            if (entry.Unlocked && NewItemTracker.IsNewThisVisit(NewItemTracker.Notes, entry.Id))
            {
                var badge = NewText(row.rectTransform, "New", "NEW", 16, Accent, TextAnchor.MiddleRight);
                var nr = badge.rectTransform;
                nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(1f, 0.5f);
                nr.sizeDelta = new Vector2(52f, RowHeight);
                nr.anchoredPosition = new Vector2(-12f, 0f);
            }

            var button = row.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => ShowEntry(captured));
        }

        var unlocked = entries.Count(e => e.Unlocked);
        counterLabel.text = $"{unlocked} / {entries.Count}";
    }

    void ShowEntry (Entry entry)
    {
        selected = entry;

        if (entry == null)
        {
            categoryLabel.text = string.Empty;
            titleLabel.text = string.Empty;
            bodyLabel.text = "從左邊選一條。";
            bodyLabel.color = Dim;
            return;
        }

        categoryLabel.text = entry.Category;
        bodyLabel.color = Ink;

        if (!entry.Unlocked)
        {
            titleLabel.text = "？？？";
            bodyLabel.text = "這一頁還是空的。";
            bodyLabel.color = Dim;
            return;
        }

        titleLabel.text = entry.Title;
        bodyLabel.text = entry.Body;

    }

    // ── 小工具 ────────────────────────────────────────────

    static void Stretch (RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    Image NewImage (Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Text NewText (Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    /// <summary>
    /// 內建 Arial 沒有中日文字會變方框，所以優先找場景裡現成的字型
    /// （專案已統一成 NotoSansCJKtc）。作法跟 SceneControlBar.ResolveFont 一致。
    /// </summary>
    static Font ResolveFont ()
    {
        var fromScene = FindObjectsOfType<Text>()
            .Select(t => t.font)
            .FirstOrDefault(f => f != null && f.name.Contains("Noto"));
        if (fromScene != null) return fromScene;

        return Resources.GetBuiltinResource<Font>("Arial.ttf")
               ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
    }
}
