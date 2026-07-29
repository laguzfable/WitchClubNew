using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Naninovel.UI;
using Naninovel;
using Hexe.TowerMode;

public class GotoBattle : MonoBehaviour
{
    /*
    private void Awake()
    {   
        // 1. Disable Naninovel input.
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = false;

        GameObject.FindObjectOfType<ContinueInputUI>().Visible = false;

        // 2. Stop script player.
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. Reset state. // 這一條指令會使整個nani重設回初始狀態 而我們只是想暫停而已
        //var stateManager = Engine.GetService<IStateManager>();
        //await stateManager.ResetStateAsync();

        
        // 4. Switch cameras.
        var advCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        advCamera.enabled = true;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }
    */
    // Use this for initialization
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(()=>
        {
            // 高塔模式期間換符文完不用回休息室，交給 ChangeRuneSceneManager 接手往下一層走
            if (TowerModeManager.IsActive) return;
            SceneManager.LoadScene("RestRoom");
        });
    }
}
