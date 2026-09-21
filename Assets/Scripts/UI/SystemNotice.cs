// Assets/Scripts/UI/SystemNotice.cs
//
// 在對話框裡插一句系統訊息（「筆記本記下了……」「蝕之聖典開放了」那類）。
//
// ★ 為什麼不用飄字提示 ★
// 飄字適合「數值變了」這種瞄一眼就好的事。但「某個模式開放了」「這條線索很重要」
// 是玩家需要真的讀到的，而且要停下來——所以走對話框，等他點一下才繼續。
//
// ★ 為什麼一定要用 await ★
// 底下是直接叫 Naninovel 的 @print。呼叫端必須是被劇本 await 的指令
// （像 @achieve、@note 那樣），這樣印出來的順序才會夾在正確的位置。
// 如果從沒被 await 的地方 fire-and-forget，會跟劇本自己的台詞搶同一個印字機，
// 症狀是訊息插在奇怪的地方、或整句被下一行洗掉。

using Naninovel;
using Naninovel.Commands;
using UnityEngine;

public static class SystemNotice
{
    /// <summary>
    /// 在對話框印一句系統訊息，等玩家點一下才往下走。
    /// 引擎還沒起來（例如在地圖或戰鬥場景）就自動跳過，不會炸。
    /// </summary>
    public static async UniTask InDialogue (string text, AsyncToken token = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (!Engine.Initialized)
        {
            Debug.Log($"[SystemNotice] 引擎沒起來，這句改成只寫進 log：{text}");
            return;
        }

        // reset:true → 自己獨佔一句，不會接在上一句台詞後面變成同一段；
        // author 留空 → 不掛在任何角色名下，玩家一看就知道是系統在講話。
        await new PrintText { Text = text, ResetPrinter = true }.ExecuteAsync(token);
        await WaitForReaderAsync(token);
    }

    /// <summary>
    /// 等玩家點一下。
    ///
    /// PrintText 自己不會等：它只是把腳本播放器的「正在等待輸入」旗標打開，真正停下來的是
    /// 播放器，而且是在指令與指令之間停。一個指令裡連印兩句以上時（例如 @guide 的一整段提示、
    /// 或 @achieve 先報結局名再報「競技場開放了」），播放器沒機會停，前一句會被後一句洗掉。
    ///
    /// 快轉時播放器會主動把旗標關掉，所以不會卡住；沒有對話框可等（旗標根本沒開）時也直接過。
    /// </summary>
    static async UniTask WaitForReaderAsync (AsyncToken token)
    {
        var player = Engine.GetService<IScriptPlayer>();
        if (player == null) return;

        while (player.WaitingForInput && token.EnsureNotCanceledOrCompleted())
            await AsyncUtils.DelayFrameAsync(1);
    }
}
