using UnityEngine;

/// <summary>
/// 「哪些教學已經看完了」＋「要不要跳過看過的教學」這個設定。
///
/// 兩者都存在 PlayerPrefs：教學紀錄是玩家帳號層級的東西，不該跟著存檔跑
/// （讀舊檔重玩不代表玩家忘了怎麼打）。
///
/// 只有**完整跑完**才算看過——標記寫在 TutorialController 跑完所有步驟之後，
/// 中途離開戰鬥場景的話那行不會執行。
/// </summary>
public static class TutorialRecord
{
    /// <summary>基本戰鬥教學（chapter0，涅莉）。</summary>
    public const string Basic = "basic";

    /// <summary>符文教學（chapter3，may）。</summary>
    public const string Rune = "rune";

    const string SeenKeyPrefix = "WC/Tutorial/Seen/";
    const string SkipKey = "WC/Tutorial/SkipSeen";

    /// <summary>設定畫面的「跳過已觀看教學」。預設打勾。</summary>
    public static bool SkipSeenEnabled
    {
        get { return PlayerPrefs.GetInt(SkipKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(SkipKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool HasSeen(string tutorialId)
    {
        return !string.IsNullOrEmpty(tutorialId)
            && PlayerPrefs.GetInt(SeenKeyPrefix + tutorialId, 0) == 1;
    }

    public static void MarkSeen(string tutorialId)
    {
        if (string.IsNullOrEmpty(tutorialId)) return;
        if (HasSeen(tutorialId)) return;

        PlayerPrefs.SetInt(SeenKeyPrefix + tutorialId, 1);
        PlayerPrefs.Save();
        Debug.Log($"[TutorialRecord] 教學「{tutorialId}」已看完，之後可以跳過。");
    }

    /// <summary>劇本用的總判斷：設定有開、而且這個教學看過了，才跳過。</summary>
    public static bool ShouldSkip(string tutorialId)
    {
        return SkipSeenEnabled && HasSeen(tutorialId);
    }

    /// <summary>除錯用。PlayerPrefs.DeleteAll 也會一起清掉，這裡是給只想重置教學時用的。</summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SeenKeyPrefix + Basic);
        PlayerPrefs.DeleteKey(SeenKeyPrefix + Rune);
        PlayerPrefs.Save();
        Debug.Log("[TutorialRecord] 教學觀看紀錄已清空。");
    }
}
