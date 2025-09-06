using Naninovel;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NaniBridgeUtility
{
    /// <summary>
    /// 優先：DataService.scriptParameter → SceneLoader.GotoScript
    /// 後備：NextScript/NextLabel（舊案）
    /// 最後：回 Title
    /// </summary>
    public static void GoBackToSavedStory(string _ = null)
    {
        var ds = DataService.Instance;
        var param = ds != null ? ds.scriptParameter : null;

        if (param != null && !string.IsNullOrEmpty(param.scriptName))
        {
            Debug.Log($"[NBU] Use ds.scriptParameter: {param.scriptName}#{param.scriptLabel}");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.GotoScript(param.scriptName, param.scriptLabel);
            }
            else
            {
                Debug.LogWarning("[NBU] SceneLoader.Instance 為空，直接切 'NaniDialogTest'");
                ds.startScript = param.scriptName;
                SceneManager.LoadScene("NaniDialogTest");
            }
            return;
        }

        var vars = Engine.GetService<ICustomVariableManager>();
        string script = null, label = null;
        if (vars != null)
        {
            vars.TryGetVariableValue("NextScript", out script);
            vars.TryGetVariableValue("NextLabel", out label);
            Debug.Log($"[NBU] Fallback vars: NextScript='{script}', NextLabel='{label}'");
        }

        if (!string.IsNullOrEmpty(script))
        {
            vars?.SetVariableValue("NextScript", "");
            vars?.SetVariableValue("NextLabel", "");

            if (SceneLoader.Instance != null)
            {
                Debug.Log($"[NBU] -> SceneLoader.GotoScript('{script}','{label}')");
                SceneLoader.Instance.GotoScript(script, label);
            }
            else
            {
                Debug.LogWarning("[NBU] 無 SceneLoader，直接切 'NaniDialogTest'");
                if (ds != null)
                {
                    ds.startScript = script;
                    if (ds.scriptParameter == null) ds.scriptParameter = new ScriptParameter();
                    ds.scriptParameter.scriptName = script;
                    ds.scriptParameter.scriptLabel = label;
                }
                SceneManager.LoadScene("NaniDialogTest");
            }
            return;
        }

        Debug.LogWarning("[NBU] 找不到任何返回點，回 Title。");
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.GoScene(SceneLoader.Instance.titleSceneName);
        else
            SceneManager.LoadScene("Title");
    }
}
