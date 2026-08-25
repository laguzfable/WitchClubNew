// Assets/Scripts/Editor/SteamAchievementDebugMenu.cs
// Usage: Tools > Witch Club > Steam 成就 > ...
// 成就狀態在 Steam 雲端，不是 PlayerPrefs，所以一定要在 Play mode（Steam 已初始化）下才動得了。
// 本地那份「拿過哪些結局」是另一套（EndingRecord），清 Steam 不會連帶清掉，所以另外給一個選項。

#if !DISABLESTEAMWORKS

using System.Linq;
using System.Reflection;
using Steamworks;
using UnityEditor;
using UnityEngine;

public static class SteamAchievementDebugMenu
{
    [MenuItem("Tools/Witch Club/Steam 成就/清空全部成就（Steam）")]
    static void ResetSteam ()
    {
        var manager = RequireManager();
        if (manager == null) return;

        manager.DebugResetAll();
    }

    [MenuItem("Tools/Witch Club/Steam 成就/清空全部成就 + 本地結局紀錄")]
    static void ResetSteamAndLocal ()
    {
        var manager = RequireManager();
        if (manager == null) return;

        manager.DebugResetAll();
        EndingRecord.Clear(); // 不清的話下次跑到結局會被判定成「已經拿過」，成就不會重發
        Hexe.UI.TitleMenuUnlockInjector.Refresh();
    }

    [MenuItem("Tools/Witch Club/Steam 成就/全部清乾淨（Steam 成就 + 本地進度）")]
    static void ResetEverything ()
    {
        var manager = RequireManager();
        if (manager == null) return;

        if (!EditorUtility.DisplayDialog(
                "全部清乾淨",
                "會清掉 Steam 上的全部成就與統計、全部 PlayerPrefs"
                + "（事件進度、符文／卡片、競技場樓層、結局紀錄），以及回憶模式 CG。"
                + " 無法復原，要繼續嗎？",
                "清掉", "取消"))
            return;

        manager.DebugResetAll();          // Steam（雲端）
        ProgressResetter.ResetLocalProgress(); // 本地，跟 F10 同一份步驟
        Debug.Log("[SteamAchievement] Steam 成就與本地進度都清掉了。");
    }

    [MenuItem("Tools/Witch Club/Steam 成就/印出目前成就狀態")]
    static void DumpStatus ()
    {
        if (RequireManager() == null) return;

        foreach (var apiName in AllApiNames())
        {
            if (SteamUserStats.GetAchievement(apiName, out bool achieved))
                Debug.Log($"{(achieved ? "✔" : "✘")} {apiName}");
            else
                Debug.LogWarning($"?  {apiName} — Steam 查不到。後台沒建／拼錯，"
                               + "或這次是剛建出 AchievementManager、stats 還沒回來（隔一下再按一次）");
        }
    }

    // 成就 API 名稱就是 AchievementManager 上那一整排 ACH_ 開頭的 const，直接反射拿，
    // 免得這邊再抄一份清單、之後兩邊不同步。
    static string[] AllApiNames ()
    {
        return typeof(AchievementManager)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("ACH_"))
            .Select(f => (string)f.GetRawConstantValue())
            .Distinct()
            .ToArray();
    }

    static AchievementManager RequireManager ()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SteamAchievement] 要先進 Play mode，Steam 沒初始化就沒東西可清。");
            return null;
        }

        // AchievementManager 是 MonoSingleton：場景裡本來就不一定放得有實體，
        // 沒有的話 Instance 會自己 new 一個 DontDestroyOnLoad 物件出來並 RequestStats。
        var manager = AchievementManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[SteamAchievement] 拿不到 AchievementManager（遊戲正在關閉？）。");
            return null;
        }

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamAchievement] Steam 沒初始化 —— Steam 客戶端要開著，"
                           + "且 steam_appid.txt 在專案根目錄。");
            return null;
        }

        return manager;
    }
}

#endif
