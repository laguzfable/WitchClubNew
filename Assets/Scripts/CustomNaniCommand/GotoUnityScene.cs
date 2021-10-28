using Naninovel;
using Naninovel.Commands;
using Naninovel.UI;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("GotoUnityScene")]
public class GotoUnityScene : Command, Command.IForceWait
{
    public StringParameter SceneName;

    public async override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GameObject.FindObjectOfType<ContinueInputUI>().Visible = false;

        // 2. Stop script player.
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. Reset state.
        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();

        // 4. Switch cameras.
        //var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        //advCamera.enabled = true;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;

        await SceneManager.LoadSceneAsync(SceneName.Value);
        //return UniTask.CompletedTask;
    }
    
}