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

        // 2. 只讓 Mel 和 Eupie 出現
        // 直接寫 PlayerPrefs（不依賴 StoryProgressManager 是否存在）
        PlayerPrefs.DeleteKey("Mel_Event_Day");
        PlayerPrefs.DeleteKey("Eupie_Event_Day");
        PlayerPrefs.SetInt("Vedia_Event_Day", 99);
        PlayerPrefs.SetInt("Nelly_Event_Day", 99);
        PlayerPrefs.Save();

        // 若 StoryProgressManager 存在，清掉 Mel/Eupie 的記憶體快取
        var spMgr = Object.FindObjectOfType<StoryProgressManager>();
        if (spMgr != null)
        {
            spMgr.ResetProgress("Mel");
            spMgr.ResetProgress("Eupie");
            // 注意：不對 Vedia/Nelly 呼叫 ResetProgress，避免把 99 從 PlayerPrefs 刪掉
        }
        // 再次確保 Vedia/Nelly 是 99（以防萬一）
        PlayerPrefs.SetInt("Vedia_Event_Day", 99);
        PlayerPrefs.SetInt("Nelly_Event_Day", 99);
        PlayerPrefs.Save();
        Debug.Log("[GotoMapDemo] 角色進度：Mel/Eupie=0, Vedia/Nelly=99");

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
