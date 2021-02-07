using Naninovel;
using Naninovel.UI;
using System.Collections;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetGame : MonoBehaviour
{
    void Awake()
    {
        if(FindObjectsOfType<ResetGame>().Length > 1)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        //Application.targetFrameRate = 30;
    }

    public KeyCode key;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(key))
        {
            SwitchStateToCombatModeAsync().Forget();
            //SceneManager.LoadSceneAsync("MainScene");
        }
    }

    public async UniTask SwitchStateToCombatModeAsync(CancellationToken cancellationToken = default)
    {
        // 1. Disable Naninovel input.
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = false;

        if(!Engine.Initialized)
        {
            return;
        }

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

        SceneManager.LoadSceneAsync("MainScene");
    }
}
