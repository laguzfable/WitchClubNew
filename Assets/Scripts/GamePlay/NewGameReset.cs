using UnityEngine;

/// <summary>
/// 開新遊戲時要清掉的「單周目進度」。
///
/// ★ 為什麼需要這個 ★
/// 標題的「新遊戲」只重置 Naninovel 的狀態（好感、旗標那些自訂變數），PlayerPrefs 一個都不碰。
/// 所以儀式進度、符文、卡片型態全部會跟著進二週目——第一個夜晚點涅莉播的是 yellow04
/// 而不是 yellow01，chapter4 的分歧一開場就全開，路線判定形同虛設。
///
/// ★ 什麼會被清、什麼不會 ★
/// 清的是「這一輪你做到哪」：儀式進度、符文、卡片型態、時段、返回點、事件預約、競技場當局狀態。
/// 不清的是「你這個人做到過什麼」：結局紀錄、怪物圖鑑、回憶CG（那份在 Naninovel 的 GlobalSave）、
/// 競技場最高樓層、教學看過紀錄、語言、玩家名字、聖典的節點解鎖。
///
/// 符文和卡片型態被清之前會先併進 Ever 那份（見 RunRecord），聖典還是看得到，
/// 不然開過新遊戲的人再進聖典，分歧會全部關上。
/// </summary>
public static class NewGameReset
{
    /// <summary>整串刪掉的 key。</summary>
    static readonly string[] PlainKeys =
    {
        "MapIsDay", "Game_IsDay", "IsDay",              // 時段
        "MapReturnPoint.Script", "MapReturnPoint.Label", // 主線返回點
        "WC/MapSpecialOverride/v1",                      // 地圖特殊事件預約
        "TowerMode.CurrentFloor", "TowerMode.IsActive",  // 女巫競技場的當局狀態
        "TowerMode.EquippedAmulets", "TowerMode.LilyReady",
        "DemoNextScript", "DemoNextLabel",
    };

    /// <summary>地圖事件進度的 key 尾巴，跟 StoryProgressManager 一致。</summary>
    static readonly string[] ProgressSuffixes = { "_Event_Day", "_Event_Night" };

    /// <summary>MapCharacterSpawner 的 characterName。加新角色要一起加。</summary>
    static readonly string[] Characters = { "Eupie", "Mel", "Nelly", "Vedia", "Sybil" };

    /// <summary>由標題的「新遊戲」呼叫。</summary>
    public static void Run ()
    {
        foreach (var key in PlainKeys)
            PlayerPrefs.DeleteKey(key);

        // 儀式／白天事件進度
        foreach (var character in Characters)
            foreach (var suffix in ProgressSuffixes)
                PlayerPrefs.DeleteKey(character + suffix);

        // 符文和卡片型態：先併進 Ever 再清，聖典才不會跟著失效
        foreach (var color in RuneCollection.Colors)
            RunRecord.PromoteAndClearRun(RuneCollection.KeyFor(color));

        foreach (var element in CardVariantUnlock.Elements)
        {
            RunRecord.PromoteAndClearRun(CardVariantUnlock.KeyFor(element));
            PlayerPrefs.DeleteKey("Equipped_" + element); // 裝備中的符文
        }

        PlayerPrefs.Save();

        // 記憶體快取也要丟，不然還是讀得到舊值
        if (StoryProgressManager.Instance != null)
            StoryProgressManager.Instance.InvalidateCache();
        MapSpecialOverride.ClearAll();

        Debug.Log("[NewGameReset] 單周目進度已清除（收集紀錄與曾經拿過的符文／卡片型態保留）");
    }
}
