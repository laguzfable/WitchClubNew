using Naninovel;
using Naninovel.Commands;
using UniRx.Async;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel.UI;

[CommandAlias("TrialEnd")]
public class GotoTrialCompleteScene : Command, Command.IForceWait
{

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
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;

        //Toolbox.Instance.GetOrAddComponent<DataService>().paramArr = new StringParameter[] { Background, Target, ScriptName, Label };
        await SceneManager.LoadSceneAsync("TrialCompleteScene");
        //return UniTask.CompletedTask;
    }
}
