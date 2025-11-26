// Assets\Naninovel\Runtime\UI\CGGallery\CGViewerPanel.cs

using System.Collections.Generic;
using System.Linq; // 確保有這個才能用 Count()
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

        private readonly Queue<Texture2D> textureQueue = new Queue<Texture2D>();
        private ImageCrossfader crossfader;
        private CanvasGroup canvasGroup; // 引用 CanvasGroup


        public virtual void Show (IEnumerable<Texture2D> textures)
        {
            // 🔥 步驟 1: 強制啟用 GameObject (解決「隱藏狀態」問題)
            gameObject.SetActive(true); 

            EnsureInitialized(); // 確保 crossfader 和 canvasGroup 存在

            EnqueueTextures(textures);

            if (textureQueue.Count == 0)
            {
                Debug.LogError("[CG VIEWER] textureQueue empty! Abort show.");
                return;
            }

            ShowNextTexture(0);
            
            // 🔥 步驟 2: 強制設定 CanvasGroup.alpha 為 1 (解決「Alpha是0」問題)
            if (canvasGroup)
            {
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("[VIEWER SHOW FIX FINAL] Forced Active, Alpha=1, Interaction Enabled.");
            }

            // 呼叫 base.Show() 確保基礎邏輯運行，但現在它是否成功已不再重要
            base.Show(); 

            // 步驟 3: 再次檢查並強制設定 (以防 base.Show() 試圖將 alpha 設回 0)
            if (canvasGroup && canvasGroup.alpha < 1)
            {
                canvasGroup.alpha = 1;
            }
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

        // 統一初始化邏輯 (確保 crossfader 和 canvasGroup 存在)
        private void EnsureInitialized()
        {
            if (crossfader != null && canvasGroup != null) return; 
            
            this.AssertRequiredObjects(contentImage);
            crossfader = new ImageCrossfader(contentImage);

            // 獲取 CanvasGroup
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
            
            // 強制清除材質，解決圖片透明問題
            contentImage.material = null; 

            crossfader.Crossfade(texture, duration);
        }
    }
}