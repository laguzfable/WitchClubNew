using UnityEngine;
using UnityEngine.UI;
using Hexe.TowerMode;

/// <summary>
/// 掛在 ChangeCardTypeScene 任意 GameObject 上。
/// 自動找到場景裡名為 "返回" 或 "Button" 的確認按鈕。
/// 一般劇情：透過 GoBackToSavedStory 返回劇本。
/// 高塔模式：回到高塔選單頁（TowerHubScene），不是直接接下一層。
/// </summary>
public class ChangeCardTypeSceneManager : MonoBehaviour
{
    void Start()
    {
        // 高塔模式的換卡片頁不是劇情流程，不需要顯示好感度除錯資訊
        if (!TowerModeManager.IsActive)
            AffinityDebugOverlay.Show();

        var btnGO = GameObject.Find("返回") ?? GameObject.Find("Button");
        if (btnGO == null)
        {
            Debug.LogWarning("[ChangeCardTypeScene] 找不到返回按鈕（'返回' 或 'Button'）");
            return;
        }

        var btn = btnGO.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning("[ChangeCardTypeScene] 找到物件但沒有 Button 元件");
            return;
        }

        btn.onClick.AddListener(OnConfirm);
        Debug.Log("[ChangeCardTypeScene] 確認按鈕已綁定");
    }

    void OnConfirm()
    {
        if (TowerModeManager.IsActive)
        {
            Debug.Log("[ChangeCardTypeScene] 確認 → 高塔模式中，回選單頁");
            TowerModeManager.BackToHub();
            return;
        }

        Debug.Log("[ChangeCardTypeScene] 確認 → GoBackToSavedStory");
        NaniBridgeUtility.GoBackToSavedStory();
    }
}
