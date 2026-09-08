using Naninovel;
using UnityEngine;

/// <summary>
/// 個人線分歧前的夜晚倒數。left 包含即將進入的今晚，不計白天地圖。
/// 第三章四夜、第四章分歧前兩夜；改動行程時須同步更新呼叫處與倒數檢查。
/// </summary>
[CommandAlias("guide")]
public class GuideCommand : Command
{
    [ParameterAlias("left"), RequiredParameter]
    public IntegerParameter Left;

    public override async UniTask ExecuteAsync (AsyncToken token = default)
    {
        if (!Assigned(Left) || Left.Value < 1 || Left.Value > 6)
        {
            Debug.LogError("[guide] 分歧前的剩餘夜晚需介於 1～6，並包含今晚。");
            return;
        }

        var left = Left.Value;
        var countLine = left == 1
            ? "星塵: 在您說出那個名字之前，只剩今夜是您的了。"
            : $"星塵: 在您說出那個名字之前，連同今夜，還有 {left} 個夜晚是您的。";
        await SystemNotice.InDialogue(countLine, token);

        // 5／4／2 各有自己的一句：同一句聽三次，提醒就變成雜訊。
        switch (left)
        {
            case 6:
                await SystemNotice.InDialogue("星塵: 白晝攢下的信賴，是夜裡點得著的柴。柴不夠，儀式就燒不起來。", token);
                await SystemNotice.InDialogue("星塵: 想與誰同行，就陪她走完三個夜晚的儀式。而儀式，要在戰鬥中贏下來才算走完。", token);
                break;
            case 5:
                await SystemNotice.InDialogue("星塵: 夜晚會一個一個熄掉，而您每次只能握住一隻手。", token);
                break;
            case 4:
                await SystemNotice.InDialogue("星塵: 您給出去的每一個夜晚，都是從另一個人那裡取走的。星星不會替您留著。", token);
                break;
            case 3:
                await SystemNotice.InDialogue("星塵: 心裡若已經有了名字，就別再分神。三場儀式只認同一個人，別人的溫柔補不進她那一頁。", token);
                break;
            case 2:
                await SystemNotice.InDialogue("星塵: 快到盡頭了。您走過的夜晚會亮著，沒走的那些，往後也不會再亮。", token);
                break;
            default:
                await SystemNotice.InDialogue("星塵: 請再確認一次：她的三場儀式，是否都已走完。白晝仍能增進信賴，卻換不回錯過的夜。", token);
                break;
        }
    }
}
