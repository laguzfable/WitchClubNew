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

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("[GotoMapDemo] ▶ ExecuteAsync 開始");

        // 1. 把返回點存到 PlayerPrefs（Reset 後 Naninovel 變數會清掉，改用 PlayerPrefs 保留）
        if (Assigned(ReturnScript)) PlayerPrefs.SetString("DemoNextScript", ReturnScript.Value);
        if (Assigned(ReturnLabel))  PlayerPrefs.SetString("DemoNextLabel",  ReturnLabel.Value);
        PlayerPrefs.SetInt("MapIsDay", 1); // Demo 固定白天
        PlayerPrefs.Save();
        Debug.Log($"[GotoMapDemo] PlayerPrefs 儲存：DemoNextScript={ReturnScript?.Value}  DemoNextLabel={ReturnLabel?.Value}");

        // 2. 重置所有角色進度為 0（確保 Demo 地圖上 4 個小人全部出現）
        var spMgr = Object.FindObjectOfType<StoryProgressManager>();
        if (spMgr != null)
        {
            foreach (var name in new[] { "Mei", "Vivia", "Euphie", "Nelly" })
                spMgr.ResetProgress(name);
            Debug.Log("[GotoMapDemo] 角色進度已重置");
        }

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
