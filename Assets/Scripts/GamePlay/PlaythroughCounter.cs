using UnityEngine;

/// <summary>
/// 幾周目。給存讀檔格子顯示用。
///
/// ★ 怎麼算 ★
/// 每看完一個結局 +1，第一輪是「第 1 周目」。同一個結局看兩次也算兩輪——
/// 這跟 <see cref="EndingRecord"/> 不一樣，那份只收「拿過哪幾種結局」，重複的不算。
/// 開新遊戲不算：中途放棄重開不是一輪。
///
/// ★ 為什麼不直接用 EndingRecord.Count ★
/// 見上。而且數字要在存檔當下記進存檔（見 SaveSlotInfo），不然讀檔畫面上所有
/// 舊存檔會一起跟著現在的周目變。
/// </summary>
public static class PlaythroughCounter
{
    const string Key = "WC/EndingsSeen/v1";

    /// <summary>上一個算進去的結局。結局演完都會 @exitToTitle，所以同一個 id 在回標題前
    /// 又來一次，只會是玩家倒帶（rollback）重看，不算新的一輪。</summary>
    static string lastCounted;

    /// <summary>看完的結局次數。</summary>
    public static int EndingsSeen
    {
        get
        {
            // 這個計數是後來才加的：之前就打過結局的人沒有這個 key，
            // 用已收集的結局數墊底，至少不會顯示成第 1 周目。
            if (!PlayerPrefs.HasKey(Key))
                return EndingRecord.Count;
            return PlayerPrefs.GetInt(Key, 0);
        }
    }

    /// <summary>目前是第幾周目（從 1 開始）。</summary>
    public static int Current => EndingsSeen + 1;

    /// <summary>由 AchievementManager 在結局成就觸發時呼叫；非結局的 id 會被忽略。</summary>
    public static void OnEndingReached (string achievementId)
    {
        if (!EndingRecord.IsEnding(achievementId)) return;
        if (achievementId == lastCounted) return;

        lastCounted = achievementId;
        PlayerPrefs.SetInt(Key, EndingsSeen + 1);
        PlayerPrefs.Save();
        Debug.Log($"[PlaythroughCounter] 看完結局 {achievementId}，接下來是第 {Current} 周目");
    }

    /// <summary>回標題之後就不是倒帶了，下次同一個結局要照算。</summary>
    public static void ForgetLastEnding () => lastCounted = null;
}
