#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

// Central place to unlock Steam achievements / push stats.
// Achievement API Names below must match exactly what is configured in the
// Steamworks partner site (App Admin > Stats & Achievements) for this AppID.
public class AchievementManager : MonoSingleton<AchievementManager>
{
    // 章節成就（VisitNodeCommand 依 @visitNode id:chapterN 自動觸發）
    // ACH_CHAPTER_0（序章）刻意不登記在 Steam 後台，VisitNodeCommand 會跳過 chapter0。
    public const string ACH_CHAPTER_0 = "ACH_CHAPTER_0";
    public const string ACH_CHAPTER_1 = "ACH_CHAPTER_1";
    public const string ACH_CHAPTER_2 = "ACH_CHAPTER_2";
    public const string ACH_CHAPTER_3 = "ACH_CHAPTER_3";
    public const string ACH_CHAPTER_4 = "ACH_CHAPTER_4";
    public const string ACH_CHAPTER_5 = "ACH_CHAPTER_5";
    public const string ACH_CHAPTER_6 = "ACH_CHAPTER_6";

    // 各路線「第五關／夜晚儀式」擊敗成就（CombatSystem.GameOver 自動觸發）
    public const string ACH_RITUAL_BLUE   = "RITUAL_BLUE";
    public const string ACH_RITUAL_RED    = "RITUAL_RED";
    public const string ACH_RITUAL_YELLOW = "RITUAL_YELLOW";
    public const string ACH_RITUAL_GREEN  = "RITUAL_GREEN";

    // 全符文收集（UnlockRuneCommand / UnlockAllRunesCommand 自動觸發）
    public const string ACH_ALL_RUNES = "ALL_RUNES";

    // 蝕之聖典解鎖：拿到 ACH_END_07「輪迴の鑰匙」的當下就給
    // （跟標題選單那顆按鈕的解鎖條件一致，見 TitleMenuUnlockInjector）
    public const string ACH_CODEX_OPEN = "CODEX_OPEN";

    // 女巫競技場開啟：拿過任一結局就算（見 MarkEndingAndUnlockArena，
    // 另外 TowerModeManager 開場也會補檢查一次，照顧更新前就已經有結局的存檔）
    public const string ACH_ARENA_OPEN = "ARENA_OPEN";

    // 女巫競技場最高樓層（TowerModeManager.RecordBestFloor 自動觸發）
    // 字串裡的 STREAK 是連勝時代的遺留，數字才是真正的樓層門檻（10 / 25 / 50）。
    // 這裡的字串必須跟 Steam 後台的 API 名稱逐字相同，上市後不要再動。
    public const string ACH_ARENA_FLOOR_10 = "STREAK_10";
    public const string ACH_ARENA_FLOOR_25 = "STREAK_25";
    public const string ACH_ARENA_FLOOR_50 = "STREAK_50";

    // 元素組合成就（PlayerController 首次打出對應元素組合牌時，透過 UnlockComboAchievement 自動觸發）
    public const string ACH_COMBO_RED_BLUE          = "ACH_COMBO_RED_BLUE";
    public const string ACH_COMBO_RED_GREEN         = "ACH_COMBO_RED_GREEN";
    public const string ACH_COMBO_RED_YELLOW        = "ACH_COMBO_RED_YELLOW";
    public const string ACH_COMBO_BLUE_GREEN        = "ACH_COMBO_BLUE_GREEN";
    public const string ACH_COMBO_BLUE_YELLOW       = "ACH_COMBO_BLUE_YELLOW";
    public const string ACH_COMBO_GREEN_YELLOW      = "ACH_COMBO_GREEN_YELLOW";
    public const string ACH_COMBO_RED_BLUE_GREEN    = "ACH_COMBO_RED_BLUE_GREEN";
    public const string ACH_COMBO_RED_BLUE_YELLOW   = "ACH_COMBO_RED_BLUE_YELLOW";
    public const string ACH_COMBO_RED_GREEN_YELLOW  = "ACH_COMBO_RED_GREEN_YELLOW";
    public const string ACH_COMBO_BLUE_GREEN_YELLOW = "ACH_COMBO_BLUE_GREEN_YELLOW";
    public const string ACH_COMBO_ALL_FOUR          = "ACH_COMBO_ALL_FOUR";

    // comboID = 組合中各卡牌 ID 加總（Red=1, Blue=2, Green=4, Yellow=8，對應 PlayerController.GetCardElement）
    private static readonly Dictionary<int, string> ComboAchievementMap = new Dictionary<int, string>
    {
        { 3,  ACH_COMBO_RED_BLUE },
        { 5,  ACH_COMBO_RED_GREEN },
        { 9,  ACH_COMBO_RED_YELLOW },
        { 6,  ACH_COMBO_BLUE_GREEN },
        { 10, ACH_COMBO_BLUE_YELLOW },
        { 12, ACH_COMBO_GREEN_YELLOW },
        { 7,  ACH_COMBO_RED_BLUE_GREEN },
        { 11, ACH_COMBO_RED_BLUE_YELLOW },
        { 13, ACH_COMBO_RED_GREEN_YELLOW },
        { 14, ACH_COMBO_BLUE_GREEN_YELLOW },
        { 15, ACH_COMBO_ALL_FOUR },
    };

    // 依 PlayedCardResult.GetComboID() 的結果解鎖對應的組合成就
    public void UnlockComboAchievement(int comboID)
    {
        if (ComboAchievementMap.TryGetValue(comboID, out string achId))
        {
            Unlock(achId);
        }
    }

    // 收集類成就
    // ACH_ALL_ENDINGS 由 MarkEndingAndUnlockArena 連動；另外兩個分別由 MonsterCodex.RecordSeen
    // 和 MonsterCodexView（回憶模式打開時，經 CGGalleryProgress.CheckAchievement）檢查。
    public const string ACH_ALL_ENDINGS      = "ALL_ENDINGS";
    public const string ACH_MONSTER_CODEX_FULL = "MONSTER_CODEX_FULL";
    public const string ACH_CG_GALLERY_FULL    = "CG_GALLERY_FULL";

    // 結局成就（由各 .nani 檔用 @achieve id:ACH_END_XX 觸發，編號對應結局清單）
    public const string ACH_END_01 = "ACH_END_01"; // 緋紅替身（chapter5blue #end_crimson）
    public const string ACH_END_02 = "ACH_END_02"; // 純藍之冠（chapter5blue #end_blue_crown）
    public const string ACH_END_03 = "ACH_END_03"; // 在妳身邊（chapter6red #zaiyushenbian）
    public const string ACH_END_04 = "ACH_END_04"; // 穢血新神（chapter6red #eupie_usurp）玩家坐視 → 優菲成神 → 最終幕梅爾穿象牙塔正裝
    public const string ACH_END_05 = "ACH_END_05"; // 深春（chapter4green #greennight_v2）
    public const string ACH_END_06 = "ACH_END_06"; // 火中的樂園（chapter4green #greennight_s2）
    public const string ACH_END_07 = "ACH_END_07"; // 輪迴の鑰匙（chapter5yellow #eclipse）
    public const string ACH_END_08 = "ACH_END_08"; // 背棄世界（chapter5yellow #realworld）
    public const string ACH_END_09 = "ACH_END_09"; // 小精靈，飛走了（chapter5yellow #success）
    public const string ACH_END_10 = "ACH_END_10"; // 改寫命運（euphie_end #end1）
    public const string ACH_END_11 = "ACH_END_11"; // 殉道（badend01）
    public const string ACH_END_12 = "ACH_END_12"; // 異端（badend12）
    public const string ACH_END_13 = "ACH_END_13"; // 唯一（badend13）
    public const string ACH_END_14 = "ACH_END_14"; // 壞滅（badend14）
    public const string ACH_END_15 = "ACH_END_15"; // 肅清（badend15）
    public const string ACH_END_16 = "ACH_END_16"; // 魔蝕（badend16）
    public const string ACH_END_17 = "ACH_END_17"; // 蘇生（badend17）
    public const string ACH_END_18 = "ACH_END_18"; // 終焉／失敗終焉（chapter5common #end 及子分支）
    public const string ACH_END_19 = "ACH_END_19"; // 墮星之主（chapter6red #mel_stands → mel_end #end1）
    public const string ACH_END_20 = "ACH_END_20"; // 私奔（syb_day05 / chapter4green #greennight_s2 → syb_end #end1）

    // 本地結局紀錄 +「女巫競技場開啟」「全結局」的連動。20 個結局全都經過 Unlock 這個
    // 入口，所以掛在這裡就好，不用去改每一份 .nani。
    private void MarkEndingAndUnlockArena(string achievementApiName)
    {
        // 周目要排在 Mark 前面：還沒有周目紀錄的人是拿 EndingRecord.Count 墊底，
        // 先 Mark 的話這次的結局會被算兩遍。
        PlaythroughCounter.OnEndingReached(achievementApiName);
        EndingRecord.Mark(achievementApiName);

        if (!EndingRecord.IsEnding(achievementApiName)) return;

        Unlock(ACH_ARENA_OPEN);

        // 輪迴の鑰匙＝蝕之聖典解鎖，系統給，不是玩家去點開才給。
        if (achievementApiName == ACH_END_07)
            Unlock(ACH_CODEX_OPEN);

        // 遞迴只會有一層：ACH_ARENA_OPEN / ACH_CODEX_OPEN / ACH_ALL_ENDINGS 都不是結局 id，
        // 第二次進來上面那個 return 就擋掉了。
        if (EndingRecord.AllCollected)
            Unlock(ACH_ALL_ENDINGS);
    }

#if !DISABLESTEAMWORKS
    private bool m_StatsValid;

    protected Callback<UserStatsReceived_t> m_UserStatsReceived;
    protected Callback<UserAchievementStored_t> m_AchievementStored;

    private void Start()
    {
        RequestStats();
    }

    public void RequestStats()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[AchievementManager] SteamManager not initialized, cannot request stats.");
            return;
        }

        if (m_UserStatsReceived == null)
            m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        if (m_AchievementStored == null)
            m_AchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

        SteamUserStats.RequestCurrentStats();
    }

    private void OnUserStatsReceived(UserStatsReceived_t pCallback)
    {
        if (pCallback.m_nGameID != (ulong)SteamUtils.GetAppID())
            return;

        if (pCallback.m_eResult == EResult.k_EResultOK)
        {
            m_StatsValid = true;
            Debug.Log("[AchievementManager] Stats received from Steam.");
        }
        else
        {
            Debug.LogWarning("[AchievementManager] RequestCurrentStats failed: " + pCallback.m_eResult);
        }
    }

    private void OnAchievementStored(UserAchievementStored_t pCallback)
    {
        Debug.Log($"[AchievementManager] Achievement stored: {pCallback.m_rgchAchievementName}");
    }

    // Unlocks an achievement by its Steamworks API Name. Safe to call repeatedly.
    public void Unlock(string achievementApiName)
    {
        // 先留本地紀錄，再處理 Steam。底下那個 early return 在 Steam 沒就緒時
        // 會直接跳出，劇情完成度不能依賴它。
        MarkEndingAndUnlockArena(achievementApiName);

        if (!SteamManager.Initialized || !m_StatsValid)
        {
            Debug.LogWarning($"[AchievementManager] Cannot unlock '{achievementApiName}', stats not ready yet.");
            return;
        }

        if (SteamUserStats.GetAchievement(achievementApiName, out bool alreadyAchieved) && alreadyAchieved)
            return;

        SteamUserStats.SetAchievement(achievementApiName);
        SteamUserStats.StoreStats();
    }

    public void SetStat(string statApiName, int value)
    {
        if (!SteamManager.Initialized || !m_StatsValid)
            return;

        SteamUserStats.SetStat(statApiName, value);
        SteamUserStats.StoreStats();
    }

    /// <summary>Steam 已經把這位玩家的成就狀態送回來了沒。沒有的話任何讀寫都會失敗。</summary>
    public bool StatsReady => m_StatsValid;

#if UNITY_EDITOR
    // Dev-only: wipe achievements/stats on Steam so they can be re-tested from scratch.
    [ContextMenu("Debug/Reset All Achievements And Stats")]
    public void DebugResetAll()
    {
        StartCoroutine(DebugResetAllRoutine());
    }

    /// <summary>
    /// 清空 Steam 上的成就與統計。
    ///
    /// ★ 為什麼要寫成協程 ★
    /// ResetAllStats 在「Steam 還沒把成就狀態送回來」時會直接失敗（回 false）。
    /// 剛進 Play mode 就按最容易踩到——舊版把回傳值丟掉、照樣印「已清空」，
    /// 所以看起來成功、實際上一個都沒清，就是「常常清不乾淨」的原因。
    /// 這裡改成：等 stats 就緒才動手、檢查回傳值、清完再等 Steam 回話一次才回報。
    /// </summary>
    public System.Collections.IEnumerator DebugResetAllRoutine()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[AchievementManager] Steam 沒初始化，沒有清。"
                           + "（Steam 客戶端要開著，steam_appid.txt 要在專案根目錄）");
            yield break;
        }

        // 等 Steam 把成就狀態送回來，最多 5 秒
        if (!m_StatsValid)
        {
            RequestStats();
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!m_StatsValid && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        if (!m_StatsValid)
        {
            Debug.LogError("[AchievementManager] Steam 一直沒回傳成就狀態，什麼都沒清。"
                         + "隔幾秒再按一次；還是不行的話多半是 Steam 客戶端沒開，"
                         + "或這個帳號沒有這個 App 的授權。");
            yield break;
        }

        // 先逐一清。ClearAchievement 是針對單一成就的，比 ResetAllStats 直接，
        // 而且哪一個沒清掉看得出來——ResetAllStats 失敗時只會回一個 false。
        var names = AllAchievementApiNames();
        var failed = names.Where(n => !SteamUserStats.ClearAchievement(n)).ToArray();
        if (failed.Length > 0)
            Debug.LogWarning("[AchievementManager] 這幾個 ClearAchievement 回 false（多半是後台沒建"
                           + "或名字拼錯）：" + string.Join("、", failed));

        // 統計也要歸零，不然靠統計累積的成就會馬上被 Steam 重新判定成達成
        if (!SteamUserStats.ResetAllStats(true))
            Debug.LogWarning("[AchievementManager] ResetAllStats 失敗（統計沒歸零），"
                           + "但上面的逐一清除可能已經生效。");

        if (!SteamUserStats.StoreStats())
        {
            Debug.LogError("[AchievementManager] StoreStats 失敗——清除還沒送到 Steam。");
            yield break;
        }

        // 送出去之後再要一次，確認 Steam 那邊真的變了才回報成功
        m_StatsValid = false;
        RequestStats();

        var wait = Time.realtimeSinceStartup + 5f;
        while (!m_StatsValid && Time.realtimeSinceStartup < wait)
            yield return null;

        if (!m_StatsValid)
        {
            Debug.LogWarning("[AchievementManager] 已經送出清除，但 Steam 還沒回話。"
                           + "用「印出目前成就狀態」確認一下。");
            yield break;
        }

        var left = 0;
        foreach (var apiName in AllAchievementApiNames())
            if (SteamUserStats.GetAchievement(apiName, out bool got) && got) left++;

        if (left == 0)
            Debug.Log("[AchievementManager] Steam 成就與統計都清乾淨了。"
                    + "（這是直接跟 Steam 要來的狀態，準的就是這個。"
                    + "Steam 用戶端的成就頁面／個人檔案是另外一份快取，會晚一點才更新）");
        else Debug.LogWarning($"[AchievementManager] 清完之後還有 {left} 個成就是解鎖狀態——"
                            + "多半是遊戲同時又發了一次（例如標題畫面重新判定），"
                            + "或 Steam 端還在同步。");
    }

    /// <summary>這個類別上所有 ACH_ 開頭的常數。清完之後拿來驗收。</summary>
    static string[] AllAchievementApiNames()
    {
        return typeof(AchievementManager)
            .GetFields(System.Reflection.BindingFlags.Public |
                       System.Reflection.BindingFlags.Static |
                       System.Reflection.BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("ACH_"))
            .Select(f => (string)f.GetRawConstantValue())
            .Distinct()
            .ToArray();
    }
#endif

#else
    // 非 Steam 平台一樣要記錄結局，劇情完成度才算得出來
    public void Unlock(string achievementApiName) { MarkEndingAndUnlockArena(achievementApiName); }
    public void SetStat(string statApiName, int value) { }
#endif
}
