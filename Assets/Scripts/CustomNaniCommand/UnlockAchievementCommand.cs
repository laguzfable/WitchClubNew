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

    public override async UniTask ExecuteAsync(AsyncToken token = default)
    {
        if (!Assigned(AchievementId) || string.IsNullOrEmpty(AchievementId.Value))
        {
            Debug.LogWarning("[achieve] 未指定 id 參數");
            return;
        }

        // 解鎖前先記住狀態，才分得出「本來就有」和「剛剛才開」。
        var hadAnyEnding = EndingRecord.Count > 0;
        var hadKey = EndingRecord.Has(AchievementManager.ACH_END_07);

        AchievementManager.Instance.Unlock(AchievementId.Value);

        // 標題選單那兩顆是系統給的，玩家不會知道自己剛剛開了什麼——
        // 在對話框講一句，而且是在這裡講，不用去改 20 份結局腳本。
        // 這支指令是被劇本 await 的，所以印出來的順序一定在 @achieve 之後、
        // @exitToTitle 之前，不會跟腳本搶。
        if (!hadAnyEnding && EndingRecord.Count > 0)
            await SystemNotice.InDialogue("（女巫競技場開放了。回到標題就能進去。）", token);

        if (!hadKey && EndingRecord.Has(AchievementManager.ACH_END_07))
            await SystemNotice.InDialogue("（蝕之聖典開放了。回到標題就能翻開它。）", token);
    }
}
