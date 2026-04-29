using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 ChangeRuneScene 任意 GameObject 上。
/// 自動找到場景裡名為 "Button" 的確認按鈕，點擊後透過 MapReturnPoint 返回劇本。
/// </summary>
public class ChangeRuneSceneManager : MonoBehaviour
{
    void Start()
    {
        // 依名稱找按鈕，支援「返回」或備援名「Button」
        var btnGO = GameObject.Find("返回") ?? GameObject.Find("Button");
        if (btnGO == null)
        {
            Debug.LogWarning("[ChangeRuneScene] 找不到返回按鈕（'返回' 或 'Button'）");
            return;
        }

        var btn = btnGO.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning("[ChangeRuneScene] 找到物件但沒有 Button 元件");
            return;
        }

        btn.onClick.AddListener(OnConfirm);
        Debug.Log("[ChangeRuneScene] 確認按鈕已綁定");
    }

    void OnConfirm()
    {
        Debug.Log("[ChangeRuneScene] 確認 → GoBackToSavedStory");
        NaniBridgeUtility.GoBackToSavedStory();
    }
}
