using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 存讀檔格子。版面照設計圖：上面截圖，下面兩行字——
    ///   01 | 第一章            09/17 14:30
    ///        旅人・第2周目
    /// 內容來源見 <see cref="SaveSlotInfo"/>。
    ///
    /// 繼承而不是直接改 GameStateSlot：那支在 Elringus.Naninovel.Runtime 底下，看不到我們的程式。
    /// 原本的 onTitleTextChanged 不用了，字直接寫進下面這幾個 Text。
    /// </summary>
    public class HexeGameStateSlot : GameStateSlot
    {
        const string DateFormat = "MM/dd  HH:mm";

        [Header("Hexe")]
        [SerializeField] private Text numberText = default;
        [SerializeField] private Text chapterText = default;
        [SerializeField] private Text dateText = default;
        [SerializeField] private Text detailText = default;
        [Tooltip("空格子的插畫，沒有存檔時取代截圖。每格都是同一張，免得玩家以為不同格有什麼不一樣。")]
        [SerializeField] private Image emptyArt = default;
        [Tooltip("滑鼠移上去或鍵盤選到時才顯示的外框。")]
        [SerializeField] private GameObject selectedFrame = default;

        public override void Bind (int slotNumber, GameStateMap state)
        {
            base.Bind(slotNumber, state);

            var empty = state is null;
            SetText(numberText, slotNumber.ToString("00"));
            SetText(dateText, empty ? "" : state.SaveDateTime.ToString(DateFormat));

            if (empty)
            {
                SetText(chapterText, SaveSlotInfo.EmptyLabel);
                SetText(detailText, "");
            }
            else
            {
                var labels = SaveSlotInfo.Describe(state);
                SetText(chapterText, labels.Chapter);
                SetText(detailText, labels.Detail);
            }

            // 沒截圖時只調成透明，不要關掉物件：舊版格子上能接點擊的只有這張圖，
            // 關掉的話空格子點不下去，就存不了檔。
            var hasThumbnail = !empty && state.Thumbnail;
            ThumbnailImage.color = hasThumbnail ? Color.white : Color.clear;
            if (hasThumbnail) CropToFit(ThumbnailImage, state.Thumbnail);

            if (emptyArt) emptyArt.gameObject.SetActive(empty);
        }

        public override void OnPointerEnter (PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            SetHighlighted(true);
        }

        public override void OnPointerExit (PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            SetHighlighted(Selected);
        }

        public override void OnSelect (BaseEventData eventData)
        {
            base.OnSelect(eventData);
            SetHighlighted(true);
        }

        public override void OnDeselect (BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            SetHighlighted(false);
        }

        protected override void Awake ()
        {
            base.Awake();
            SetHighlighted(false);
        }

        // 字改由上面那幾個 Text 顯示。
        protected override void SetTitleText (string value) { }

        void SetHighlighted (bool on)
        {
            if (selectedFrame) selectedFrame.SetActive(on);
        }

        static void SetText (Text text, string value)
        {
            if (text) text.text = value;
        }

        /// <summary>截圖是 16:9，格子裡的框扁得多：取中間那一段，不要壓扁。</summary>
        static void CropToFit (RawImage image, Texture texture)
        {
            var size = image.rectTransform.rect.size;
            if (size.x <= 0 || size.y <= 0 || texture.width <= 0 || texture.height <= 0)
            {
                image.uvRect = new Rect(0, 0, 1, 1);
                return;
            }

            var target = size.x / size.y;
            var source = (float)texture.width / texture.height;
            if (source < target)
            {
                var h = source / target;
                image.uvRect = new Rect(0, (1 - h) / 2, 1, h);
            }
            else
            {
                var w = target / source;
                image.uvRect = new Rect((1 - w) / 2, 0, w, 1);
            }
        }
    }
}
