using System.Collections;
using UnityEngine;

//CG = canvas group
[RequireComponent(typeof(CanvasGroup))]
public class CGFadeHelper : MonoBehaviour
{
    protected CanvasGroup _canvasGroup;

    public System.Action onFadeInBegin;
    public System.Action onFadeOutBegin;
    public System.Action onFadeInComplete;
    public System.Action onFadeOutComplete;

    [SerializeField, Tooltip("Set default alpha value.")]
    float defaultAlpha;

    public CanvasGroup canvasGroup
    {
        get
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            return _canvasGroup;
        }
    }

    void Awake()
    {
        canvasGroup.alpha = defaultAlpha;

        canvasGroup.blocksRaycasts = canvasGroup.alpha == 1f;
        canvasGroup.interactable = canvasGroup.alpha == 1f;
        Init();
    }

    protected virtual void Init()
    {

    }   

    protected virtual void OnFadeInBegin()
    {

    }

    protected virtual void FadeInComplete()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    protected virtual void OnFadeOutBegin()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    protected virtual void FadeOutComplete()
    {

    }

    public void FadeIn(float fadeDuration = 0.3f)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        StartCoroutine(FadeInCoroutine(fadeDuration));
    }

    private IEnumerator FadeInCoroutine(float fadeDuration)
    {
        OnFadeInBegin();
        if (onFadeInBegin != null)
        {
            onFadeInBegin();
        }
        //gameObject.SetActive(true);

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            var curTime = Time.realtimeSinceStartup;
            var deltaTime = 0f;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += deltaTime * (1 / fadeDuration);
                yield return null;
                deltaTime = Time.realtimeSinceStartup - curTime;
                curTime = Time.realtimeSinceStartup;
            }
        }
        FadeInComplete();
        if (onFadeInComplete != null)
        {
            onFadeInComplete();
        }
    }

    public void FadeOut(float fadeDuration = 0.3f)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        StartCoroutine(FadeOutCoroutine(fadeDuration));
    }

    private IEnumerator FadeOutCoroutine(float fadeDuration)
    {
        OnFadeOutBegin();
        if (onFadeOutBegin != null)
        {
            onFadeOutBegin();
        }
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = 0f;
        }
        else
        {
            var curTime = Time.realtimeSinceStartup;
            var deltaTime = 0f;
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= deltaTime * (1 / fadeDuration);
                yield return null;
                deltaTime = Time.realtimeSinceStartup - curTime;
                curTime = Time.realtimeSinceStartup;
            }
        }
        //gameObject.SetActive(false);
        FadeOutComplete();
        if (onFadeOutComplete != null)
        {
            onFadeOutComplete();
        }
    }
}