using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 「某個顏色的符文收集了幾個」的查詢。
///
/// 符文解鎖清單是 CombatSystem.TryUnlockRune 寫進 PlayerPrefs 的
/// UnlockedRunes_&lt;元素&gt;（逗號分隔的 id 清單），每條路線各 5 個。
/// 夜晚儀式打贏才會拿到，所以「有幾個符文」＝「做完幾場儀式」——
/// 結局判定改用這個，比好感更能代表玩家真的走完了那條線。
/// </summary>
public static class RuneCollection
{
    /// <summary>每條路線的符文總數。</summary>
    public const int PerColor = 5;

    /// <summary>指定顏色已解鎖的符文數。color 可以用 blue/red/yellow/green（大小寫不拘）。</summary>
    public static int Count (string color)
    {
        if (string.IsNullOrEmpty(color)) return 0;

        var key = "UnlockedRunes_" + Normalize(color);
        var raw = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(raw)) return 0;

        return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => x.Trim())
                  .Where(x => x.Length > 0)
                  .Distinct()
                  .Count();
    }

    /// <summary>這個顏色是不是全收了。</summary>
    public static bool IsComplete (string color) => Count(color) >= PerColor;

    /// <summary>PlayerPrefs 的 key。RunRecord 和聖典都要用同一個算法。</summary>
    public static string KeyFor (string color) => "UnlockedRunes_" + Normalize(color);

    /// <summary>四個顏色的 key。</summary>
    public static readonly string[] Colors = { "blue", "red", "yellow", "green" };

    /// <summary>「曾經拿過」的數量（跨周目，開新遊戲不會清）。</summary>
    public static int CountEver (string color) => RunRecord.CountEver(KeyFor(color));

    /// <summary>把曾經拿過的符文還原到這一輪（進聖典節點時用）。</summary>
    public static void RestoreEver ()
    {
        foreach (var color in Colors)
            RunRecord.RestoreEverToRun(KeyFor(color));
    }

    /// <summary>PlayerPrefs 的 key 是首字母大寫（元素列舉的 ToString），這裡統一轉過去。</summary>
    static string Normalize (string color)
    {
        var lower = color.Trim().ToLower();
        if (lower.Length == 0) return color;
        return char.ToUpper(lower[0]) + lower.Substring(1);
    }
}
