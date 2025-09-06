using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Restroom")]
public class GotoRestRoomScene : Command, Command.IForceWait
{
    public StringParameter ScriptName;
    public StringParameter Label;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        var bgm = Engine.GetService<IBackgroundManager>();
        var printerMgr = Engine.GetService<ITextPrinterManager>();
        Debug.Log($"[GRS] Restroom called. Assigned? ScriptName={Assigned(ScriptName)}, Label={Assigned(Label)}");

        scriptPlayer.SetSkipEnabled(false);
        bgm.GetActor(BackgroundsConfiguration.MainActorId).Visible = false;
        await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);

        if (Assigned(ScriptName))
        {
            var sn = ScriptName.Value;
            var lb = Assigned(Label) ? Label.Value : null;
            DataService.Instance.afterChatScript = new ScriptParameter() { scriptName = sn, scriptLabel = lb };
            Debug.Log($"[GRS] afterChatScript <- {sn}#{lb}");
        }

        var canChat = Assigned(ScriptName).ToString();
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", canChat);
        Debug.Log($"[GRS] Set CanChat={canChat}");

        await SceneManager.LoadSceneAsync("RestRoom");
        Debug.Log("[GRS] Loaded 'RestRoom'.");
    }
}
