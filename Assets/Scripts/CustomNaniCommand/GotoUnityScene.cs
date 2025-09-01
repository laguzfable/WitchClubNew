using Naninovel;
using Naninovel.UI;                 // ContinueInputUI 在這裡
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("GotoUnityScene")]     // 你的腳本就要寫：@GotoUnityScene sceneName:MapTest
public class GotoUnityScene : Command, Command.IForceWait
{
    [ParameterAlias("sceneName"), RequiredParameter]
    public StringParameter SceneName;

    public override async UniTask ExecuteAsync (AsyncToken token = default)
    {
        // 0) 參數檢查
        var target = Assigned(SceneName) ? SceneName.Value?.Trim() : null;
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[GotoUnityScene] 失敗：缺少 sceneName。");
            return;
        }
        Debug.Log($"[GotoUnityScene] >>> 開始切換 → '{target}'");

        // 1) 安全關閉可能擋互動的 UI（主選單可能沒有這些物件；全部做 null-check）
        try
        {
            var cont = Object.FindObjectOfType<ContinueInputUI>();
            if (cont != null) { cont.Visible = false; Debug.Log("[GotoUnityScene] 關閉 ContinueInputUI。"); }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GotoUnityScene] 關 UI 時出例外（忽略）：{ex.Message}");
        }

        // 2) 不做 Stop()/ResetState()，避免中斷指令鏈或清掉狀態

        // 3) 檢查 Build Settings
        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError($"[GotoUnityScene] 失敗：Scene '{target}' 不在 Build Settings 或不可載入。");
            return;
        }

        // 4) 直接用 Single 載入（相容你專案既有做法；最不容易被 UI/相機干擾）
        var op = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError("[GotoUnityScene] 失敗：LoadSceneAsync 回傳 null。");
            return;
        }
        await op;

        Debug.Log("[GotoUnityScene] <<< 完成。");
    }
}
