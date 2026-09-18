using UnityEngine;
using UnityEngine.EventSystems;

namespace Hexe.UI
{
    /// <summary>
    /// 滑鼠移上去（或鍵盤選到）時顯示選中外框。回憶模式的 CG 格子和怪物格子用。
    /// 存讀檔格子的同一件事寫在 HexeGameStateSlot 裡，那邊本來就有繼承可以覆寫。
    /// </summary>
    public class HoverFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject frame = default;

        private bool hovered, selected;

        void OnEnable () => Apply();

        void OnDisable ()
        {
            hovered = selected = false;
            Apply();
        }

        public void OnPointerEnter (PointerEventData _) { hovered = true; Apply(); }
        public void OnPointerExit (PointerEventData _) { hovered = false; Apply(); }
        public void OnSelect (BaseEventData _) { selected = true; Apply(); }
        public void OnDeselect (BaseEventData _) { selected = false; Apply(); }

        void Apply ()
        {
            if (frame) frame.SetActive(hovered || selected);
        }
    }
}
