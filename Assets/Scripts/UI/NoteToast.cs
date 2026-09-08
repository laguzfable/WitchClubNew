// Assets/Scripts/UI/NoteToast.cs
//
// 「筆記本 新增一頁：燼芽」的提示。
//
// 為什麼要有這個：解鎖的當下是玩家好奇心最高的時候，沒有提示的話他根本不知道
// 筆記本多了東西，那本書就只會在破台之後被翻一次。作法比照 AffinityChangeNotifier
// （同樣是程式生 UI、同樣掛在一個 DontDestroyOnLoad 的 Canvas 上），
// 但刻意排在畫面另一個高度，免得跟好感度提示疊在一起。

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.UI;

public class NoteToast : MonoBehaviour
{
    const float AnchorY = 0.74f;      // 好感度提示在 0.82，這裡往下讓開
    const int FontSize = 26;
    static readonly Color Ink = new Color(0.85f, 0.80f, 0.98f);

    static NoteToast instance;

    RectTransform root;
    readonly Queue<string> pending = new Queue<string>();
    bool running;

    /// <summary>跳一行提示。標題是筆記的標題。</summary>
    public static void Show (string noteTitle)
    {
        if (string.IsNullOrWhiteSpace(noteTitle)) return;

        if (instance == null)
        {
            var go = new GameObject("NoteToast");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<NoteToast>();
        }

        instance.pending.Enqueue($"筆記本　新增一頁：{noteTitle}");
        if (!instance.running) instance.Run().Forget();
    }

    async UniTaskVoid Run ()
    {
        running = true;
        while (pending.Count > 0)
            await ShowOne(pending.Dequeue());
        running = false;
    }

    async UniTask ShowOne (string message)
    {
        EnsureCanvas();

        var go = new GameObject("Toast", typeof(RectTransform));
        go.transform.SetParent(root, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, AnchorY);
        rect.sizeDelta = new Vector2(900f, 80f);
        rect.anchoredPosition = Vector2.zero;

        var text = go.AddComponent<Text>();
        text.font = ResolveFont();
        text.fontSize = FontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(Ink.r, Ink.g, Ink.b, 0f);
        text.text = message;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = new Vector2(2f, -2f);

        const float fadeIn = 0.2f, hold = 1.6f, fadeOut = 0.45f, rise = 26f;
        var startPos = rect.anchoredPosition;

        var t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            var a = Mathf.Clamp01(t / fadeIn);
            text.color = new Color(Ink.r, Ink.g, Ink.b, a);
            outline.effectColor = new Color(0f, 0f, 0f, a * 0.8f);
            await UniTask.Yield();
        }
        text.color = Ink;
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);

        await UniTask.Delay(TimeSpan.FromSeconds(hold));

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            var p = t / fadeOut;
            text.color = new Color(Ink.r, Ink.g, Ink.b, 1f - p);
            outline.effectColor = new Color(0f, 0f, 0f, (1f - p) * 0.8f);
            rect.anchoredPosition = startPos + new Vector2(0f, rise * p);
            await UniTask.Yield();
        }

        Destroy(go);
    }

    void EnsureCanvas ()
    {
        if (root != null) return;

        var go = new GameObject("NoteToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root = go.GetComponent<RectTransform>();
    }

    static Font ResolveFont ()
    {
        var fromScene = FindObjectsOfType<Text>()
            .Select(x => x.font)
            .FirstOrDefault(f => f != null && f.name.Contains("Noto"));
        if (fromScene != null) return fromScene;

        return Resources.GetBuiltinResource<Font>("Arial.ttf")
               ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
    }
}
