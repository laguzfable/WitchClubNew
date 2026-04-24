// Assets\Naninovel\Runtime\UI\CGGallery\CGViewerPanel.cs

using System.Collections.Generic;
using System.Linq;
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

        private readonly Queue<Texture2D> textureQueue = new Queue<Texture2D>();
        private ImageCrossfader crossfader;
        private CanvasGroup canvasGroup;

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
            var viewportWidth = viewportRect.rect.width;
            var viewportHeight = viewportRect.rect.height;

            var scale = viewportHeight / texture.height;
            var displayedWidth = texture.width * scale;
            var isWide = displayedWidth > viewportWidth + 1f;

            if (aspectRatioFitter != null)
                aspectRatioFitter.enabled = false;

            scrollRect.horizontal = isWide;
            scrollRect.vertical = false;

            var contentWidth = isWide ? displayedWidth : viewportWidth;
            scrollContent.sizeDelta = new Vector2(contentWidth, viewportHeight);

            var imageRect = contentImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;

            if (isWide)
                scrollRect.normalizedPosition = new Vector2(0f, 0f);
        }
    }
}