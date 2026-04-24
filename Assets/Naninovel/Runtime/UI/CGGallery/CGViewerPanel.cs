// Assets\Naninovel\Runtime\UI\CGGallery\CGViewerPanel.cs

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class CGViewerPanel : ScriptableButton
    {
        protected virtual string ShaderName { get; } = "Naninovel/TransitionalUI";

        [Tooltip("The image where the assigned CGs will be shown.")]
        [SerializeField] private RawImage contentImage = default;
        [Tooltip("When multiple CGs assigned, controls crossfade duration, in seconds.")]
        [SerializeField] private float crossfadeDuration = .3f;
        [Tooltip("ScrollRect used for wide CGs. When null, scrolling is disabled.")]
        [SerializeField] private ScrollRect scrollRect = default;
        [Tooltip("RectTransform of the content inside ScrollRect (parent of contentImage).")]
        [SerializeField] private RectTransform scrollContent = default;
        [Tooltip("AspectRatioFitter on the image — disabled when scroll layout is active.")]
        [SerializeField] private AspectRatioFitter aspectRatioFitter = default;

        [Tooltip("寬圖自動滾動到底所需的秒數。")]
        [SerializeField] private float autoScrollDuration = 5f;

        private readonly Queue<Texture2D> textureQueue = new Queue<Texture2D>();
        private ImageCrossfader crossfader;
        private CanvasGroup canvasGroup;
        private CancellationTokenSource scrollCancel;

        public virtual void Show (IEnumerable<Texture2D> textures)
        {
            gameObject.SetActive(true);
            EnsureInitialized();
            EnqueueTextures(textures);

            if (textureQueue.Count == 0)
            {
                Debug.LogError("[CG VIEWER] textureQueue empty! Abort show.");
                return;
            }

            base.Show();
            Canvas.ForceUpdateCanvases();
            ShowNextTexture(0);

            if (canvasGroup)
            {
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (canvasGroup && canvasGroup.alpha < 1)
                canvasGroup.alpha = 1;
        }

        protected override void Awake ()
        {
            base.Awake();
            EnsureInitialized();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            crossfader?.Dispose();
            scrollCancel?.Cancel();
            scrollCancel?.Dispose();
        }

        protected override void OnButtonClick ()
        {
            if (textureQueue.Count > 0)
                ShowNextTexture(crossfadeDuration);
            else Hide();
        }

        private void EnsureInitialized()
        {
            if (crossfader != null && canvasGroup != null) return;

            this.AssertRequiredObjects(contentImage);
            crossfader = new ImageCrossfader(contentImage);
            canvasGroup = GetComponent<CanvasGroup>();
            this.AssertRequiredObjects(canvasGroup);
        }

        private void EnqueueTextures (IEnumerable<Texture2D> textures)
        {
            textureQueue.Clear();
            foreach (var texture in textures)
                if (texture != null)
                    textureQueue.Enqueue(texture);
        }

        private void ShowNextTexture (float duration)
        {
            var texture = textureQueue.Dequeue();
            contentImage.material = null;
            crossfader.Crossfade(texture, duration);
            ApplyScrollLayout(texture);
        }

        private void ApplyScrollLayout (Texture2D texture)
        {
            if (scrollRect == null || scrollContent == null) return;

            var viewportRect = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            var viewportWidth  = viewportRect.rect.width;
            var viewportHeight = viewportRect.rect.height;

            // 長寬比超過 16:9 + 容差 → 寬圖，需要滾動
            const float standardAspect = 16f / 9f;
            var textureAspect = (float)texture.width / texture.height;
            var isWide = textureAspect > standardAspect + 0.05f;

            scrollRect.horizontal = isWide;
            scrollRect.vertical   = false;

            if (isWide)
            {
                if (aspectRatioFitter != null) aspectRatioFitter.enabled = false;

                // 寬圖：以 viewport 高度為基準，算出原始比例寬度
                var displayedWidth = texture.width * (viewportHeight / texture.height);
                scrollContent.sizeDelta = new Vector2(displayedWidth, 0f);

                var imageRect = contentImage.rectTransform;
                imageRect.anchorMin  = Vector2.zero;
                imageRect.anchorMax  = Vector2.one;
                imageRect.sizeDelta  = Vector2.zero;

                scrollRect.normalizedPosition = new Vector2(0f, 0f);
                StartAutoScroll();
            }
            else
            {
                // 標準圖：還原 AspectRatioFitter，讓原本的縮放邏輯處理
                if (aspectRatioFitter != null) aspectRatioFitter.enabled = true;
                scrollContent.sizeDelta = new Vector2(viewportWidth, 0f);

                var imageRect = contentImage.rectTransform;
                imageRect.anchorMin  = Vector2.zero;
                imageRect.anchorMax  = Vector2.one;
                imageRect.sizeDelta  = Vector2.zero;
            }
        }

        private void StartAutoScroll ()
        {
            scrollCancel?.Cancel();
            scrollCancel?.Dispose();
            scrollCancel = new CancellationTokenSource();
            AutoScrollAsync(scrollCancel.Token).Forget();
        }

        private async UniTaskVoid AutoScrollAsync (CancellationToken token)
        {
            // 稍等一下再開始滾（讓圖片先顯示完）
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: token);

            var elapsed = 0f;
            while (elapsed < autoScrollDuration)
            {
                if (token.IsCancellationRequested || scrollRect == null) return;
                elapsed += Time.deltaTime;
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(elapsed / autoScrollDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (token.IsCancellationRequested) return;
            }

            if (scrollRect != null)
                scrollRect.horizontalNormalizedPosition = 1f;
        }
    }
}