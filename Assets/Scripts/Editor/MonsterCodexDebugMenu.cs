// Assets/Scripts/Editor/MonsterCodexDebugMenu.cs
// Usage: Tools > Witch Club > 怪物圖鑑 > ...
// 圖鑑解鎖紀錄存在 PlayerPrefs，Editor 跟 Play mode 共用同一份，所以不用進 Play 也能改。

using Hexe.UI;
using UnityEditor;
using UnityEngine;

public static class MonsterCodexDebugMenu
{
    [MenuItem("Tools/Witch Club/怪物圖鑑/全部解鎖")]
    static void UnlockAll ()
    {
        MonsterCodex.InvalidateCache();
        MonsterCodex.UnlockAll();
        Debug.Log($"[MonsterCodex] 已解鎖全部 {MonsterCodex.AllMobs.Count} 隻怪物");
    }

    [MenuItem("Tools/Witch Club/怪物圖鑑/清空解鎖紀錄")]
    static void ResetAll ()
    {
        MonsterCodex.ResetAll();
    }

    [MenuItem("Tools/Witch Club/怪物圖鑑/印出目前收錄清單")]
    static void DumpList ()
    {
        foreach (var mob in MonsterCodex.AllMobs)
            Debug.Log($"{(MonsterCodex.IsSeen(mob) ? "✔" : "✘")} {mob.name} — {mob.displayName}");
    }
}
