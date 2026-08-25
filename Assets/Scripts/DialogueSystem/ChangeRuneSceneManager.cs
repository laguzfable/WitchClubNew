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
        // Translate all tab / button labels to English.
        // 交給 refresher 記住每個標籤的原文（Track 會立刻套用一次，行為跟原本一樣），
        // 這樣玩家在設定選單切語言時整頁會即時重翻，不用退出再進來。
        var refresher = LocaleRefresher.For(gameObject);
        foreach (var t in FindObjectsOfType<Text>())
        {
            // 右側詳情面板的文字是隨著滑到哪張卡動態換的，原文不固定，
            // 由 RuneDetailPanel 自己註冊重畫；被這裡記成「原文」的話，切語言時
            // 會被蓋回當初記到的那一份（通常是「← 選擇一個符文」）。
            if (t.GetComponentInParent<RuneDetailPanel>() != null) continue;
            refresher.Track(t);
        }

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

        // 競技場挑戰中整條控制列都不顯示：那邊每一頁本來就有自己的按鈕組（返回／回標題），
        // 而且回標題必須走 TowerModeManager.QuitToTitle 才會保留續關記錄。
        // 只有劇情流程（休息室進來的）才需要這排。
        if (!TowerModeManager.IsActive)
            Hexe.UI.SceneControlBar.Show();

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
