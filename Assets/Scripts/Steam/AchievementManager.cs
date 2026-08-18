#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections.Generic;
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
    public const string ACH_CHAPTER_0 = "ACH_CHAPTER_0";
    public const string ACH_CHAPTER_1 = "ACH_CHAPTER_1";
    public const string ACH_CHAPTER_2 = "ACH_CHAPTER_2";
    public const string ACH_CHAPTER_3 = "ACH_CHAPTER_3";
    public const string ACH_CHAPTER_4 = "ACH_CHAPTER_4";
    public const string ACH_CHAPTER_5 = "ACH_CHAPTER_5";
    public const string ACH_CHAPTER_6 = "ACH_CHAPTER_6";

    // 各路線「第五關／夜晚儀式」擊敗成就（CombatSystem.GameOver 自動觸發）
    public const string ACH_RITUAL_BLUE   = "ACH_RITUAL_BLUE";
    public const string ACH_RITUAL_RED    = "ACH_RITUAL_RED";
    public const string ACH_RITUAL_YELLOW = "ACH_RITUAL_YELLOW";
    public const string ACH_RITUAL_GREEN  = "ACH_RITUAL_GREEN";

    // 全符文收集（UnlockRuneCommand / UnlockAllRunesCommand 自動觸發）
    public const string ACH_ALL_RUNES = "ACH_ALL_RUNES";

    // 蝕之聖典（劇情分歧地圖）第一次打開（BranchMapUI 顯示時自動觸發）
    public const string ACH_CODEX_OPEN = "ACH_CODEX_OPEN";

    // 女巫競技場開啟：拿過任一結局就算（見 MarkEndingAndUnlockArena，
    // 另外 TowerModeManager 開場也會補檢查一次，照顧更新前就已經有結局的存檔）
    public const string ACH_ARENA_OPEN = "ACH_ARENA_OPEN";

    // 女巫競技場最高樓層（TowerModeManager.RecordBestFloor 自動觸發）
    // ★ 常數名稱改成 FLOOR，但字串刻意維持舊的 ACH_ARENA_STREAK_*：那是 Steam 後台已經
    //   登記的 API 名稱，改字串等於變成三個全新的成就，已經解鎖的玩家會掉紀錄。
    //   要一起正名的話，Steam 後台的 API 名稱也要同步改。
    public const string ACH_ARENA_FLOOR_10 = "ACH_ARENA_STREAK_10";
    public const string ACH_ARENA_FLOOR_25 = "ACH_ARENA_STREAK_50";
    public const string ACH_ARENA_FLOOR_50 = "ACH_ARENA_STREAK_100";

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

    // 結局成就（由各 .nani 檔用 @achieve id:ACH_END_XX 觸發，編號對應結局清單）
    public const string ACH_END_01 = "ACH_END_01"; // 緋紅替身（chapter5blue #end_crimson）
    public const string ACH_END_02 = "ACH_END_02"; // 純藍之冠（chapter5blue #end_blue_crown）
    public const string ACH_END_03 = "ACH_END_03"; // 在妳身邊（chapter6red #zaiyushenbian）
    public const string ACH_END_04 = "ACH_END_04"; // 穢血新神（chapter6red #eupie_usurp）玩家坐視 → 優菲成神 → 最終幕梅爾穿象牙塔正裝
    public const string ACH_END_05 = "ACH_END_05"; // 深春（chapter4green #greennight_v2）
    public const string ACH_END_06 = "ACH_END_06"; // 火中の幻影（chapter4green #greennight_s2）
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

    // 本地結局紀錄 + 「女巫競技場開啟」的連動。20 個結局全都經過 Unlock 這個入口，
    // 所以掛在這裡就好，不用去改每一份 .nani。
    // 遞迴只會有一層：ACH_ARENA_OPEN 不是結局 id，第二次進來就不會再往下走。
    private void MarkEndingAndUnlockArena(string achievementApiName)
    {
        EndingRecord.Mark(achievementApiName);

        if (EndingRecord.IsEnding(achievementApiName))
            Unlock(ACH_ARENA_OPEN);
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

#if UNITY_EDITOR
    // Dev-only: wipe achievements/stats on Steam so they can be re-tested from scratch.
    [ContextMenu("Debug/Reset All Achievements And Stats")]
    public void DebugResetAll()
    {
        if (!SteamManager.Initialized)
            return;

        SteamUserStats.ResetAllStats(true);
        SteamUserStats.StoreStats();
        RequestStats();
        Debug.Log("[AchievementManager] All stats/achievements reset.");
    }
#endif

#else
    // 非 Steam 平台一樣要記錄結局，劇情完成度才算得出來
    public void Unlock(string achievementApiName) { MarkEndingAndUnlockArena(achievementApiName); }
    public void SetStat(string statApiName, int value) { }
#endif
}
