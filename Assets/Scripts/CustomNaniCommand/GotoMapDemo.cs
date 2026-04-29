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
    public StringParameter SceneName = "MapTest";

    /// <summary>IsDay:1 = 白天（Mel/Eupie），IsDay:0 = 晚上（Nelly/Vedia）</summary>
    [ParameterAlias("IsDay")]
    public IntegerParameter IsDay;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("[GotoMapDemo] ▶ ExecuteAsync 開始");

        bool isDay = !Assigned(IsDay) || IsDay.Value != 0;

        // 1. 返回點存到 PlayerPrefs
        if (Assigned(ReturnScript)) PlayerPrefs.SetString("DemoNextScript", ReturnScript.Value);
        if (Assigned(ReturnLabel))  PlayerPrefs.SetString("DemoNextLabel",  ReturnLabel.Value);
        PlayerPrefs.SetInt("MapIsDay", isDay ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[GotoMapDemo] IsDay={isDay}  ReturnScript={ReturnScript?.Value}  ReturnLabel={ReturnLabel?.Value}");

        // 3. Reset Naninovel 狀態（讓地圖相機能正常運作，與 @GotoUnityScene 一致）
        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();
        Debug.Log("[GotoMapDemo] ResetStateAsync 完成");

        // 3. 載入地圖場景
        var targetScene = Assigned(SceneName) ? SceneName.Value : "MapTest";
        Debug.Log($"[GotoMapDemo] 即將載入場景：'{targetScene}'");
        await SceneManager.LoadSceneAsync(targetScene);
        Debug.Log("[GotoMapDemo] 場景載入完成");
    }
}
