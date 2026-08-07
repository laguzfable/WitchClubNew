using UnityEngine;
using UnityEngine.UI;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 掛在 ChangeAmuletScene 任意 GameObject 上。
    /// 自動找到場景裡名為 "返回" 或 "Button" 的按鈕，點擊後回到高塔選單頁（TowerHubScene）。
    /// 這個場景只有高塔模式會用到，不用像符文/卡片頁那樣分兩種情況判斷。
    /// </summary>
    public class ChangeAmuletSceneManager : MonoBehaviour
    {
        void Start()
        {
            var btnGO = GameObject.Find("返回") ?? GameObject.Find("Button");
            if (btnGO == null)
            {
                Debug.LogWarning("[ChangeAmuletScene] 找不到返回按鈕（'返回' 或 'Button'）");
                return;
            }

            var btn = btnGO.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogWarning("[ChangeAmuletScene] 找到物件但沒有 Button 元件");
                return;
            }

            btn.onClick.AddListener(() => TowerModeManager.BackToHub());
        }
    }
}
