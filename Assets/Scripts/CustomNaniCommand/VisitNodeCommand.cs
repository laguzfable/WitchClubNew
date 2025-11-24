using Naninovel;
using UnityEngine;

/// <summary>
/// Nani 用：@visitNode id:ch4_start
/// 會呼叫 VisitedNodeManager 紀錄節點。
/// </summary>
[CommandAlias("visitNode")]
public class VisitNodeCommand : Command
{
    [ParameterAlias("id"), RequiredParameter]
    public StringParameter NodeId;

    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var id = Assigned(NodeId) ? NodeId.Value : null;

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[VisitNodeCommand] NodeId is null or empty.");
            return UniTask.CompletedTask;
        }

        if (VisitedNodeManager.Instance == null)
        {
            Debug.LogWarning($"[VisitNodeCommand] VisitedNodeManager not found. Node: {id}");
            return UniTask.CompletedTask;
        }

        VisitedNodeManager.Instance.MarkVisited(id);
        return UniTask.CompletedTask;
    }
}
