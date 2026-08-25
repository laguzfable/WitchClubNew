// Assets/Scripts/Editor/TutorialDebugMenu.cs
// Usage: Tools > Witch Club > 教學 > ...
// 教學紀錄存在 PlayerPrefs，Editor 跟 Play mode 共用同一份，不用進 Play 也能改。

using UnityEditor;
using UnityEngine;

public static class TutorialDebugMenu
{
    [MenuItem("Tools/Witch Club/教學/清空觀看紀錄（兩個教學都會重播）")]
    static void ClearSeen ()
    {
        TutorialRecord.Clear();
    }

    [MenuItem("Tools/Witch Club/教學/標記為全部看過")]
    static void MarkAllSeen ()
    {
        TutorialRecord.MarkSeen(TutorialRecord.Basic);
        TutorialRecord.MarkSeen(TutorialRecord.Rune);
        Debug.Log("[Tutorial] 兩個教學都標記成看過了。");
    }

    [MenuItem("Tools/Witch Club/教學/印出目前狀態")]
    static void Dump ()
    {
        Debug.Log($"[Tutorial] 跳過已觀看教學={TutorialRecord.SkipSeenEnabled}  "
                + $"基本={(TutorialRecord.HasSeen(TutorialRecord.Basic) ? "看過" : "沒看過")}  "
                + $"符文={(TutorialRecord.HasSeen(TutorialRecord.Rune) ? "看過" : "沒看過")}");
    }
}
