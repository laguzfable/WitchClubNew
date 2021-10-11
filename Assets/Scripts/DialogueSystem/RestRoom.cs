using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestRoom : MonoBehaviour
{
    [SerializeField] GameObject selectPanel;

    [SerializeField] Button chatBtn;


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

    private void Start()
    {
        bool canChat = false;
        Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("CanChat", out canChat);
        selectPanel.SetActive(canChat);

        chatBtn.interactable = canChat;
    }

    public void ChangeSkill()
    {
        SceneManager.LoadScene("ChangeRuneScene");
    }

    public void Sleep()
    {
        var dataService = DataService.Instance;
        dataService.scriptParameter = dataService.afterChatScript;
        GotoNani();
    }

    public void Chat()
    {
        selectPanel.SetActive(true);
    }

    public void SelectCharacter(string scriptName, string label)
    {
        var scriptParameter = new ScriptParameter();
        scriptParameter.scriptName = scriptName;
        if(!string.IsNullOrEmpty(label))
        {
            scriptParameter.scriptLabel = label;
        }

        DataService.Instance.scriptParameter = scriptParameter;
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", "false");
        GotoNani();
    }

    void GotoNani()
    {
        var advCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        advCamera.enabled = false;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = true;
        GameObject.FindObjectOfType<ContinueInputUI>().Visible = true;
        // Engine.GetService<ICustomVariableManager>().SetVariableValue("PlayerName", PlayerData.Instance.playerName);
        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void BackToRoom()
    {
        selectPanel.SetActive(false);
    }
}