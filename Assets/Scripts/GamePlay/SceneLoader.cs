using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;
using UniRx.Async;

public class SceneLoader : MonoBehaviour
{

    private bool loadScene = false;

    [SerializeField]
    GameObject loadingScreenPrefab;
    GameObject loadingObj;
    [SerializeField]
    AudioClip ac;
    AudioSource audioSource;
    //fungus切場景用
    [SerializeField]
    string defaultSceneName = "";
    //fungus加載場景用
    [SerializeField]
    List<string> sceneNameList;
    bool is_multiScene = false;
    [SerializeField]  //跳block測試加載用
    int nowSceneNum = 0;

    // Updates once per frame
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    //fungus用卸載場景
    public void DestroyFungusMultiSceneUse()
    {
        SceneManager.UnloadSceneAsync(sceneNameList[nowSceneNum-1]);
    }
    //fungus用加載場景
    public void FungusCallMultiSceneUse()
    {
        Debug.Log("FungusCallMultiSceneUse");
        is_multiScene = true;
        GoScene(sceneNameList[nowSceneNum]);
        nowSceneNum++;
    }
    //fungus用直接切場景
    public void FungusGoSceneUse()
    {
        Debug.Log("FungusGoSceneUse");
        GoScene(defaultSceneName);
    }

    public void GotoScript(string scriptName)
    {
        DataService.Instance.startScript = scriptName;
        GoScene("NaniDialogTest");
    }

    public void GoScene(string sceneName)
    {
        loadScene = true;
        loadingObj = Instantiate(loadingScreenPrefab, loadingScreenPrefab.transform.position, loadingScreenPrefab.transform.rotation);
        LoadNewSceneAsync(sceneName).Forget();
        audioSource.clip = ac;
        audioSource.Play();
    }

    async UniTaskVoid LoadNewSceneAsync(string sceneName)
    {
        if(Engine.Initialized)
        {
            var isEnable = !sceneName.Equals("MainScene");
            var naniCamera = Engine.GetService<ICameraManager>().Camera;
            naniCamera.enabled = isEnable;// true;
            //var inputManager = Engine.GetService<IInputManager>();
            //inputManager.ProcessInput = true;
            GameObject.FindObjectOfType<ContinueInputUI>().Visible = isEnable;

            DataService.Instance.scriptParameter = null;
            // PlayerData.Instance.playerName = null;
        }
        
        // !到底是誰做個多場景讀取還要去抄網路上的東西啦 2021/10/10改掉了 by K
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));

        await SceneManager.LoadSceneAsync(sceneName, is_multiScene? LoadSceneMode.Additive : LoadSceneMode.Single);

        is_multiScene = false;

        Destroy(loadingObj);  //加載場景時loading要消失才行
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}