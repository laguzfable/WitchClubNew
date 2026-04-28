using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 放在 DemoMap 場景的任意 GameObject 上。
/// 顯示背景圖 + 4 個角色挑戰按鈕，不依賴地圖系統。
/// </summary>
public class DemoMapAutoProgress : MonoBehaviour
{
    [Header("背景圖（選填，留空則用純色）")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Color  backgroundColor = new Color(0.08f, 0.05f, 0.12f, 1f);

    // ── 4 角色設定 ────────────────────────────────────────────────

    readonly CharacterEntry[] characters = new CharacterEntry[]
    {
        new CharacterEntry(
            name:       "魅兒",
            sub:        "血系女巫",
            script:     "demo_mei",
            color:      new Color(0.80f, 0.18f, 0.18f, 1f),   // 深紅
            labelColor: new Color(1.00f, 0.55f, 0.55f, 1f)),

        new CharacterEntry(
            name:       "薇狄亞",
            sub:        "自然系女巫",
            script:     "demo_vivia",
            color:      new Color(0.15f, 0.55f, 0.25f, 1f),   // 森綠
            labelColor: new Color(0.55f, 1.00f, 0.65f, 1f)),

        new CharacterEntry(
            name:       "優菲",
            sub:        "象牙塔女巫",
            script:     "demo_euphie",
            color:      new Color(0.15f, 0.30f, 0.75f, 1f),   // 寶藍
            labelColor: new Color(0.60f, 0.80f, 1.00f, 1f)),

        new CharacterEntry(
            name:       "涅莉",
            sub:        "瓶中精靈",
            script:     "demo_nelly",
            color:      new Color(0.65f, 0.50f, 0.05f, 1f),   // 暗金
            labelColor: new Color(1.00f, 0.90f, 0.35f, 1f)),
    };

    // ════════════════════════════════════════════════════════════

    void Start() => BuildUI();

    void BuildUI()
    {
        // ── 根 Canvas ──
        var canvasGO = new GameObject("[Demo] MapSelect");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 全螢幕背景 ──
        var bgImg = MakeImage(canvasGO.transform, "Background",
            Vector2.zero, Vector2.one, backgroundColor);
        if (backgroundSprite != null)
        {
            bgImg.sprite = backgroundSprite;
            bgImg.type   = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }

        // ── 底部半透明深色面板 ──
        MakeImage(canvasGO.transform, "BG",
            new Vector2(0f, 0f), new Vector2(1f, 0.28f),
            new Color(0f, 0f, 0f, 0.80f));

        // ── 頂部金色分隔線 ──
        var line = MakeImage(canvasGO.transform, "Line",
            new Vector2(0f, 0.276f), new Vector2(1f, 0.281f),
            new Color(1f, 0.85f, 0.30f, 1f));

        // ── 標題文字 ──
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "◈  選擇挑戰對象  —  CHOOSE YOUR OPPONENT";
        title.fontSize  = 28;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = new Color(1f, 0.92f, 0.60f, 1f);
        Anchor(titleGO, new Vector2(0.1f, 0.215f), new Vector2(0.9f, 0.275f));

        // ── 4 個角色按鈕 ──
        float btnW = 0.20f;
        float gap  = 0.025f;
        float startX = (1f - (btnW * 4 + gap * 3)) / 2f;

        for (int i = 0; i < characters.Length; i++)
        {
            float x0 = startX + i * (btnW + gap);
            float x1 = x0 + btnW;
            BuildCharButton(canvasGO.transform, characters[i], x0, x1, i);
        }

        // ── Watermark ──
        var wm = new GameObject("Watermark");
        wm.transform.SetParent(canvasGO.transform, false);
        var wmCG = wm.AddComponent<CanvasGroup>();
        wmCG.alpha = 0.45f;
        wmCG.blocksRaycasts = false;
        var wmT = wm.AddComponent<TextMeshProUGUI>();
        wmT.text      = "HEXE  ·  Card Combat Demo";
        wmT.fontSize  = 20;
        wmT.alignment = TextAlignmentOptions.Right;
        wmT.color     = Color.white;
        Anchor(wm, new Vector2(0.72f, 0.93f), new Vector2(0.98f, 0.99f));
    }

    void BuildCharButton(Transform parent, CharacterEntry c, float x0, float x1, int index)
    {
        // 外框（元素色）
        var frame = new GameObject($"Frame_{c.name}");
        frame.transform.SetParent(parent, false);
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = c.color;
        Anchor(frame, new Vector2(x0, 0.02f), new Vector2(x1, 0.20f));

        // 按鈕本體（略暗）
        var btn = new GameObject($"Btn_{c.name}");
        btn.transform.SetParent(frame.transform, false);
        var btnImg = btn.AddComponent<Image>();
        btnImg.color = c.color * 0.65f;
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.04f, 0.04f);
        rt.anchorMax = new Vector2(0.96f, 0.96f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var button = btn.AddComponent<Button>();

        // 角色名
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(btn.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text      = c.name;
        nameTMP.fontSize  = 38;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color     = Color.white;
        Anchor(nameGO, new Vector2(0f, 0.50f), new Vector2(1f, 0.90f));

        // 副標題（元素種類）
        var subGO = new GameObject("Sub");
        subGO.transform.SetParent(btn.transform, false);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text      = c.sub;
        subTMP.fontSize  = 22;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = c.labelColor;
        Anchor(subGO, new Vector2(0f, 0.30f), new Vector2(1f, 0.52f));

        // 挑戰提示
        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(btn.transform, false);
        var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text      = "▶  挑戰";
        hintTMP.fontSize  = 20;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.color     = new Color(1f, 1f, 1f, 0.70f);
        Anchor(hintGO, new Vector2(0f, 0.05f), new Vector2(1f, 0.30f));

        // 按下事件
        var scriptName = c.script;
        button.onClick.AddListener(() => OnCharacterSelected(scriptName, frame));

        // 進場動畫（由下滑入）
        var frameRT = frame.GetComponent<RectTransform>();
        frameRT.anchoredPosition += new Vector2(0f, -60f);
        frameRT.DOAnchorPosY(frameRT.anchoredPosition.y + 60f, 0.4f)
               .SetDelay(index * 0.08f)
               .SetEase(Ease.OutBack);
    }

    void OnCharacterSelected(string scriptName, GameObject frame)
    {
        // 閃光反饋
        var frameImg = frame.GetComponent<Image>();
        if (frameImg != null)
            frameImg.DOColor(Color.white, 0.1f).OnComplete(() =>
                frameImg.DOColor(frameImg.color, 0.2f));

        var loader = SceneLoader.Instance;
        if (loader != null)
        {
            loader.GotoScript(scriptName);
        }
        else
        {
            Debug.LogError("[DemoMapAutoProgress] 找不到 SceneLoader.Instance！");
        }
    }

    // ── Utilities ────────────────────────────────────────────────

    static Image MakeImage(Transform parent, string name,
                           Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        Anchor(go, anchorMin, anchorMax);
        return img;
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Data ─────────────────────────────────────────────────────

    class CharacterEntry
    {
        public string name;
        public string sub;
        public string script;
        public Color  color;
        public Color  labelColor;

        public CharacterEntry(string name, string sub, string script,
                              Color color, Color labelColor)
        {
            this.name       = name;
            this.sub        = sub;
            this.script     = script;
            this.color      = color;
            this.labelColor = labelColor;
        }
    }
}
