using Naninovel;
using Naninovel.Commands;
using UnityEngine;

[CommandAlias("SaveReturnPoint")]
public class SaveReturnPoint : Command
{
    [ParameterAlias("script")] public StringParameter Script;
    [ParameterAlias("label")]  public StringParameter Label;

    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        var player = Engine.GetService<IScriptPlayer>();
        var curScript = player?.PlayedScript?.Name ?? "(null)";
        var targetScript = Assigned(Script) ? Script.Value : curScript;
        var targetLabel  = Assigned(Label)  ? Label.Value  : null;

        Debug.Log($"[RET] @SaveReturnPoint called. current='{curScript}', target='{targetScript}#{targetLabel}'");

        if (!string.IsNullOrEmpty(targetScript))
        {
            MapReturnPoint.Set(targetScript, targetLabel);
        }
        else
        {
            Debug.LogWarning("[RET] targetScript is empty. NOT saved.");
        }

        return UniTask.CompletedTask;
    }
}
