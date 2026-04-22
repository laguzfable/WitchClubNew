// Assets/MapSystem/call911.cs
using UnityEngine;
using UnityEngine.UI;
using Naninovel;
using UnityEngine.SceneManagement;

public class call911 : MonoBehaviour
{
    public string 劇本名 = "chapter1";
    public string 回到小說場景 = "NaniDialogTest";

    bool _busy; Button _btn;

    void Start()
    {
        _btn = GetComponent<Button>();
        if (_btn != null) _btn.onClick.AddListener(CallNaniScript);
    }

    void OnDestroy()
    {
        if (_btn != null) _btn.onClick.RemoveListener(CallNaniScript);
    }

    void CallNaniScript()
    {
        Debug.Log($"★HEXE★ call911.CallNaniScript  劇本={劇本名}  busy={_busy}");
        if (_busy) { Debug.Log("【call911】忽略：上次切場尚未完成。"); return; }
        _busy = true;
        if (_btn) _btn.interactable = false;

        var loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
            Debug.Log($"【call911】SceneLoader → GotoScript({劇本名})");
            loader.GotoScript(劇本名);
            return;
        }

        Debug.LogWarning("【call911】找不到 SceneLoader，改走後備。");
        try
        {
            var vars = Engine.GetService<ICustomVariableManager>();
            if (vars != null)
            {
                vars.SetVariableValue("NextScript", 劇本名);
                vars.SetVariableValue("NextLabel",  "");
            }
            NaniBridgeUtility.GoBackToSavedStory(回到小說場景);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("【call911】後備失敗：" + ex.Message);
            try { SceneManager.LoadScene(回到小說場景); } catch {}
        }
    }
}
