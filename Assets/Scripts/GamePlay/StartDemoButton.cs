using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 掛在主選單的 DEMO 按鈕上。
/// 不依賴 SceneLoader，直接設好 DataService 再切換到 Nani 場景。
/// </summary>
[RequireComponent(typeof(Button))]
public class StartDemoButton : MonoBehaviour
{
    [SerializeField] string demoScript    = "demo";
    [SerializeField] string naniSceneName = "NaniDialogTest";

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        var ds = DataService.Instance;
        if (ds != null)
        {
            ds.startScript    = demoScript;
            ds.scriptParameter = new ScriptParameter { scriptName = demoScript };
        }
        SceneManager.LoadScene(naniSceneName);
    }
}
