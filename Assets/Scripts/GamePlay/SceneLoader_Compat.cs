using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Naninovel;
using Naninovel.UI;

/// <summary>
/// 兼容用 SceneLoader（最小實作）。
/// - 提供 Instance 單例、title/nani 場景名
/// - GotoScript() 會把 DataService.startScript / scriptParameter 設好再切到 Nani 場景
/// - GoScene() 切任意 Unity 場景，並配合 ILoadingUI 顯示/隱藏
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public static bool ReturnToTitleOnce = false;

    /// <summary>剛透過 GotoScript（或戰鬥返回）明確指定過目標劇本時為 true，
    /// NaniScriptLoader_HEX 讀取後歸零。用來區分「玩家剛指定的去向」跟
    /// 「scriptParameter 的舊殘留」——前者必須優先於 MapReturnPoint，
    /// 不然點地圖事件會被還沒用掉的主線返回點蓋掉、直接跳回主線。</summary>
    public static bool ExplicitGotoPending = false;

    [Header("Scene Names")]
    public string titleSceneName = "Title";
    public string naniSceneName  = "NaniDialogTest";

    [Header("New Game (optional)")]
    public string newGameScript = "chapter0";
    public string newGameLabel  = "";

    [Header("Demo (optional)")]
    public string demoScript = "demo";
    public string demoLabel  = "";

    private void Awake ()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[SL*] Awake. title='{titleSceneName}', nani='{naniSceneName}'");
    }

    public void StartDemo() => GotoScript(demoScript, string.IsNullOrEmpty(demoLabel) ? null : demoLabel);

    public void GotoScript (string scriptName, string label = null)
    {
        if (string.IsNullOrEmpty(scriptName))
        {
            Debug.LogError("[SL*] GotoScript: scriptName is empty.");
            return;
        }

        var ds = DataService.Instance;
        if (ds == null)
        {
            Debug.LogError("[SL*] DataService.Instance is null.");
            return;
        }

        ds.startScript = scriptName;
        ds.scriptParameter = string.IsNullOrEmpty(label)
            ? new ScriptParameter { scriptName = scriptName }
            : new ScriptParameter { scriptName = scriptName, scriptLabel = label };

        ExplicitGotoPending = true;
        Debug.Log($"[SL*] GotoScript -> '{scriptName}#{label}' ; load Nani scene '{naniSceneName}'");
        GoScene(naniSceneName);
    }

    private bool _isLoading;

    public void GoScene (string sceneName)
    {
        // 同一幀可能有多個來源觸發切場（例如場景裡重複掛了熱鍵腳本），只接受第一個
        if (_isLoading)
        {
            Debug.LogWarning($"[SL*] GoScene('{sceneName}') ignored: another load in progress.");
            return;
        }
        Debug.Log($"[SL*] GoScene('{sceneName}')");
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine (string sceneName)
    {
        _isLoading = true;
        try
        {
            var ui = Engine.GetService<IUIManager>();
            var loading = ui != null ? ui.GetUI<ILoadingUI>() : null;
            loading?.Show();

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            loading?.Hide();
            Debug.Log($"[SL*] Loaded '{SceneManager.GetActiveScene().name}'");
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void ReturnToTitleIfPending()
    {
        Debug.Log($"[SL*] ReturnToTitleIfPending? {ReturnToTitleOnce}");
        if (!ReturnToTitleOnce) return;
        ReturnToTitleOnce = false;
        GoScene(titleSceneName);
    }
}
