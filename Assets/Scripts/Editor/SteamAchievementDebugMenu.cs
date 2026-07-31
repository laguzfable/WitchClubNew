// Assets/Scripts/Editor/SteamAchievementDebugMenu.cs
// Usage: Tools > Witch Club > Reset Steam Achievements (or press Ctrl+Shift+R / Cmd+Shift+R)
// Only enabled while in Play mode, since AchievementManager is a runtime singleton.

using UnityEditor;
using UnityEngine;

public static class SteamAchievementDebugMenu
{
    [MenuItem("Tools/Witch Club/Reset Steam Achievements %#r")]
    static void ResetAchievements()
    {
        AchievementManager.Instance.DebugResetAll();
    }

    [MenuItem("Tools/Witch Club/Reset Steam Achievements %#r", true)]
    static bool ValidateResetAchievements()
    {
        return Application.isPlaying;
    }
}
