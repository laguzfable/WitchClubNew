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

        void Start()
        {
            if (floorText != null)
                floorText.text = BuildFloorLabel(TowerModeManager.CurrentFloor);

            SetupButton(runeButton, TowerModeManager.OpenRuneScreen, "runeButton");
            SetupButton(cardButton, TowerModeManager.OpenCardScreen, "cardButton");
            SetupButton(amuletButton, TowerModeManager.OpenAmuletScreen, "amuletButton");
            SetupButton(continueButton, TowerModeManager.ContinueToNextFloor, "continueButton");
            SetupButton(titleButton, TowerModeManager.QuitToTitle, "titleButton");
            SetupButton(abandonButton, ShowAbandonConfirmPanel, "abandonButton");
            SetupButton(abandonYesButton, TowerModeManager.AbandonRun, "abandonYesButton");
            SetupButton(abandonNoButton, HideAbandonConfirmPanel, "abandonNoButton");

            if (abandonConfirmMessage != null)
                abandonConfirmMessage.text = RuneEnTranslation.TranslateDesc(abandonConfirmMessage.text);

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

            // 按鈕上的文字沿用場景既有的多國語言字典（RuneEnTranslation）
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = RuneEnTranslation.TranslateName(label.text);

            btn.onClick.AddListener(onClick);
        }

        static string BuildFloorLabel(int floor)
        {
            var lang = PlayerPrefs.GetString("Language", "zh-TW").ToLower();
            if (lang.StartsWith("ja")) return $"{floor}階";
            if (!lang.StartsWith("zh")) return $"Floor {floor}";
            return $"第 {floor} 層";
        }
    }
}
