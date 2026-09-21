using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 選單（PauseUI）裡的「CLOSE」按鈕：把選單收起來。
    ///
    /// ★ 為什麼需要 ★
    /// PauseUI 是 modal：開著的時候其他 UI（包括對話框控制列上的 MENU 鍵）都不能按，
    /// 所以不能靠「再按一次 MENU」關掉。原版只能按 Esc／Backspace，滑鼠玩家會卡住。
    /// </summary>
    public class PauseMenuCloseButton : ScriptableButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();
            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick ()
        {
            uiManager?.GetUI<IPauseUI>()?.ChangeVisibilityAsync(false).Forget();
        }
    }
}
