using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 在 Naninovel 腳本中解鎖 Steam 成就。
/// 用法：@achieve id:ACH_END_CRIMSON
/// </summary>
[CommandAlias("achieve")]
public class UnlockAchievementCommand : Command
{
    [ParameterAlias("id"), RequiredParameter]
    public StringParameter AchievementId;

    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        if (!Assigned(AchievementId) || string.IsNullOrEmpty(AchievementId.Value))
        {
            Debug.LogWarning("[achieve] 未指定 id 參數");
            return UniTask.CompletedTask;
        }

        AchievementManager.Instance.Unlock(AchievementId.Value);
        return UniTask.CompletedTask;
    }
}
