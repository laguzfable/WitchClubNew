using System.Collections.Generic;

/// <summary>
/// 結局 id → 名字。給蝕之聖典的右頁提示用。
///
/// ★ 名字的來源 ★
/// 跟 AchievementManager 那二十個 ACH_END_XX 常數的註解一致（那裡同時記了每個結局
/// 出自哪支劇本的哪個 label）。加新結局時三個地方要一起改：AchievementManager 的常數、
/// 這張表、還有 BranchNode.endingIds。
///
/// ★ 為什麼不從 Steam 拿名字 ★
/// SteamUserStats 的成就名稱要連上 Steam 才拿得到，離線／Editor／沒登入時會是空字串。
/// 聖典是離線也要能看的畫面，所以名字寫在這裡。
/// （代價是這份沒有多語言，之後要翻譯的話這裡換成查 managed text。）
/// </summary>
public static class EndingCatalog
{
    static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        { "ACH_END_01", "緋紅替身" },
        { "ACH_END_02", "純藍之冠" },
        { "ACH_END_03", "在妳身邊" },
        { "ACH_END_04", "穢血新神" },
        { "ACH_END_05", "深春" },
        { "ACH_END_06", "火中の幻影" },
        { "ACH_END_07", "輪迴の鑰匙" },
        { "ACH_END_08", "背棄世界" },
        { "ACH_END_09", "小精靈，飛走了" },
        { "ACH_END_10", "改寫命運" },
        { "ACH_END_11", "殉道" },
        { "ACH_END_12", "異端" },
        { "ACH_END_13", "唯一" },
        { "ACH_END_14", "壞滅" },
        { "ACH_END_15", "肅清" },
        { "ACH_END_16", "魔蝕" },
        { "ACH_END_17", "蘇生" },
        { "ACH_END_18", "終焉" },
        { "ACH_END_19", "墮星之主" },
        { "ACH_END_20", "私奔" },
    };

    /// <summary>結局名字。查不到就回 id 本身，至少看得出是哪一個。</summary>
    public static string NameOf (string endingId)
    {
        if (string.IsNullOrEmpty(endingId)) return "";

        string name;
        return Names.TryGetValue(endingId, out name) ? name : endingId;
    }

    /// <summary>玩家該看到的樣子：已收集顯示名字，還沒拿到的藏起來。</summary>
    public static string DisplayFor (string endingId, string hiddenLabel)
    {
        return EndingRecord.Has(endingId) ? NameOf(endingId) : hiddenLabel;
    }
}
