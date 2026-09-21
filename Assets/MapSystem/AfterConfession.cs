using System.Collections.Generic;

/// <summary>
/// 告白之後的日常。
///
/// ★ 為什麼需要 ★
/// 夜晚的五場儀式是一條有頭有尾的線，最後一場（blue05／red05／yellow05／green05）
/// 就是告白場。做完之後 <see cref="MapCharacterSpawner"/> 會讓她從夜晚地圖上消失——
/// 那對「儀式」是對的，對「人」卻不對：主線後面還有好幾個夜晚地圖，
/// 剛告白完的對象卻整個人不見了，等於答應完就被收走。
///
/// 所以進度滿了之後改掛這支劇本：每支裡面有三段短短的日常，隨機挑一段演。
/// 它是走 specialEvents 的路子，點下去不推進任何進度，也不動好感——
/// 該漲的都在儀式裡漲完了，這裡純粹是甜的。
/// </summary>
public static class AfterConfession
{
    /// <summary>地圖上那顆 icon 的標籤，也是塞進 specialEvents 的事件名。</summary>
    public const string EventName = "告白後";

    /// <summary>
    /// 角色 → 告白後的劇本。角色名是 MapCharacterSpawner 的 characterName。
    /// 西碧兒也在這裡：她那五場 syb_day01～05 沒有戰鬥，但一樣是走完就消失，
    /// 而綠線後面還有好幾個地圖夜。沒列在這張表裡的角色維持原本的「做完就不出現」。
    /// </summary>
    static readonly Dictionary<string, string> Table = new Dictionary<string, string>
    {
        { "Eupie", "after_eupie" },
        { "Mel",   "after_mel"   },
        { "Nelly", "after_nelly" },
        { "Vedia", "after_vedia" },
        { "Sybil", "after_sybil" },
    };

    /// <summary>這個角色告白後要播哪一支；沒設定的話回 null。</summary>
    public static string ScriptFor (string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return null;
        return Table.TryGetValue(characterName, out var script) ? script : null;
    }
}
