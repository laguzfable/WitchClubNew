using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 對話框控制列的「TITLE」按鈕：跳確認視窗，確定後回標題畫面。
    ///
    /// ★ 為什麼不用 Naninovel 內建的 ControlPanelTitleButton ★
    /// 那顆只做 ResetStateAsync() + TitleUI.Show()，**沒有停止腳本播放、也沒有載入 Title 場景**。
    /// 這個專案的劇本會繼續在背景跑（Console 會一直冒 ExecutePlayedCommandAsync），
    /// 畫面則卡在原本的場景，等於按了沒反應。
    /// 這裡改成呼叫 <see cref="ExitToTitleCommand.RunAsync"/>——@exitToTitle 走的同一套流程，
    /// 停播放器、重置狀態、載入 Title 場景、並把被關掉的 UI／相機搶回來。
    /// </summary>
    public class ControlPanelExitToTitleButton : ScriptableButton
    {
        [ManagedText("DefaultUI")]
        public static string ConfirmationMessage = "Return to the title screen?\nAny unsaved progress will be lost.";

        private IUIManager uiManager;
        private IConfirmationUI confirmationUI;

        protected override void Awake ()
        {
            base.Awake();
            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void Start ()
        {
            base.Start();
            confirmationUI = uiManager.GetUI<IConfirmationUI>();
        }

        protected override void OnButtonClick () => ExitAsync();

        private async void ExitAsync ()
        {
            // 暫停選單如果開著要先收掉，不然回到標題後它會蓋在選單上面
            uiManager.GetUI<IPauseUI>()?.Hide();

            // 確認視窗拿不到就直接走（總比按了沒反應好）
            if (confirmationUI != null && !await confirmationUI.ConfirmAsync(ConfirmationMessage))
                return;

            await ExitToTitleCommand.RunAsync();
        }
    }
}
