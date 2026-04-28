using Naninovel;
using Naninovel.Commands;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// @GotoMapDemo ReturnScript:XXX ReturnLabel:YYY
///
/// 切換到地圖場景（MapTest），但不重置 Naninovel 狀態，
/// 讓 NextScript / NextLabel 變數保留，供 DemoMapAutoProgress 返回時使用。
/// 等同 @GotoUnityScene 但省略了 ResetStateAsync 步驟。
/// </summary>
[CommandAlias("GotoMapDemo")]
public class GotoMapDemo : Command, Command.IForceWait
{
    [ParameterAlias("ReturnScript")]
    public StringParameter ReturnScript;

    [ParameterAlias("ReturnLabel")]
    public StringParameter ReturnLabel;

    [ParameterAlias("Scene")]
    public StringParameter SceneName = "DemoMap";

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("[GotoMapDemo] ▶ ExecuteAsync 開始");

        // 1. 隱藏繼續輸入提示
        var continueUI = GameObject.FindObjectOfType<ContinueInputUI>();
        if (continueUI != null) continueUI.Visible = false;
        Debug.Log($"[GotoMapDemo] ContinueUI: {(continueUI != null ? "找到並隱藏" : "null，跳過")}");

        // 2. 停止腳本播放
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        if (scriptPlayer == null) { Debug.LogError("[GotoMapDemo] IScriptPlayer 為 null！"); return; }
        scriptPlayer.Stop();
        Debug.Log("[GotoMapDemo] ScriptPlayer.Stop() 完成");

        // 3. 寫入返回點變數
        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars == null) { Debug.LogError("[GotoMapDemo] ICustomVariableManager 為 null！"); }
        else
        {
            if (Assigned(ReturnScript)) vars.SetVariableValue("NextScript", ReturnScript.Value);
            if (Assigned(ReturnLabel))  vars.SetVariableValue("NextLabel",  ReturnLabel.Value);
            Debug.Log($"[GotoMapDemo] 變數寫入：NextScript={ReturnScript?.Value}  NextLabel={ReturnLabel?.Value}");
        }

        // 4. 關閉 Naninovel 相機
        var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCamera != null) naniCamera.enabled = false;
        Debug.Log($"[GotoMapDemo] NaniCamera: {(naniCamera != null ? "已關閉" : "null，跳過")}");

        // 5. 載入地圖場景
        var targetScene = Assigned(SceneName) ? SceneName.Value : "DemoMap";
        Debug.Log($"[GotoMapDemo] 即將載入場景：'{targetScene}'");
        await SceneManager.LoadSceneAsync(targetScene);
        Debug.Log("[GotoMapDemo] 場景載入完成");
    }
}
