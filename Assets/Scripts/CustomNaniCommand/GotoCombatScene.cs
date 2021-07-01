using Naninovel;
using Naninovel.Commands;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Battle")]
public class GotoCombatScene : Command, Command.IForceWait
{
    public StringParameter Background;
    public StringParameter Target;
    public StringParameter ScriptName;
    public StringParameter Label;

    public async override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {

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
        Toolbox.Instance.GetOrAddComponent<DataService>().scriptParameter = new ScriptParameter() { background = Background, combatTarget = Target, scriptName = ScriptName, scriptLabel = Label };
        await SceneManager.LoadSceneAsync("CombatScene");
        //return UniTask.CompletedTask;
    }
}