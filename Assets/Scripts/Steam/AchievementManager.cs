#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

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

    // 結局成就（由各 .nani 檔用 @achieve id:ACH_END_XX 觸發，編號對應結局清單）
    public const string ACH_END_01 = "ACH_END_01"; // 緋紅替身（chapter5blue #end_crimson）
    public const string ACH_END_02 = "ACH_END_02"; // 純藍之冠（chapter5blue #end_blue_crown）
    public const string ACH_END_03 = "ACH_END_03"; // 在妳身邊（chapter6red #zaiyushenbian）
    public const string ACH_END_04 = "ACH_END_04"; // 穢血新神（chapter6red #eupie_newgod）
    public const string ACH_END_05 = "ACH_END_05"; // 深春（chapter4green #greennight_v2）
    public const string ACH_END_06 = "ACH_END_06"; // 火中の幻影（chapter4green #greennight_s2）
    public const string ACH_END_07 = "ACH_END_07"; // 輪迴の鑰匙（chapter5yellow #eclipse）
    public const string ACH_END_08 = "ACH_END_08"; // 背棄世界（chapter5yellow #realworld）
    public const string ACH_END_09 = "ACH_END_09"; // 小精靈，飛走了（chapter5yellow #success）
    public const string ACH_END_10 = "ACH_END_10"; // 改寫命運（euphie_end #end1）
    public const string ACH_END_11 = "ACH_END_11"; // 殉道（euphie_end #end2）
    public const string ACH_END_12 = "ACH_END_12"; // 異端（badend12）
    public const string ACH_END_13 = "ACH_END_13"; // 唯一（badend13）
    public const string ACH_END_14 = "ACH_END_14"; // 壞滅（badend14）
    public const string ACH_END_15 = "ACH_END_15"; // 肅清（badend15）
    public const string ACH_END_16 = "ACH_END_16"; // 魔蝕（badend16）
    public const string ACH_END_17 = "ACH_END_17"; // 蘇生（badend17）
    public const string ACH_END_18 = "ACH_END_18"; // 終焉／失敗終焉（chapter5common #end 及子分支）

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
    public void Unlock(string achievementApiName) { }
    public void SetStat(string statApiName, int value) { }
#endif
}
