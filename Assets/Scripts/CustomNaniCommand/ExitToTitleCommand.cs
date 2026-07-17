using System.Linq;
using Naninovel;
using Naninovel.Commands;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// @exitToTitle
/// 結局播完後返回 Title 畫面：停止腳本播放、完整重置 Naninovel 狀態
/// （包含自訂變數，例如好感度，確保下次開新遊戲時是乾淨的），
/// 然後載入 Title 場景。
/// </summary>
[CommandAlias("exitToTitle")]
public class ExitToTitleCommand : Command, Command.IForceWait
{
    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        // 隱藏「繼續」提示（可能是 inactive 狀態，FindObjectOfType 抓不到，
        // 用 FindObjectsOfTypeAll 保險）。
        var continueInputUI = Resources.FindObjectsOfTypeAll<ContinueInputUI>()
            .FirstOrDefault(x => x.gameObject.scene.IsValid());
        if (continueInputUI != null)
            continueInputUI.Visible = false;

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer?.Stop();

        // 完整重置（不排除任何 service），確保好感度等自訂變數不會帶到下一輪。
        var stateManager = Engine.GetService<IStateManager>();
        if (stateManager != null)
            await stateManager.ResetStateAsync();

        await SceneManager.LoadSceneAsync("Title");

        // Title 選單是 Naninovel 常駐的 UI，只有在遊戲第一次啟動時會被自動 Show()
        // （見 RuntimeInitializer.cs 的 ShowTitleUI 邏輯）。中途重新載入 Title 場景
        // 並不會讓它自動出現，這裡比照同樣的方式手動顯示一次。
        Engine.GetService<IUIManager>()?.GetUI<ITitleUI>()?.Show();
    }
}
