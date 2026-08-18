using UnityEngine;
using UnityEngine.UI;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 掛在 TowerHubScene 裡。高塔模式進/出戰鬥之間的選單頁：
    /// 換符文、換卡片、進入下一層。三顆按鈕在 Inspector 拖進去即可，不用照名字找。
    /// </summary>
    public class TowerHubSceneManager : MonoBehaviour
    {
        [SerializeField] Button runeButton;
        [SerializeField] Button cardButton;
        [SerializeField] Button amuletButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button titleButton;
        [SerializeField] Button abandonButton; // 選填：「放棄本次挑戰」，點了會跳出下面的確認彈窗
        [SerializeField] Text floorText; // 選填，顯示「第 X 層」

        [Header("放棄挑戰確認彈窗（跟其他按鈕同一個 Overlay Canvas 底下，避免疊層問題）")]
        [SerializeField] GameObject abandonConfirmPanel; // 選填：彈窗物件，預設要是關閉狀態
        [SerializeField] Text abandonConfirmMessage; // 選填：彈窗上的說明文字
        [SerializeField] Button abandonYesButton; // 選填：彈窗的「確定」
        [SerializeField] Button abandonNoButton; // 選填：彈窗的「取消」

        LocaleRefresher refresher;

        void Start()
        {
            // 玩家在設定選單切語言時，這一頁要即時跟著換（不用退出再進來）
            refresher = LocaleRefresher.For(gameObject);

            if (floorText != null)
            {
                // 這個 Text 的寬度只夠放「第 X 層」，右邊緊接著就是「放棄本次挑戰」按鈕，
                // 最高紀錄接在同一行會被切掉。所以換行放第二行，並且關掉截斷
                // （場景裡設的是 Truncate，第二行會直接不見）。
                floorText.horizontalOverflow = HorizontalWrapMode.Overflow;
                floorText.verticalOverflow = VerticalWrapMode.Overflow;

                // 樓層文字是數字組出來的、查表處理不了，所以走自訂重繪（註冊時會立刻執行一次）
                refresher.OnRefresh(() =>
                    floorText.text = BuildFloorLabel(TowerModeManager.CurrentFloor));
            }

            SetupButton(runeButton, TowerModeManager.OpenRuneScreen, "runeButton");
            SetupButton(cardButton, TowerModeManager.OpenCardScreen, "cardButton");

            // 還沒解鎖任何卡片型態就把換卡片藏起來（跟休息室同一個規則）。
            // 競技場開挑戰時會把四色都解開，所以挑戰中這顆一定看得到。
            if (cardButton != null && !CardVariantUnlock.AnyUnlocked)
                cardButton.gameObject.SetActive(false);
            SetupButton(amuletButton, TowerModeManager.OpenAmuletScreen, "amuletButton");
            SetupButton(continueButton, TowerModeManager.ContinueToNextFloor, "continueButton");
            SetupButton(titleButton, TowerModeManager.QuitToTitle, "titleButton");
            SetupButton(abandonButton, ShowAbandonConfirmPanel, "abandonButton");
            SetupButton(abandonYesButton, TowerModeManager.AbandonRun, "abandonYesButton");
            SetupButton(abandonNoButton, HideAbandonConfirmPanel, "abandonNoButton");

            if (abandonConfirmMessage != null)
                refresher.Track(abandonConfirmMessage, isDesc: true);

            HideAbandonConfirmPanel();
        }

        void ShowAbandonConfirmPanel()
        {
            if (abandonConfirmPanel != null)
                abandonConfirmPanel.SetActive(true);
        }

        void HideAbandonConfirmPanel()
        {
            if (abandonConfirmPanel != null)
                abandonConfirmPanel.SetActive(false);
        }

        void SetupButton(Button btn, UnityEngine.Events.UnityAction onClick, string fieldName)
        {
            if (btn == null)
            {
                Debug.LogWarning($"[TowerHubScene] {fieldName} 未指定");
                return;
            }

            // 按鈕上的文字沿用場景既有的多國語言字典（RuneEnTranslation）。
            // 交給 refresher 記住原文，切語言時才回得去（直接覆寫的話第二次就查不到表了）。
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) refresher.Track(label);

            btn.onClick.AddListener(onClick);
        }

        /// <summary>
        /// 「第 X 層」下面接一行最高紀錄（最高樓層有成就，看不到的話玩家不會知道自己打到哪）。
        /// BestFloor 一直有在寫進 PlayerPrefs（TowerModeManager.RecordBestFloor：打贏一場 / 戰敗 /
        /// 退出 / 回標題 / 放棄挑戰都會更新），
        /// 但之前沒有任何地方讀出來顯示，玩家等於看不到自己的紀錄。
        /// 還沒有紀錄（0）時那一行就整行不出現，不會變成「最高紀錄 0 層」。
        /// </summary>
        static string BuildFloorLabel(int floor)
        {
            var lang = PlayerPrefs.GetString("Language", "zh-TW").ToLower();
            var best = TowerModeManager.BestFloor;

            if (lang.StartsWith("ja"))
            {
                var label = $"{floor}階";
                if (best > 0) label += $"\n最高記録 {best}階";
                return label;
            }

            if (!lang.StartsWith("zh"))
            {
                var label = $"Floor {floor}";
                if (best > 0) label += $"\nBest: {best}";
                return label;
            }

            var zh = $"第 {floor} 層";
            if (best > 0) zh += $"\n最高紀錄 {best} 層";
            return zh;
        }
    }
}
