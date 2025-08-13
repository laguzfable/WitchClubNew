using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public static bool ReturnToTitleOnce = false;

    [Header("Scene Names")]
    public string titleSceneName = "Title";
    public string naniSceneName  = "NaniDialogTest";

    [Header("New Game")]
    public string newGameScript = "chapter0";
    public string newGameLabel  = "";

    [Header("UI Script + labels")]
    public string uiScript      = "System_UI";
    public string labelLoad     = "openLoad";
    public string labelSettings = "openSettings";
    public string labelGallery  = "openGallery";

    private void Awake ()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Title 按鈕
    public void StartNewGame() => GotoScript(newGameScript, newGameLabel);
    public void OpenLoadUI()    => PrepareUI(labelLoad);
    public void OpenConfigUI()  => PrepareUI(labelSettings);
    public void OpenGalleryUI() => PrepareUI(labelGallery);

    private void PrepareUI(string label)
    {
        ReturnToTitleOnce = true;
        GotoScript(uiScript, label);
    }

    // 保留舊名：GotoScript
    public void GotoScript(string scriptName, string label = null)
    {
        if (string.IsNullOrEmpty(scriptName)) return;

        var ds = DataService.Instance;
        if (ds == null) { Debug.LogError("[SceneLoader] DataService is null"); return; }

        ds.startScript = scriptName;
        ds.scriptParameter = !string.IsNullOrEmpty(label)
            ? new ScriptParameter { scriptName = scriptName, scriptLabel = label }
            : null;

        GoScene(naniSceneName);
    }

    public void GoScene(string sceneName) => StartCoroutine(LoadSceneRoutine(sceneName));

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return op;
    }

    // 若 UI 關閉要回標題，可呼叫這個
    public void ReturnToTitleIfPending()
    {
        if (!ReturnToTitleOnce) return;
        ReturnToTitleOnce = false;
        GoScene(titleSceneName);
    }
}
