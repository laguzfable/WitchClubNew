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
        
        if(Assigned(ScriptName))
        {
            Toolbox.Instance.GetOrAddComponent<DataService>().afterChatScript = new ScriptParameter() { scriptName = ScriptName, scriptLabel = Label };
        }
        // PlayerData.Instance.playerName = Engine.GetService<ICustomVariableManager>().GetVariableValue("PlayerName");
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", "true");
        await SceneManager.LoadSceneAsync("RestRoom");
        //return UniTask.CompletedTask;
    }
    
}