using UnityEngine;
using UnityEngine.UI;
using Hexe.TowerMode;

/// <summary>
/// 掛在 ChangeRuneScene 任意 GameObject 上。
/// 自動找到場景裡名為 "Button" 的確認按鈕，點擊後透過 MapReturnPoint 返回劇本。
/// </summary>
public class ChangeRuneSceneManager : MonoBehaviour
{
    void Start()
    {
        // Translate all tab / button labels to English
        foreach (var t in FindObjectsOfType<Text>())
            t.text = RuneEnTranslation.TranslateName(t.text);

        // 好感度除錯顯示要在翻譯掃描「之後」才建立，不然會被 TranslateName 誤改
        // 高塔模式的換符文頁不是劇情流程，不需要顯示好感度除錯資訊
        if (!TowerModeManager.IsActive)
            AffinityDebugOverlay.Show();
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
        // 高塔模式：從選單頁進來的，換完符文要回選單頁，不是直接接下一層
        if (TowerModeManager.IsActive)
        {
            Debug.Log("[ChangeRuneScene] 確認 → 高塔模式中，回選單頁");
            TowerModeManager.BackToHub();
            return;
        }

        Debug.Log("[ChangeRuneScene] 確認 → GoBackToSavedStory");
        NaniBridgeUtility.GoBackToSavedStory();
    }
}
