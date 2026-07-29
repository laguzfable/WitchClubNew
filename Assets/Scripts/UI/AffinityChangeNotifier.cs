using System;
using System.Collections.Generic;
using System.Globalization;
using Naninovel;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 監聽所有 affinity_XXX 好感度變數的變化，選項按下後好感度一旦真的改變，
/// 就在畫面上跳出一行提示（例如「薇狄亞 好感度 +15」）。
/// 不需要修改任何 .nani 劇本或場景/prefab，開場自動掛載。
/// </summary>
public class AffinityChangeNotifier : MonoBehaviour
{
    const string VariablePrefix = "affinity_";

    static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Mel", "魅兒" },
        { "Ved", "薇狄亞" },
        { "Eup", "優菲" },
        { "Nel", "涅莉" },
        { "Nelly", "涅莉" },
        { "Syb", "西碧兒" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<AffinityChangeNotifier>() != null) return;
        var go = new GameObject(nameof(AffinityChangeNotifier));
        DontDestroyOnLoad(go);
        go.AddComponent<AffinityChangeNotifier>();
    }

    RectTransform toastRoot;
    Font cachedFont;
    readonly Queue<string> pendingMessages = new Queue<string>();
    bool showingToasts;

    void Awake() => WaitForEngineAndSubscribe().Forget();

    async UniTaskVoid WaitForEngineAndSubscribe()
    {
        while (!Engine.Initialized)
            await UniTask.Yield();

        var variableManager = Engine.GetService<ICustomVariableManager>();
        if (variableManager == null) return;
        variableManager.OnVariableUpdated += HandleVariableUpdated;
    }

    void OnDestroy()
    {
        if (!Engine.Initialized) return;
        var variableManager = Engine.GetService<ICustomVariableManager>();
        if (variableManager != null) variableManager.OnVariableUpdated -= HandleVariableUpdated;
    }

    void HandleVariableUpdated(CustomVariableUpdatedArgs args)
    {
        if (string.IsNullOrEmpty(args.Name) || !args.Name.StartsWith(VariablePrefix, StringComparison.OrdinalIgnoreCase)) return;
        // 變數第一次被建立（例如章節開頭 @set affinity_Ved=0）不算好感度變化。
        if (string.IsNullOrEmpty(args.InitialValue)) return;
        if (!float.TryParse(args.InitialValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var oldValue)) return;
        if (!float.TryParse(args.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var newValue)) return;

        var delta = newValue - oldValue;
        if (Mathf.Approximately(delta, 0f)) return;

        var key = args.Name.Substring(VariablePrefix.Length);
        var displayName = DisplayNames.TryGetValue(key, out var mapped) ? mapped : key;
        var deltaText = delta > 0 ? $"+{FormatNumber(delta)}" : FormatNumber(delta);
        Enqueue($"{displayName} 好感度 {deltaText}");
    }

    static string FormatNumber(float value) =>
        Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#", CultureInfo.InvariantCulture);

    void Enqueue(string message)
    {
        pendingMessages.Enqueue(message);
        if (!showingToasts) ProcessQueue().Forget();
    }

    async UniTaskVoid ProcessQueue()
    {
        showingToasts = true;
        while (pendingMessages.Count > 0)
            await ShowToast(pendingMessages.Dequeue());
        showingToasts = false;
    }

    async UniTask ShowToast(string message)
    {
        EnsureCanvas();

        var go = new GameObject("AffinityToast", typeof(RectTransform));
        go.transform.SetParent(toastRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.82f);
        rect.sizeDelta = new Vector2(900, 90);
        rect.anchoredPosition = Vector2.zero;

        var text = go.AddComponent<Text>();
        text.font = GetFont();
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(1f, 0.85f, 0.92f, 0f);
        text.text = message;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = new Vector2(2f, -2f);

        const float fadeIn = 0.2f, hold = 1.1f, fadeOut = 0.4f, rise = 30f;
        var startPos = rect.anchoredPosition;
        var baseColor = text.color;

        var t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            var a = Mathf.Clamp01(t / fadeIn);
            text.color = SetAlpha(baseColor, a);
            outline.effectColor = new Color(0f, 0f, 0f, a * 0.8f);
            await UniTask.Yield();
        }
        text.color = SetAlpha(baseColor, 1f);
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);

        await UniTask.Delay(TimeSpan.FromSeconds(hold));

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            var p = t / fadeOut;
            text.color = SetAlpha(baseColor, 1f - p);
            outline.effectColor = new Color(0f, 0f, 0f, (1f - p) * 0.8f);
            rect.anchoredPosition = startPos + new Vector2(0f, rise * p);
            await UniTask.Yield();
        }

        Destroy(go);
    }

    static Color SetAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    void EnsureCanvas()
    {
        if (toastRoot != null) return;

        var go = new GameObject("AffinityToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        toastRoot = go.GetComponent<RectTransform>();
    }

    Font GetFont()
    {
        if (cachedFont != null) return cachedFont;

        // 遊戲對話框本身用的是舊版 UI.Text（不是 TMP），在 Windows 上會靠系統字型連結
        // 自動補中文字，所以這裡直接抓對話框正在用的 Font，而不是自己另外指定字型。
        if (Engine.Initialized)
        {
            var printerManager = Engine.GetService<ITextPrinterManager>();
            if (printerManager != null && !string.IsNullOrEmpty(printerManager.DefaultPrinterId) &&
                printerManager.ActorExists(printerManager.DefaultPrinterId))
            {
                var actor = printerManager.GetActor(printerManager.DefaultPrinterId) as Component;
                var liveText = actor != null ? actor.GetComponentInChildren<Text>(true) : null;
                if (liveText != null && liveText.font != null)
                {
                    cachedFont = liveText.font;
                    return cachedFont;
                }
            }
        }

        foreach (var anyText in Resources.FindObjectsOfTypeAll<Text>())
            if (anyText.font != null && anyText.gameObject.scene.IsValid())
            {
                cachedFont = anyText.font;
                return cachedFont;
            }

        cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return cachedFont;
    }
}
