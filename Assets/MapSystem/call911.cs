using UnityEngine;
using UnityEngine.UI;

public class call911 : MonoBehaviour
{
    public string 劇本名 = "chapter1";

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(CallNaniScript);
    }

    void CallNaniScript()
    {
        var loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
            Debug.Log($"【call911】找到 SceneLoader，準備呼叫 GotoScript({劇本名})");
            loader.GotoScript(劇本名);
        }
        else
        {
            Debug.LogWarning("【call911】找不到 SceneLoader！");
        }
    }
}
