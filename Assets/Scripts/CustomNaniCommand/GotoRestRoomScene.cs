using Naninovel;
using Naninovel.Commands;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Restroom")]
public class GotoRestRoomScene : Command, Command.IForceWait
{
    public StringParameter ScriptName;
    public StringParameter Label;
    public async override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {

        // Engine.GetService<IUIManager>().SetUIVisibleWithToggle(false, false);

        var printerMgr = Engine.GetService<ITextPrinterManager>();
        await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);
        
        if(!Assigned(ScriptName))
        {
            ScriptName = Engine.GetService<IScriptPlayer>().PlayedScript.Name;
        }
        // PlayerData.Instance.playerName = Engine.GetService<ICustomVariableManager>().GetVariableValue("PlayerName");
        Toolbox.Instance.GetOrAddComponent<DataService>().afterChatScript = new ScriptParameter() { scriptName = ScriptName, scriptLabel = Label };
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", "true");
        await SceneManager.LoadSceneAsync("RestRoom");
        //return UniTask.CompletedTask;
    }
    
}