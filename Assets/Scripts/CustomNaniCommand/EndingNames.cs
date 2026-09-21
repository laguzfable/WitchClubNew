using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 結局編號 → 結局名。演完一個結局時，由 @achieve（UnlockAchievementCommand）在對話框報一句
/// 「（結局 11：殉道）　已收集 5 / 20」，玩家才知道自己剛剛拿到的是哪一個。
///
/// 名字跟 AchievementManager 裡 ACH_END_XX 的註解一致。加新結局時三張表要一起補。
/// </summary>
public static class EndingNames
{
    static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
    {
        { "ACH_END_01", "緋紅替身" },
        { "ACH_END_02", "純藍之冠" },
        { "ACH_END_03", "在妳身邊" },
        { "ACH_END_04", "穢血新神" },
        { "ACH_END_05", "深春" },
        { "ACH_END_06", "火中的樂園" },
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

    static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        { "ACH_END_01", "Crimson Stand-In" },
        { "ACH_END_02", "Crown of Pure Blue" },
        { "ACH_END_03", "By Your Side" },
        { "ACH_END_04", "The Tainted New God" },
        { "ACH_END_05", "Deep Spring" },
        { "ACH_END_06", "Paradise in Flames" },
        { "ACH_END_07", "Key of the Cycle" },
        { "ACH_END_08", "Turning from the World" },
        { "ACH_END_09", "The Fairy Flew Away" },
        { "ACH_END_10", "Fate Rewritten" },
        { "ACH_END_11", "Martyrdom" },
        { "ACH_END_12", "Heretic" },
        { "ACH_END_13", "The Only One" },
        { "ACH_END_14", "Ruin" },
        { "ACH_END_15", "Purge" },
        { "ACH_END_16", "Witch-Rot" },
        { "ACH_END_17", "Revival" },
        { "ACH_END_18", "The End" },
        { "ACH_END_19", "Lord of the Fallen Star" },
        { "ACH_END_20", "Elopement" },
    };

    static readonly Dictionary<string, string> Ja = new Dictionary<string, string>
    {
        { "ACH_END_01", "緋の身代わり" },
        { "ACH_END_02", "純藍の冠" },
        { "ACH_END_03", "あなたのそばに" },
        { "ACH_END_04", "穢れの新神" },
        { "ACH_END_05", "深き春" },
        { "ACH_END_06", "炎の中の楽園" },
        { "ACH_END_07", "輪廻の鍵" },
        { "ACH_END_08", "世界を捨てて" },
        { "ACH_END_09", "妖精は、飛び去った" },
        { "ACH_END_10", "運命を書き換える" },
        { "ACH_END_11", "殉教" },
        { "ACH_END_12", "異端" },
        { "ACH_END_13", "唯一" },
        { "ACH_END_14", "壊滅" },
        { "ACH_END_15", "粛清" },
        { "ACH_END_16", "魔蝕" },
        { "ACH_END_17", "蘇生" },
        { "ACH_END_18", "終焉" },
        { "ACH_END_19", "堕星の主" },
        { "ACH_END_20", "駆け落ち" },
    };

    /// <summary>演完一個結局時要講的那一句。不是結局 id 就回 null（@achieve 也會拿來判斷要不要講）。</summary>
    public static string Describe (string achievementId)
    {
        if (!EndingRecord.IsEnding(achievementId)) return null;

        var lang = GetLang();
        var table = lang.StartsWith("ja") ? Ja : lang.StartsWith("zh") ? Zh : En;
        if (!table.TryGetValue(achievementId, out var name)) return null;

        // 編號取 id 最後兩碼（ACH_END_11 → 11），沒解析成功就不顯示編號。
        var number = achievementId.Substring(achievementId.Length - 2).TrimStart('0');
        var collected = EndingRecord.Count;
        var total = EndingRecord.Total;

        if (lang.StartsWith("ja"))
            return $"（エンディング {number}：{name}）　収集 {collected} / {total}";
        if (lang.StartsWith("zh"))
            return $"（結局 {number}：{name}）　已收集 {collected} / {total}";
        return $"(Ending {number}: {name})　Collected {collected} / {total}";
    }

    // 跟 RuneEnTranslation.GetLang 同一套：以引擎的 SelectedLocale 為準，引擎還沒起來才看 PlayerPrefs。
    static string GetLang ()
    {
        if (Engine.Initialized)
        {
            var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
            if (!string.IsNullOrEmpty(locale)) return locale.ToLower();
        }
        return PlayerPrefs.GetString("Language", "zh-TW").ToLower();
    }
}
