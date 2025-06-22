using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;

    [Header("Loading & Audio")]
    [SerializeField] private GameObject loadingScreenPrefab;
    private GameObject loadingObj;
    [SerializeField] private AudioClip ac;
    private AudioSource audioSource;

    [Header("Scene Control")]
    [SerializeField] private string defaultSceneName = "";
    [SerializeField] private List<string> sceneNameList;
    [SerializeField] private int nowSceneNum = 0;
    private bool isMultiScene = false;

    void Awake()
    {
        // 全局單例，只留一份，切場景不消失
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        // AudioSource 自動抓取或掛在同物件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && ac != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 多場景流程，卸載前一個 Additive 場景
    /// </summary>
    public void DestroyFungusMultiSceneUse()
    {
        if (nowSceneNum > 0 && nowSceneNum - 1 < sceneNameList.Count)
            SceneManager.UnloadSceneAsync(sceneNameList[nowSceneNum - 1]);
    }

    /// <summary>
    /// 以 Additive 模式載入多場景
    /// </summary>
    public void FungusCallMultiSceneUse()
    {
        Debug.Log("FungusCallMultiSceneUse");
        isMultiScene = true;
        GoScene(sceneNameList[nowSceneNum]);
        nowSceneNum++;
    }

    /// <summary>
    /// 跳轉預設場景
    /// </summary>
    public void FungusGoSceneUse()
    {
        Debug.Log("FungusGoSceneUse");
        GoScene(defaultSceneName);
    }

    /// <summary>
    /// 跳轉到 Naninovel 劇本
    /// </summary>
    public void GotoScript(string scriptName)
    {
        DataService.Instance.startScript = scriptName;
        GoScene("NaniDialogTest");
    }

    /// <summary>
    /// 主切場景接口
    /// </summary>
    public void GoScene(string sceneName)
    {
        // 產生 Loading 畫面（僅限一個）
        if (loadingObj == null && loadingScreenPrefab != null)
            loadingObj = Instantiate(loadingScreenPrefab, loadingScreenPrefab.transform.position, loadingScreenPrefab.transform.rotation);

        LoadNewSceneAsync(sceneName).Forget();

        // 播放音效
        if (audioSource == null && ac != null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (audioSource != null && ac != null)
        {
            audioSource.clip = ac;
            audioSource.Play();
        }
    }

    /// <summary>
    /// 精確清理主選單 UI，載入新場景
    /// </summary>
    async UniTaskVoid LoadNewSceneAsync(string sceneName)
    {
        // 只砍主選單 Canvas（需確保名字唯一！）
        var mainMenuCanvas = GameObject.Find("MainMenuCanvas");
        if (mainMenuCanvas != null)
        {
            Destroy(mainMenuCanvas);
        }

        // 只砍舊BG（如需）
        var oldBG = GameObject.Find("BG");
        if (oldBG != null) Destroy(oldBG);

        // Naninovel相關 UI 控制
        if (Engine.Initialized)
        {
            var isEnable = !sceneName.Equals("MainScene");
            var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
            if (naniCamera != null)
                naniCamera.enabled = isEnable;
            var continueInputUI = GameObject.FindObjectOfType<ContinueInputUI>();
            if (continueInputUI != null)
                continueInputUI.Visible = isEnable;

            DataService.Instance.scriptParameter = null;
        }

        // 避免切太快，至少等 1 秒
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));

        // 正確載入新場景
        await SceneManager.LoadSceneAsync(sceneName, isMultiScene ? LoadSceneMode.Additive : LoadSceneMode.Single);
        isMultiScene = false;

        // 移除 loading 畫面
        if (loadingObj != null) Destroy(loadingObj);
        loadingObj = null;
    }

    /// <summary>
    /// 關閉遊戲
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
}
