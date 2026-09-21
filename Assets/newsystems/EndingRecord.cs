using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 結局收集紀錄（本地）。
///
/// 結局本來只透過 @achieve 寫進 Steam 成就，但 AchievementManager.Unlock 在
/// Steam 沒初始化、stats 還沒回來、或非 Steam 平台時會直接 return，所以那份
/// 紀錄不能拿來算完成度。這裡在同一個入口另外留一份 PlayerPrefs 紀錄，
/// 離線、Editor、還沒登入 Steam 的情況下都算得出來。
///
/// 只收 ACH_END_* 的 id——章節、符文、元素組合那些成就不算進劇情完成度。
/// </summary>
public static class EndingRecord
{
    private const string PlayerPrefsKey = "WC/Endings/v1";
    private const string EndingPrefix = "ACH_END_";

    private static HashSet<string> cache;

    private static HashSet<string> Unlocked
    {
        get
        {
            if (cache == null)
            {
                cache = new HashSet<string>();
                var raw = PlayerPrefs.GetString(PlayerPrefsKey, "");
                foreach (var id in raw.Split('|'))
                    if (!string.IsNullOrEmpty(id)) cache.Add(id);
            }
            return cache;
        }
    }

    /// <summary>這個成就 id 算不算「結局」。給 AchievementManager 判斷要不要連動開啟女巫競技場用。</summary>
    public static bool IsEnding(string achievementId)
    {
        return !string.IsNullOrEmpty(achievementId) && achievementId.StartsWith(EndingPrefix);
    }

    /// <summary>由 AchievementManager.Unlock 呼叫；非結局的成就會被忽略。</summary>
    public static void Mark(string achievementId)
    {
        if (!IsEnding(achievementId)) return;
        if (!Unlocked.Add(achievementId)) return;   // 已經有了就不重複寫檔

        PlayerPrefs.SetString(PlayerPrefsKey, string.Join("|", Unlocked));
        PlayerPrefs.Save();
        Debug.Log($"[EndingRecord] 收集到結局 {achievementId}（目前 {Unlocked.Count} 個）");
    }

    public static bool Has(string achievementId)
    {
        return !string.IsNullOrEmpty(achievementId) && Unlocked.Contains(achievementId);
    }

    /// <summary>已收集的結局數。</summary>
    public static int Count { get { return Unlocked.Count; } }

    /// <summary>結局總數。加新結局時這裡跟 AchievementManager 的 ACH_END_XX 常數要一起改。</summary>
    public const int Total = 20;

    /// <summary>結局是不是全收集了。</summary>
    public static bool AllCollected { get { return Count >= Total; } }

    /// <summary>丟掉記憶體快取，下次查詢重新從 PlayerPrefs 讀。
    /// 有人繞過這個類別直接動 PlayerPrefs（例如 ProgressResetter 的 DeleteAll）之後要叫一次，
    /// 不然讀到的還是清掉前的那份。</summary>
    public static void InvalidateCache()
    {
        cache = null;
    }

    /// <summary>除錯用：清空紀錄。</summary>
    public static void Clear()
    {
        Unlocked.Clear();
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("[EndingRecord] 已清空結局紀錄");
    }
}
