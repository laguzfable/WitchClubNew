using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class RestRoom : MonoBehaviour
{
    [Header("可選，沒綁也不會爆")]
    [SerializeField] private GameObject selectPanel;
    [SerializeField] private Button chatBtn;

    private void Awake()
    {
        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI cont)
            cont.Visible = false;

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer?.Stop();

        var advCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCam) advCam.enabled = true;

        var naniCam = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCam) naniCam.enabled = false;

        Debug.Log("[RR ] Awake: disable Nani UI/camera, enable RestRoom camera.");
    }

    private void Start()
    {
        bool canChat = false;
        Engine.GetService<ICustomVariableManager>()?.TryGetVariableValue("CanChat", out canChat);

        if (selectPanel) selectPanel.SetActive(canChat);
        else Debug.LogWarning("[RR ] selectPanel 未指派（Inspector）。");

        if (chatBtn) chatBtn.interactable = canChat;
        else Debug.LogWarning("[RR ] chatBtn 未指派（Inspector）。");

        Debug.Log($"[RR ] Start: CanChat={canChat}");
    }

    public void ChangeSkill()
    {
        Debug.Log("[RR ] ChangeSkill -> Load 'ChangeRuneScene'");
        SceneManager.LoadScene("ChangeRuneScene");
    }

    public void Sleep() { StartCoroutine(SleepFlow()); }

    private IEnumerator SleepFlow()
    {
        Debug.Log("[RR ] Sleep clicked.");

        PreRestoreSync(); // 關休息室 Canvas + 還原 Nani 必要狀態

        var ui = Engine.GetService<IUIManager>();
        var trans   = ui != null ? ui.GetUI<ISceneTransitionUI>() : null;
        var loading = ui != null ? ui.GetUI<ILoadingUI>()        : null;

        // 先嘗試 Naninovel Capture；失敗則用 Fader 先蓋黑
        bool captured = false;
        if (trans != null)
        {
            var task = NaniTransitionCompat.CaptureAsync(trans);
            while (!task.IsCompleted) yield return null;
            captured = task.Result;
        }
        if (!captured)
        {
            Debug.LogWarning("[RR ] Transition Capture 失敗 -> fallback ScreenFader.FadeOut");
            var f = ScreenFader.Ensure();
            yield return f.FadeOutCoroutine(0.18f);
        }
        else Debug.Log("[RR ] Scene captured by SceneTransitionUI.");

        loading?.Show();

        // 依優先順序回去
        if (MapReturnPoint.HasValid())
        {
            Debug.Log($"[RR ] Using MapReturnPoint -> {MapReturnPoint.ScriptName}#{MapReturnPoint.Label}");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
            else
                SceneManager.LoadScene("NaniDialogTest");
            KillRestRoomDDOL();
            yield break;
        }

        var ds = DataService.Instance; var p = ds != null ? ds.scriptParameter : null;
        if (p != null && !string.IsNullOrEmpty(p.scriptName))
        {
            Debug.Log($"[RR ] Use ds.scriptParameter -> {p.scriptName}#{p.scriptLabel}");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(p.scriptName, p.scriptLabel);
            else
                SceneManager.LoadScene("NaniDialogTest");
            KillRestRoomDDOL();
            yield break;
        }

        Debug.LogWarning("[RR ] No return point. Go Title.");
        if (SceneLoader.Instance != null) SceneLoader.Instance.GoScene(SceneLoader.Instance.titleSceneName);
        else SceneManager.LoadScene("Title");
        KillRestRoomDDOL();
    }

    public void Chat()
    {
        if (selectPanel) selectPanel.SetActive(true);
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
        GotoNani();
    }

    private void GotoNani()
    {
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCamera != null) naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI cont)
            cont.Visible = true;

        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void BackToRoom()
    {
        if (selectPanel) selectPanel.SetActive(false);
        Debug.Log("[RR ] BackToRoom: close chat panel.");
    }

    // ===== 內部 =====
    void PreRestoreSync()
    {
        // 關掉休息室畫面（避免疊 UI）
        var canvasList = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvasList) c.enabled = false;
        var cgs = GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in cgs) { cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false; }
        Debug.Log("[RR ] pre-restore: disabled RestRoom canvases.");

        var naniCam = Engine.GetService<ICameraManager>()?.Camera;
        if (naniCam && !naniCam.enabled) { naniCam.enabled = true; Debug.Log("[RR ] restore: enable Nani camera."); }

        var bgm = Engine.GetService<IBackgroundManager>();
        var mainBg = bgm != null ? bgm.GetActor(BackgroundsConfiguration.MainActorId) : null;
        if (mainBg != null && !mainBg.Visible) { mainBg.Visible = true; Debug.Log("[RR ] restore: main background.Visible = true"); }

        var pm = Engine.GetService<ITextPrinterManager>();
        var printer = pm != null ? pm.GetActor(pm.DefaultPrinterId) : null;
        if (printer != null && !printer.Visible) { printer.Visible = true; Debug.Log("[RR ] restore: default printer visible (sync)"); }

        if (Time.timeScale != 1f) { Time.timeScale = 1f; Debug.Log("[RR ] restore: Time.timeScale = 1"); }
    }

    void KillRestRoomDDOL()
    {
        var s = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (!s.IsValid()) return;

        foreach (var go in s.GetRootGameObjects())
            if (go.GetComponentInChildren<RestRoom>(true) || go.name.Contains("RestButton"))
            {
                Debug.Log($"[RR ] destroy DDOL leftover: {go.name}");
                GameObject.Destroy(go);
            }
    }
}
