using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// @gotoScript script:"scriptName" label:"labelName"
/// 透過 SceneLoader.GotoScript 跳轉，與地圖系統走同一條路。
/// </summary>
[CommandAlias("gotoScript")]
public class GotoScript : Command, Command.IForceWait
{
    [ParameterAlias("script")] public StringParameter Script;
    [ParameterAlias("label")]  public StringParameter Label;

    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var scriptName = Assigned(Script) ? Script.Value : null;
        var label      = Assigned(Label)  ? Label.Value  : null;

        if (string.IsNullOrEmpty(scriptName))
        {
            Debug.LogError("[gotoScript] script 參數不能為空。");
            return UniTask.CompletedTask;
        }

        if (SceneLoader.Instance != null)
        {
            Debug.Log($"[gotoScript] GotoScript('{scriptName}', '{label}')");
            SceneLoader.Instance.GotoScript(scriptName, label);
        }
        else
        {
            Debug.LogError("[gotoScript] SceneLoader.Instance 為 null。");
        }

        return UniTask.CompletedTask;
    }
}
