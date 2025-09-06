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
        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI cont)
            cont.Visible = false;

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

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

    /// <summary>睡覺：先還原狀態/關休息室 UI，再切回 Naninovel。</summary>
    public async void Sleep()
    {
        Debug.Log("[RR ] Sleep clicked.");
        DDOLDumper.Dump("before-sleep");

        await PreRestore(); // 關掉休息室 Canvas + 還原 Nani 狀態

        // 1) 優先用 @SaveReturnPoint 存的主線返回點
        if (MapReturnPoint.HasValid())
        {
            Debug.Log($"[RR ] Using MapReturnPoint -> {MapReturnPoint.ScriptName}#{MapReturnPoint.Label}");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
            else
            {
                var ds = DataService.Instance;
                if (ds != null)
                {
                    ds.startScript = MapReturnPoint.ScriptName;
                    ds.scriptParameter = new ScriptParameter {
                        scriptName = MapReturnPoint.ScriptName,
                        scriptLabel = MapReturnPoint.Label
                    };
                }
                SceneManager.LoadScene("NaniDialogTest");
            }

            KillRestRoomDDOL();
            DDOLDumper.Dump("leaving-restroom");
            return;
        }

        // 2) 次選：若有 ds.scriptParameter 就依它
        var ds2 = DataService.Instance;
        var p   = ds2 != null ? ds2.scriptParameter : null;
        var snap = p != null ? $"{p.scriptName}#{p.scriptLabel}" : "(null)";
        Debug.Log($"[RR ] MapReturnPoint empty. ds.scriptParameter={snap}");

        if (p != null && !string.IsNullOrEmpty(p.scriptName))
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(p.scriptName, p.scriptLabel);
            else
                SceneManager.LoadScene("NaniDialogTest");

            KillRestRoomDDOL();
            DDOLDumper.Dump("leaving-restroom");
            return;
        }

        // 3) 沒任何返回資訊 -> 回 Title
        Debug.LogWarning("[RR ] No any return point. Go Title.");
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.GoScene(SceneLoader.Instance.titleSceneName);
        else
            SceneManager.LoadScene("Title");

        KillRestRoomDDOL();
        DDOLDumper.Dump("leaving-restroom");
    }

    public void Chat()
    {
        selectPanel.SetActive(true);
        Debug.Log("[RR ] Chat open.");
    }

    /// <summary>選角聊天：保持原資料流，寫 ds.scriptParameter 然後切回 Nani。</summary>
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

        GotoNani();
    }

    private void GotoNani()
    {
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI cont)
            cont.Visible = true;

        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void BackToRoom()
    {
        selectPanel.SetActive(false);
        Debug.Log("[RR ] BackToRoom: close chat panel.");
    }

    // ====== 內部：先關休息室 UI，還原 Nani 狀態 ======

    private async UniTask PreRestore()
    {
        // 關掉休息室整個 Canvas（避免 Overlay 殘留）
        var canvasList = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvasList) c.enabled = false;
        var cgs = GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in cgs) { cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false; }
        Debug.Log("[RR ] pre-restore: disabled RestRoom canvases.");

        // 還原 Nani 相機 / 背景 / 文字框 / TimeScale
        var naniCam = Engine.GetService<ICameraManager>().Camera;
        if (naniCam && !naniCam.enabled) { naniCam.enabled = true; Debug.Log("[RR ] restore: enable Nani camera."); }

        var bgm = Engine.GetService<IBackgroundManager>();
        var mainBg = bgm != null ? bgm.GetActor(BackgroundsConfiguration.MainActorId) : null;
        if (mainBg != null && !mainBg.Visible) { mainBg.Visible = true; Debug.Log("[RR ] restore: main background.Visible = true"); }

        var pm = Engine.GetService<ITextPrinterManager>();
        var printer = pm != null ? pm.GetActor(pm.DefaultPrinterId) : null;
        if (printer != null && !printer.Visible)
        {
            await printer.ChangeVisibilityAsync(true, 0f);
            Debug.Log("[RR ] restore: default printer visible");
        }

        if (Time.timeScale != 1f) { Time.timeScale = 1f; Debug.Log("[RR ] restore: Time.timeScale = 1"); }
    }

    private void KillRestRoomDDOL()
    {
        var s = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (!s.IsValid()) return;

        foreach (var go in s.GetRootGameObjects())
        {
            // 依實際專案命名可再收斂條件
            if (go.GetComponentInChildren<RestRoom>(true) || go.name.Contains("RestButton"))
            {
                Debug.Log($"[RR ] destroy DDOL leftover: {go.name}");
                GameObject.Destroy(go);
            }
        }
    }
}
