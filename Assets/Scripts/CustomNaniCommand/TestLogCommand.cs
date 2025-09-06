using Naninovel;
using Naninovel.Commands;
using UnityEngine;

[CommandAlias("TestLog")]
public class TestLogCommand : Command
{
    [ParameterAlias("msg")] public StringParameter Message;

    public override UniTask ExecuteAsync (AsyncToken token = default)
    {
        var text = Assigned(Message) ? Message.Value : "(null)";
        Debug.Log($"[TestLog] {text}");
        return UniTask.CompletedTask;
    }
}
