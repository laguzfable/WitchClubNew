using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 對話框控制列的「MENU」（選項）按鈕：開關暫停選單。
    ///
    /// 控制列原本各有一顆 SETTINGS 和 TITLE，太擠也太容易誤按回標題。
    /// 現在收成一顆，按下去叫出 Naninovel 的 PauseUI，裡面是 存檔／讀檔／設定／回標題。
    /// PauseUI 本來就接好了 Esc／Backspace（Pause 輸入），這顆按鈕跟那個鍵是同一個選單。
    /// PauseUI 裡的每顆按鈕按下去都會先把自己收掉，所以這裡只負責開關。
    /// </summary>
    public class ControlPanelMenuButton : ScriptableButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();
            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick ()
        {
            var pauseUI = uiManager?.GetUI<IPauseUI>();
            if (pauseUI != null) pauseUI.ChangeVisibilityAsync(!pauseUI.Visible).Forget();
        }
    }
}
