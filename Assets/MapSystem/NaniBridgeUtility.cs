using Naninovel;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hexe.TowerMode;

public static class NaniBridgeUtility
{
    /// <summary>
    /// 回到暫存劇情：**先**用 MapReturnPoint（主線存點），
    /// 否則用 ds.scriptParameter，最後才回退到 NextScript/NextLabel。
    /// </summary>
    public static void GoBackToSavedStory (string targetSceneName = "NaniDialogTest")
    {
        // 高塔模式跑到一半來換符文，返回按鈕要接著往下一層走，不能回劇本
        if (TowerModeManager.IsActive)
        {
            TowerModeManager.ContinueToNextFloor();
            return;
        }

        var ds = DataService.Instance;

        // A) 最高優先：MapReturnPoint（來自 @SaveReturnPoint）
        if (MapReturnPoint.HasValid())
        {
            Debug.Log($"[NBU] Use MapReturnPoint: {MapReturnPoint.ScriptName}#{MapReturnPoint.Label}");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
            }
            else
            {
                if (ds != null)
                {
                    ds.startScript = MapReturnPoint.ScriptName;
                    ds.scriptParameter = new ScriptParameter {
                        scriptName = MapReturnPoint.ScriptName,
                        scriptLabel = MapReturnPoint.Label
                    };
                }
                SceneManager.LoadScene(targetSceneName);
            }
            return;
        }

        // B) 次優先：若有人已經指定 ds.scriptParameter（例如聊天/教學流程）
        if (ds != null && ds.scriptParameter != null && !string.IsNullOrEmpty(ds.scriptParameter.scriptName))
        {
            var p = ds.scriptParameter;
            Debug.Log($"[NBU] Use ds.scriptParameter: {p.scriptName}#{p.scriptLabel}");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(p.scriptName, p.scriptLabel);
            else
                SceneManager.LoadScene(targetSceneName);
            return;
        }

        // C) 最後：舊案的 NextScript/NextLabel 後備
        var vars = Engine.GetService<ICustomVariableManager>();
        string script = null, label = null;
        vars?.TryGetVariableValue("NextScript", out script);
        vars?.TryGetVariableValue("NextLabel",  out label);

        if (!string.IsNullOrEmpty(script))
        {
            Debug.Log($"[NBU] Fallback vars: NextScript='{script}', NextLabel='{label}'");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GotoScript(script, label);
            else
            {
                if (ds != null)
                {
                    ds.startScript = script;
                    ds.scriptParameter = new ScriptParameter { scriptName = script, scriptLabel = label };
                }
                SceneManager.LoadScene(targetSceneName);
            }

            // 清空舊變數，避免下次誤用
            vars?.SetVariableValue("NextScript", "");
            vars?.SetVariableValue("NextLabel", "");
            return;
        }

        // D) 實在沒有任何可回去的點：回 Title
        Debug.LogWarning("[NBU] No return point found. Back to Title.");
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.GoScene(SceneLoader.Instance.titleSceneName);
        else
            SceneManager.LoadScene("Title");
    }
}
