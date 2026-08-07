using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 除錯用：在畫面角落顯示各角色目前好感度數值。
/// 掛在場景任意 GameObject 的 Start()/Awake() 呼叫 AffinityDebugOverlay.Show()。
/// </summary>
public static class AffinityDebugOverlay
{
    const string RootName = "AffinityDebugOverlay";

    static readonly (string varName, string label)[] Affinities =
    {
        ("affinity_Eup", "優菲"),
        ("affinity_Nel", "涅莉"),
        ("affinity_Ved", "薇狄亞"),
        ("affinity_Mel", "梅爾"),
        ("affinity_Syb", "西碧兒"),
    };

    public static void Show ()
    {
        // 已經建立過就只更新數字，不重複建立
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            var existingText = existing.GetComponentInChildren<Text>();
            if (existingText != null) existingText.text = BuildText();
            return;
        }

        // 不用 DontDestroyOnLoad：只在目前這個場景顯示，離開場景就自動清掉
        var canvasGO = new GameObject(RootName);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("AffinityText");
        textGO.transform.SetParent(canvasGO.transform, false);

        var text = textGO.AddComponent<Text>();
        // Unity 2019 沒有 LegacyRuntime.ttf（那是 2021.2+ 才有的內建字型名），
        // 這裡用 Arial.ttf，找不到的話再退回專案裡隨便一個現成字型，確保一定有字型可用。
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (text.font == null)
            text.font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
        text.fontSize = 20;
        text.color = Color.yellow;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -12);
        rect.sizeDelta = new Vector2(260, 160);

        text.text = BuildText();
    }

    static string BuildText ()
    {
        var vars = Engine.Initialized ? Engine.GetService<ICustomVariableManager>() : null;

        string Get (string varName)
        {
            if (vars != null && vars.TryGetVariableValue<float>(varName, out var v))
                return v.ToString("0");
            return "0";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[好感度]");
        foreach (var (varName, label) in Affinities)
            sb.AppendLine($"{label}: {Get(varName)}");
        return sb.ToString();
    }
}
