using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    CanvasGroup cg;

    public static ScreenFader Ensure()
    {
        if (Instance) return Instance;
        var go = new GameObject("__ScreenFader__");
        Object.DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500000;

        go.AddComponent<GraphicRaycaster>();
        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;

        var imgGO = new GameObject("Black");
        imgGO.transform.SetParent(go.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var f = go.AddComponent<ScreenFader>();
        f.cg = group; Instance = f; return f;
    }

    public void InstantBlack() { if (!cg) cg = GetComponent<CanvasGroup>(); cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
    public void InstantClear() { if (!cg) cg = GetComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }

    public Coroutine FadeOut(float d = 0.18f) => StartCoroutine(FadeTo(1f, d));
    public Coroutine FadeIn (float d = 0.18f) => StartCoroutine(FadeTo(0f, d));
    public IEnumerator FadeOutCoroutine(float d = 0.18f) { yield return FadeTo(1f, d); }
    public IEnumerator FadeInCoroutine (float d = 0.18f) { yield return FadeTo(0f, d); }

    IEnumerator FadeTo(float target, float duration)
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        float start = cg.alpha, t = 0f;
        cg.blocksRaycasts = true;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, duration <= 0 ? 1f : Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = target;
        cg.blocksRaycasts = target >= 1f;
        cg.interactable   = target >= 1f;
    }
}
