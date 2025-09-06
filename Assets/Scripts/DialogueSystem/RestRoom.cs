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
        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI cont) cont.Visible = false;

        var player = Engine.GetService<IScriptPlayer>();
        player.Stop();

        var advCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCam) advCam.enabled = true;

        var naniCam = Engine.GetService<ICameraManager>().Camera;
        naniCam.enabled = false;

        Debug.Log("[RR ] Awake: disable Nani UI/camera, enable RestRoom camera.");
    }

    private void Start()
    {
        bool canChat = false;
        Engine.GetService<ICustomVariableManager>().TryGetVariableValue("CanChat", out canChat);
        selectPanel.SetActive(canChat);
        chatBtn.interactable = canChat;
        Debug.Log($"[RR ] Start: CanChat={canChat}");
    }

    public void ChangeSkill()
    {
        Debug.Log("[RR ] ChangeSkill -> Load 'ChangeRuneScene'");
        SceneManager.LoadScene("ChangeRuneScene");
    }

    public void Sleep()
    {
        Debug.Log("[RR ] Sleep clicked.");

        // 1) 先用 MapReturnPoint（主線返回點）
        if (MapReturnPoint.HasValid())
        {
            Debug.Log($"[RR ] Using MapReturnPoint -> {MapReturnPoint.ScriptName}#{MapReturnPoint.Label}");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
            }
            else
            {
                Debug.LogWarning("[RR ] SceneLoader.Instance == null, direct load Nani scene.");
                SceneManager.LoadScene("NaniDialogTest");
            }
            return;
        }

        // 2) 沒有主線返回點，退而求其次用 ds.scriptParameter（可能是 afterChat 或別處覆蓋）
        var ds = DataService.Instance;
        var p  = ds != null ? ds.scriptParameter : null;
        var snap = p != null ? $"{p.scriptName}#{p.scriptLabel}" : "(null)";
        Debug.Log($"[RR ] MapReturnPoint empty. ds.scriptParameter={snap}");

        if (p != null && !string.IsNullOrEmpty(p.scriptName))
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(p.scriptName, p.scriptLabel);
            else
            {
                Debug.LogWarning("[RR ] SceneLoader.Instance == null, direct load Nani scene.");
                SceneManager.LoadScene("NaniDialogTest");
            }
            return;
        }

        // 3) 最後保險
        Debug.LogWarning("[RR ] No any return point. Go Title.");
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.GoScene(SceneLoader.Instance.titleSceneName);
        else
            SceneManager.LoadScene("Title");
    }

    public void Chat()
    {
        selectPanel.SetActive(true);
        Debug.Log("[RR ] Chat open.");
    }

    public void SelectCharacter(string scriptName, string label)
    {
        Debug.Log($"[RR ] SelectCharacter('{scriptName}','{label}')");
        var ds = DataService.Instance;
        if (ds != null)
        {
            var q = new ScriptParameter { scriptName = scriptName };
            if (!string.IsNullOrEmpty(label)) q.scriptLabel = label;
            ds.scriptParameter = q;
            Debug.Log($"[RR ] ds.scriptParameter set -> {scriptName}#{label}");
        }

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.GotoScript(scriptName, label);
        else
        {
            Debug.LogWarning("[RR ] SceneLoader.Instance == null, direct load Nani scene.");
            SceneManager.LoadScene("NaniDialogTest");
        }
    }

    public void BackToRoom()
    {
        selectPanel.SetActive(false);
        Debug.Log("[RR ] BackToRoom: close chat panel.");
    }
}
