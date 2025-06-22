using Naninovel;
using Naninovel.Commands;
using UnityEngine;

[CommandAlias("TestLog")]
public class TestLogCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("TestLog Command executed!");
        return UniTask.CompletedTask;
    }
}
