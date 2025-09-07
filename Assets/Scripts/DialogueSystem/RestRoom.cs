using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestRoom : MonoBehaviour
{
    [SerializeField] private GameObject selectPanel;
    [SerializeField] private Button chatBtn;

    private void Awake()
    {
        // 停用 Naninovel UI 輸入
        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = false;

        // 暫停劇情播放
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 啟用 RestRoom 攝影機，關閉 Naninovel 攝影機
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = true;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }

    private void Start()
    {
        // 根據變數決定是否可聊天
        bool canChat = false;
        Engine.GetService<ICustomVariableManager>().TryGetVariableValue("CanChat", out canChat);
        selectPanel.SetActive(canChat);
        chatBtn.interactable = canChat;
    }

    public void ChangeSkill()
    {
        SceneManager.LoadScene("ChangeRuneScene");
    }

    public void Sleep()
    {
        // 關閉休息室攝影機，啟用 Naninovel 攝影機，並顯示 ContinueInputUI
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = true;

        // 回到暫存劇情段落
        NaniBridgeUtility.GoBackToSavedStory();
    }

    public void Chat()
    {
        selectPanel.SetActive(true);
    }

    public void SelectCharacter(string scriptName, string label)
    {
        var scriptParameter = new ScriptParameter();
        scriptParameter.scriptName = scriptName;

        if (!string.IsNullOrEmpty(label))
            scriptParameter.scriptLabel = label;

        DataService.Instance.scriptParameter = scriptParameter;

        // 設定不能再重複聊天
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", "false");

        GotoNani();
    }

    private void GotoNani()
    {
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = true;

        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void BackToRoom()
    {
        selectPanel.SetActive(false);
    }
}
