using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;

public static class NaniBridgeUtility
{
    private const string fallbackScript = "MainStory_Fallback";
    private const string fallbackLabel = ""; // 可設定預設標籤（如果你願意）

    /// <summary>
    /// 從 Naninovel 的變數中讀取暫存劇情跳接點，回到指定劇本段落。
    /// 建議用於：RestRoom 睡覺、戰鬥勝利、地圖支線結束等。
    /// </summary>
    public static void GoBackToSavedStory(string targetSceneName = "NaniDialogTest")
    {
        var vars = Engine.GetService<ICustomVariableManager>();

        string script = fallbackScript;
        string label = fallbackLabel;

        vars.TryGetVariableValue("NextScript", out script);
        vars.TryGetVariableValue("NextLabel", out label);

        if (string.IsNullOrEmpty(script))
        {
            Debug.LogWarning("[NaniBridgeUtility] NextScript 未設置，使用預設劇本。");
            script = fallbackScript;
        }

        var param = new ScriptParameter { scriptName = script };
        if (!string.IsNullOrEmpty(label))
            param.scriptLabel = label;

        DataService.Instance.scriptParameter = param;

        // 清空變數（避免誤用）
        vars.SetVariableValue("NextScript", "");
        vars.SetVariableValue("NextLabel", "");

        Debug.Log($"🌀 轉回 Naninovel → 劇本: {script}, 標籤: {label}");
        SceneManager.LoadScene(targetSceneName);
    }
}
