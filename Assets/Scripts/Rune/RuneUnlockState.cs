using System.Linq;
using UnityEngine;

// Shared check used by UnlockRuneCommand / UnlockAllRunesCommand to know
// whether every rune (blue/red/yellow/green/none, 26 total) has been unlocked.
public static class RuneUnlockState
{
    public static bool AreAllUnlocked()
    {
        return Count("UnlockedRunes_Blue") >= 5
            && Count("UnlockedRunes_Red") >= 5
            && Count("UnlockedRunes_Yellow") >= 5
            && Count("UnlockedRunes_Green") >= 5
            && Count("UnlockedRunes_None") >= 6;
    }

    private static int Count(string key) =>
        PlayerPrefs.GetString(key, "").Split(',').Count(s => s.Length > 0);
}
