using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

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
    public void GoScene(string sceneName)
    {
        loadScene = true;
        loadingObj = Instantiate(loadingScreenPrefab, loadingScreenPrefab.transform.position, loadingScreenPrefab.transform.rotation);
        StartCoroutine(LoadNewScene(sceneName));
        audioSource.clip = ac;
        audioSource.Play();
    }

    IEnumerator LoadNewScene(string sceneName)
    {
        if(Engine.Initialized)
        {
            var isEnable = !sceneName.Equals("MainScene");
            var naniCamera = Engine.GetService<ICameraManager>().Camera;
            naniCamera.enabled = isEnable;// true;
            //var inputManager = Engine.GetService<IInputManager>();
            //inputManager.ProcessInput = true;
            GameObject.FindObjectOfType<ContinueInputUI>().Visible = isEnable;

            Toolbox.Instance.GetOrAddComponent<DataService>().paramArr = null;
            PlayerData.Instance.playerName = null;
        }
        

        // This line waits for 3 seconds before executing the next line in the coroutine.
        // This line is only necessary for this demo. The scenes are so simple that they load too fast to read the "Loading..." text.
        yield return new WaitForSeconds(1);

        // Start an asynchronous operation to load the scene that was passed to the LoadNewScene coroutine.
        AsyncOperation async;
        if (!is_multiScene) async = Application.LoadLevelAsync(sceneName);
        else async = Application.LoadLevelAdditiveAsync(sceneName);
        is_multiScene = false;

        // While the asynchronous operation to load the new scene is not yet complete, continue waiting until it's done.
        while (!async.isDone)
        {
            yield return null;
        }
        Destroy(loadingObj);  //加載場景時loading要消失才行
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}