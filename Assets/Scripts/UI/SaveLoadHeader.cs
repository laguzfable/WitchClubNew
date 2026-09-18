using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 存讀檔畫面的標題：讀檔時是「讀取進度」，存檔時是「儲存進度」。
    ///
    /// SaveLoadMenu 切換模式的 SetPresentationMode 在 Naninovel 的 assembly 裡，
    /// 掛不上事件，所以每幀看一下模式有沒有變。只是比一個 enum，不花什麼。
    /// </summary>
    public class SaveLoadHeader : MonoBehaviour
    {
        [SerializeField] private SaveLoadMenu menu = default;
        [SerializeField] private Image title = default;
        [SerializeField] private Sprite loadTitle = default;
        [SerializeField] private Sprite saveTitle = default;

        private SaveLoadUIPresentationMode? shownMode;

        void LateUpdate ()
        {
            if (!menu || !title) return;

            var mode = menu.PresentationMode;
            if (shownMode == mode) return;
            shownMode = mode;

            var sprite = mode == SaveLoadUIPresentationMode.Save ? saveTitle : loadTitle;
            if (sprite) title.sprite = sprite;
        }
    }
}
