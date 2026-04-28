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
        // 1. 隱藏繼續輸入提示
        var continueUI = GameObject.FindObjectOfType<ContinueInputUI>();
        if (continueUI != null) continueUI.Visible = false;

        // 2. 停止腳本播放（但不 Reset，保留所有 Naninovel 變數）
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. 在 Naninovel 自訂變數中記下返回點
        var vars = Engine.GetService<ICustomVariableManager>();
        if (Assigned(ReturnScript))
            vars.SetVariableValue("NextScript", ReturnScript.Value);
        if (Assigned(ReturnLabel))
            vars.SetVariableValue("NextLabel", ReturnLabel.Value);

        // 4. 關閉 Naninovel 相機（地圖場景用自己的相機）
        var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCamera != null) naniCamera.enabled = false;

        // 5. 載入地圖場景
        var targetScene = Assigned(SceneName) ? SceneName.Value : "MapTest";
        await SceneManager.LoadSceneAsync(targetScene);
    }
}
