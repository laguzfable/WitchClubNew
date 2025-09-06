using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 劇本內存回跳點：@SaveReturnPoint script:"chapter3" label:"hallwayMorning"
/// 寫入 MapReturnPoint（主線返回點），不碰 DataService.scriptParameter。
/// </summary>
[CommandAlias("SaveReturnPoint")]
public class SaveReturnPoint : Command
{
    [ParameterAlias("script")] public StringParameter Script;
    [ParameterAlias("label")]  public StringParameter Label;

    public override UniTask ExecuteAsync (AsyncToken token = default)
    {
        var player = Engine.GetService<IScriptPlayer>();
        var curScript = player?.PlayedScript?.Name ?? "(null)";
        var targetScript = Assigned(Script) ? Script.Value : curScript;
        var targetLabel  = Assigned(Label)  ? Label.Value  : null;

        Debug.Log($"[RET] @SaveReturnPoint called. current='{curScript}', target='{targetScript}#{targetLabel}'");

        if (!string.IsNullOrEmpty(targetScript))
            MapReturnPoint.Set(targetScript, targetLabel);
        else
            Debug.LogWarning("[RET] targetScript is empty. NOT saved.");

        return UniTask.CompletedTask;
    }
}
