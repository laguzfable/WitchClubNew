using Naninovel;
using Naninovel.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetGame : MonoBehaviour
{
    public string mainScene = "MainScene";


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

    public float pressToResetTime = 5f;

    float curPressTime = 0f;

    bool canReset = true;

    // Update is called once per frame
    void Update()
    {
        if(!canReset)
        {
            return;
        }
        if(Input.GetKeyUp(key))
        {
            SwitchStateToCombatModeAsync().Forget();
            //SceneManager.LoadSceneAsync("MainScene");
        }
        if(Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            curPressTime += Time.deltaTime;
            if(curPressTime >= pressToResetTime)
            {
                curPressTime = 0f;
                SwitchStateToCombatModeAsync().Forget();
            }
        }
        if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            curPressTime = 0f;
        }
    }

    public async UniTask SwitchStateToCombatModeAsync(AsyncToken asyncToken = default)
    {
        canReset = false;
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

        await SceneManager.LoadSceneAsync(mainScene);
        canReset = true;
    }
}
