using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("RuneTutorial")]
public class GotoCombatTutorial2 : Command, Command.IForceWait
{
    public StringParameter Background;
    public StringParameter ScriptName;
    public StringParameter Label;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        // Engine.GetService<IUIManager>().SetUIVisibleWithToggle(false, false);
        
        Engine.GetService<IScriptPlayer>().SetSkipEnabled(false);
        Engine.GetService<IBackgroundManager>().GetActor(BackgroundsConfiguration.MainActorId).Visible = false;
        var printerMgr = Engine.GetService<ITextPrinterManager>();
        await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);

        if(!Assigned(ScriptName))
        {
            ScriptName = Engine.GetService<IScriptPlayer>().PlayedScript.Name;
        }

        // PlayerData.Instance.playerName = Engine.GetService<ICustomVariableManager>().GetVariableValue("PlayerName");
        /*
        PlayerData.Instance.usingRuneIDs[(int)ECardElement.Red] = "艾妮(血系)";
        PlayerData.Instance.usingRuneIDs[(int)ECardElement.Green] = "樹女";
        PlayerData.Instance.usingRuneIDs[(int)ECardElement.Blue] = "赫菲";
        */
        DataService.Instance.scriptParameter = new ScriptParameter() { background = Background, scriptName = ScriptName, scriptLabel = Label };
        TutorialController.isTutorial = false;
        TutorialController.isTutorial2 = true;
        await SceneManager.LoadSceneAsync("CombatScene");
        //return UniTask.CompletedTask;
    }
}