using Naninovel;
using Naninovel.Commands;
using UnityEngine; // Debug.Log

[CommandAlias("setLilinaDone")]
public class SetLilinaDoneCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        DemoMapAutoProgress.LilinaSpokenThisSession = true;
        Debug.Log("[Lilina] 已標記為本次已拜訪，重開遊戲會重出現。");
        return UniTask.CompletedTask;
    }
}
