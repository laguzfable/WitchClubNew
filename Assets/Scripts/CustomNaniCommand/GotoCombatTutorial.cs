using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Tutorial")]
public class GotoCombatTutorial : Command, Command.IForceWait
{
    public StringParameter Background;
    public StringParameter ScriptName;
    public StringParameter Label;

    // ⭐⭐⭐「魔法」讓 Unity 強制刷新
    public static int magicTutorialPatch = 20240601;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("GotoCombatTutorial 指令被執行！");   
        // Engine.GetService<IUIManager>().SetUIVisibleWithToggle(false, false);

        Engine.GetService<IScriptPlayer>().SetSkipEnabled(false);
        Engine.GetService<IBackgroundManager>().GetActor(BackgroundsConfiguration.MainActorId).Visible = false;
        var printerMgr = Engine.GetService<ITextPrinterManager>();
        await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);

        if(!Assigned(ScriptName))
        {
            ScriptName = Engine.GetService<IScriptPlayer>().PlayedScript.Name;
        }

        DataService.Instance.scriptParameter = new ScriptParameter() { background = Background, scriptName = ScriptName, scriptLabel = Label };
        TutorialController.isTutorial = true;
        TutorialController.isTutorial2 = false;
        await SceneManager.LoadSceneAsync("CombatScene");
    }
}
