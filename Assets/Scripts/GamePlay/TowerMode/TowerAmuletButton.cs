using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 掛在 TowerHubScene / ChangeAmuletScene 的護身符按鈕上，一顆對應一個護身符。
    /// 全部護身符共用同一個裝備欄位（只能裝一個）：點還沒裝備的會換成那個，點已經裝備中的會取消裝備。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TowerAmuletButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("護身符代號：顏色護身符請填 Blue/Red/Yellow/Green（要跟 ECardElement 名稱一致），其他護身符（如次元護符）自行取一個獨一無二的英文代號。")]
        [SerializeField] string amuletId;

        void Awake()
        {
            // 交給 refresher 記住原文，玩家中途切語言時護身符名稱才跟得上
            var label = GetComponentInChildren<Text>();
            if (label != null) LocaleRefresher.For(gameObject).Track(label);

            GetComponent<Button>().onClick.AddListener(OnClick);
            RefreshVisual();
        }

        void OnClick()
        {
            TowerModeManager.ToggleAmulet(amuletId);

            // 同一組（同一個父物件）底下的護身符按鈕，全部一起刷新放大狀態
            // （裝/卸轉接頭會影響欄位數，其他按鈕的可裝備狀態也可能跟著變）
            var parent = transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var btn = parent.GetChild(i).GetComponent<TowerAmuletButton>();
                if (btn != null) btn.RefreshVisual();
            }
        }

        void RefreshVisual()
        {
            bool equipped = TowerModeManager.IsAmuletEquipped(amuletId);
            transform.localScale = equipped ? new Vector3(1.15f, 1.15f, 1.15f) : Vector3.one;
        }

        // ── Hover → 顯示右側詳情（跟符文頁同一套面板、同一套邏輯）──────
        // 護身符沒有獨立的立繪資源，圖示就直接拿按鈕自己那張。
        // 選單頁（TowerHubScene）沒有詳情面板，Instance 是 null，這裡就自動不做事。
        public void OnPointerEnter (PointerEventData eventData)
        {
            var icon = GetComponent<Image>();
            RuneDetailPanel.Instance?.ShowAmulet(amuletId, icon != null ? icon.sprite : null);
        }

        public void OnPointerExit (PointerEventData eventData)
        {
            RuneDetailPanel.Instance?.Hide();
        }
    }
}
