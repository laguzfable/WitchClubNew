using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 從劇本進入休息室。可帶 ScriptName/Label 設定 afterChatScript（用於聊天分支），
/// 不會動主線返回點（MapReturnPoint）。舊版 Naninovel：AsyncToken + IForceWait。
/// 用法：@Restroom ScriptName:"yellow01" Label:"night1"
/// </summary>
[CommandAlias("Restroom")]
public class GotoRestRoomScene : Command, Command.IForceWait
{
    [ParameterAlias("ScriptName")] public StringParameter ScriptName;
    [ParameterAlias("Label")]      public StringParameter Label;

    public async override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        var bgm          = Engine.GetService<IBackgroundManager>();
        var printerMgr   = Engine.GetService<ITextPrinterManager>();

        Debug.Log($"[GRS] Restroom called. Assigned? ScriptName={Assigned(ScriptName)}, Label={Assigned(Label)}");

        // 關閉快轉、背景與文字框（跟你原始行為一致）
        scriptPlayer.SetSkipEnabled(false);
        if (bgm != null && bgm.GetActor(BackgroundsConfiguration.MainActorId) != null)
            bgm.GetActor(BackgroundsConfiguration.MainActorId).Visible = false;

        if (printerMgr != null && printerMgr.DefaultPrinterId != null)
            await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);

        // 若有帶腳本參數，設定 afterChatScript（給休息室聊天使用）
        if (Assigned(ScriptName))
        {
            var sn = ScriptName.Value;
            var lb = Assigned(Label) ? Label.Value : null;
            DataService.Instance.afterChatScript = new ScriptParameter() { scriptName = sn, scriptLabel = lb };
            Debug.Log($"[GRS] afterChatScript <- {sn}#{lb}");
        }

        // 決定是否能聊天
        var canChat = Assigned(ScriptName).ToString();
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", canChat);
        Debug.Log($"[GRS] Set CanChat={canChat}");

        await SceneManager.LoadSceneAsync("RestRoom");
        Debug.Log("[GRS] Loaded 'RestRoom'.");
    }
}
