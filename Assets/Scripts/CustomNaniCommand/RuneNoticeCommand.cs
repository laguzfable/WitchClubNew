using Naninovel;

/// <summary>
/// @runeNotice
/// 把戰鬥中拿到的符文，在對話框裡一則一則報出來（有顏色，並播提示音）。
///
/// 放在儀式戰打完回來的那個標籤底下就好；沒拿到符文（例如打輸了）就什麼都不會印。
/// 符文是在戰鬥場景給的，那裡沒有對話框，所以要等回到劇本才報——
/// 排隊的機制見 UnlockNotice。
/// </summary>
[CommandAlias("runeNotice")]
public class RuneNoticeCommand : Command
{
    public override async UniTask ExecuteAsync (AsyncToken token = default)
    {
        await UnlockNotice.FlushRunesAsync(token);
    }
}
