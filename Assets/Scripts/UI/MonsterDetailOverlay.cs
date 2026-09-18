using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 點圖鑑裡的怪物之後蓋滿全螢幕的放大檢視：
    /// 隨機背景（幾張輪流，不會連續兩次同一張）、緩緩上下漂浮的立繪、上方一句台詞、下方名字。
    /// 點畫面任一處關掉。
    ///
    /// 沒有沿用 Naninovel 的 CGViewerPanel，因為那支是綁 CG 解鎖流程的 RawImage 檢視器，
    /// 這裡只要單純把 Sprite 放大而已。
    ///
    /// 物件在 CGGalleryUI.prefab 裡（由 GalleryLayoutBuilder 建），整片是一顆 Button，點了就關。
    /// </summary>
    public class MonsterDetailOverlay : MonoBehaviour
    {
        [SerializeField] private Image portrait = default;
        [SerializeField] private Text nameLabel = default;
        [Tooltip("立繪上方的台詞（MobData 的「圖鑑台詞」）。沒填就整行藏起來。")]
        [SerializeField] private Text quoteLabel = default;

        [Header("背景")]
        [SerializeField] private Image background = default;
        [Tooltip("每次打開隨機挑一張，不會連續兩次同一張。")]
        [SerializeField] private Sprite[] backgrounds = default;

        [Header("漂浮")]
        [Tooltip("上下漂的是這個物件（立繪的外層），立繪本身的位置照 Editor 擺的不動。")]
        [SerializeField] private RectTransform floatTarget = default;
        [Tooltip("往上、往下各漂幾像素。")]
        [SerializeField] private float floatAmplitude = 16f;
        [Tooltip("上去再下來一趟要幾秒。")]
        [SerializeField] private float floatPeriod = 4f;

        private Vector2 floatOrigin;
        private float shownAt;
        private int lastBackground = -1;

        public bool IsShown => gameObject.activeSelf;

        void Awake ()
        {
            var button = GetComponent<Button>();
            if (button) button.onClick.AddListener(Hide);
            if (floatTarget) floatOrigin = floatTarget.anchoredPosition;
        }

        void Update ()
        {
            if (!floatTarget || floatPeriod <= 0) return;
            // 用 unscaledTime：回憶模式可能是在暫停選單底下開的。
            var t = (Time.unscaledTime - shownAt) / floatPeriod * Mathf.PI * 2f;
            floatTarget.anchoredPosition = floatOrigin + new Vector2(0, Mathf.Sin(t) * floatAmplitude);
        }

        public void Show (Sprite sprite, string displayName, string quote)
        {
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
            nameLabel.text = displayName;

            if (quoteLabel)
            {
                var hasQuote = !string.IsNullOrWhiteSpace(quote);
                quoteLabel.gameObject.SetActive(hasQuote);
                quoteLabel.text = hasQuote ? quote.Trim() : string.Empty;
            }

            PickBackground();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // 每次都從中間開始漂，不要一打開就在最高點。
            shownAt = Time.unscaledTime;
            if (floatTarget) floatTarget.anchoredPosition = floatOrigin;
        }

        public void Hide ()
        {
            gameObject.SetActive(false);
        }

        void PickBackground ()
        {
            if (!background || backgrounds == null || backgrounds.Length == 0) return;

            var index = Random.Range(0, backgrounds.Length);
            if (backgrounds.Length > 1 && index == lastBackground)
                index = (index + 1 + Random.Range(0, backgrounds.Length - 1)) % backgrounds.Length;
            lastBackground = index;

            background.sprite = backgrounds[index];
        }
    }
}
