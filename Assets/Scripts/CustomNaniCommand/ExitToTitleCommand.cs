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
        Debug.Log("[ExitToTitle] Title 場景已載入，開始還原 UI");

        // 場景載入「完成」的當下還有一堆東西沒跑完：Title 自己的 Awake/Start、
        // 舊場景物件的 OnDestroy（例如 NaniCamGate 會把 naniCam.enabled 還原成它
        // 當初記下的值，那個值很可能是 false）、Naninovel 的後續初始化。
        // 任何一個都可能把我們設好的狀態蓋回去，所以持續重設一段時間，
        // 並且記錄有沒有人在中途把它關掉。
        var foughtBack = 0;
        for (var i = 0; i < 60; i++)
        {
            await UniTask.Yield();
            if (i > 0 && !IsUIShowing()) foughtBack++;
            RestoreTitleUI();
        }

        LogFinalState(foughtBack);
    }

    /// <summary>
    /// ITitleUI / IManagedUI 介面本身沒有 GameObject（那是 UIManager 內部
    /// ManagedUI 包裝類才有的），但實作一定是 MonoBehaviour，從那裡取。
    /// </summary>
    private static GameObject GetGameObject (ITitleUI ui)
    {
        var behaviour = ui as MonoBehaviour;
        return behaviour != null ? behaviour.gameObject : null;
    }

    /// <summary>目前 UI 到底看不看得見。</summary>
    private static bool IsUIShowing ()
    {
        var cameraManager = Engine.GetService<ICameraManager>();
        if (cameraManager == null || !cameraManager.RenderUI) return false;

        var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>();
        if (titleUI == null || !titleUI.Visible) return false;

        var go = GetGameObject(titleUI);
        return go != null && go.activeInHierarchy;
    }

    private static void LogFinalState (int foughtBack)
    {
        var cameraManager = Engine.GetService<ICameraManager>();
        var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>();

        var go = titleUI != null ? GetGameObject(titleUI) : null;
        var inactiveParent = "無";
        if (go != null && !go.activeInHierarchy)
        {
            // 自己是 active 卻仍然 activeInHierarchy=false → 一定是某個上層被關掉了
            for (var t = go.transform; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) inactiveParent = t.name;
        }

        Debug.Log($"[ExitToTitle] 完成 — " +
                  $"RenderUI={cameraManager?.RenderUI}, " +
                  $"naniCam.enabled={cameraManager?.Camera?.enabled}, " +
                  $"TitleUI={(titleUI == null ? "<null>" : "有")}, " +
                  $"Visible={titleUI?.Visible}, " +
                  $"activeSelf={go?.activeSelf}, activeInHierarchy={go?.activeInHierarchy}, " +
                  $"被關掉的上層={inactiveParent}, " +
                  $"中途被別人關掉次數={foughtBack}");
    }

    /// <summary>
    /// 把進地圖時被關掉、而 ResetStateAsync() 又還原不了的 UI 狀態補回來。
    /// </summary>
    private static void RestoreTitleUI ()
    {
        var uiManager = Engine.GetService<IUIManager>();

        // MapTest 進地圖時會呼叫 SetUIVisibleWithToggle(false)。RenderUI 實際上就是
        // UICamera.enabled，而 CameraManager.ResetService() 只還原主相機、不碰 UICamera，
        // 所以連 ResetStateAsync() 都清不掉。這行同時會把擋住點擊的 ClickThroughPanel 收掉。
        uiManager?.SetUIVisibleWithToggle(true, false);

        var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCamera != null) naniCamera.enabled = true;

        // Title 選單是 Naninovel 常駐的 UI，只有在遊戲第一次啟動時會被自動 Show()
        // （見 RuntimeInitializer.cs 的 ShowTitleUI 邏輯）。中途重新載入 Title 場景
        // 並不會讓它自動出現，這裡比照同樣的方式手動顯示一次。
        var titleUI = uiManager?.GetUI<ITitleUI>();
        if (titleUI == null) return;

        // Visible 只管 CanvasGroup，物件本身（或它的某個上層）被 SetActive(false)
        // 的話一樣看不見，所以這裡連 GameObject 也一起打開。
        var go = GetGameObject(titleUI);
        if (go != null && !go.activeSelf) go.SetActive(true);

        titleUI.Show();
    }
}
