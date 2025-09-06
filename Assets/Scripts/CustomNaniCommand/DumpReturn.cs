using Naninovel;
using Naninovel.Commands;
using UnityEngine;

[CommandAlias("DumpReturn")]
public class DumpReturn : Command
{
    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        var ds = DataService.Instance;
        var p  = ds != null ? ds.scriptParameter : null;
        var dsSnap = p != null ? $"{p.scriptName}#{p.scriptLabel}" : "(null)";

        var vars = Engine.GetService<ICustomVariableManager>();
        string vScript = null, vLabel = null;
        if (vars != null)
        {
            vars.TryGetVariableValue("NextScript", out vScript);
            vars.TryGetVariableValue("NextLabel",  out vLabel);
        }
        var varSnap = string.IsNullOrEmpty(vScript) ? "(empty)" : $"{vScript}#{vLabel}";

        var mrpSnap = MapReturnPoint.HasValid() ? $"{MapReturnPoint.ScriptName}#{MapReturnPoint.Label}" : "(null)";

        Debug.Log($"[DMP] MapReturnPoint={mrpSnap} | ds.scriptParameter={dsSnap} | Vars={varSnap}");
        return UniTask.CompletedTask;
    }
}
