using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 讓「這一輪做到哪」跟著存檔一起走。
///
/// ★ 為什麼需要這個 ★
/// 好感、旗標那些是 Naninovel 的自訂變數，存檔會一起存、讀檔會一起回捲。
/// 但儀式進度、符文、卡片型態、時段、返回點這些是寫 PlayerPrefs 的，
/// 而 PlayerPrefs 是全域的、只有一份，跟存檔格無關。所以：
///   ‧ 讀舊檔回到打贏之前 → 劇本回去了，符文和夜晚進度還停在打贏之後，
///     RitualGate 會算出「好感 20 卻已經做完三場儀式」這種對不起來的狀態。
///   ‧ 存檔格 A 打完的符文，載入存檔格 B 也看得到。
///
/// 所以存檔時把這些 key 打包進存檔（GameStateMap），讀檔時整包還原回 PlayerPrefs。
/// 各處的讀寫都不用改，它們照樣讀 PlayerPrefs。
///
/// ★ 哪些收、哪些不收 ★
/// 收的是「這一輪你做到哪」——跟 <see cref="NewGameReset"/> 清的那一份是同一個概念。
/// 不收「你這個人做到過什麼」：Ever_ 那份、怪物圖鑑、結局紀錄、競技場最高樓層、
/// 教學看過、語言、玩家名字。那些本來就不該因為讀檔而倒退。
///
/// 女巫競技場的當局狀態（TowerMode.*）也刻意不收：那邊是另一個模式、有自己的
/// 進出流程，而且 IsActive／CurrentFloor 在記憶體裡另有一份 static 欄位，
/// 只改 PlayerPrefs 反而會讓兩邊對不起來。
///
/// ★ 舊存檔 ★
/// 沒有這包資料的舊存檔，讀檔時什麼都不做（PlayerPrefs 維持現狀），
/// 不會把玩家的進度洗掉。
/// </summary>
public static class RunSnapshot
{
    /// <summary>MapCharacterSpawner 的 characterName。跟 NewGameReset／SanctumLoan 那兩份一致。</summary>
    static readonly string[] Characters = { "Eupie", "Mel", "Nelly", "Vedia", "Sybil" };

    /// <summary>字串型的 key。</summary>
    static IEnumerable<string> StringKeys
    {
        get
        {
            // 符文：路線判定（RunesGreen()>=3 那些）直接看它，一定要跟著回捲
            foreach (var color in RuneCollection.Colors)
                yield return RuneCollection.KeyFor(color);
            yield return "UnlockedRunes_None";

            // 卡片型態與身上裝的
            foreach (var element in CardVariantUnlock.Elements)
            {
                yield return CardVariantUnlock.KeyFor(element);
                yield return "Equipped_" + element;
                yield return "EquippedCardVariant_" + element;
            }

            yield return "MapReturnPoint.Script";   // 主線返回點
            yield return "MapReturnPoint.Label";
            yield return "WC/MapSpecialOverride/v1"; // 地圖特殊事件預約
            yield return "enemyName";                // 正要打的那一隻
            yield return "DemoNextScript";
            yield return "DemoNextLabel";
        }
    }

    /// <summary>數字型的 key。跟字串分開處理：PlayerPrefs 用 GetString 讀 int 的 key
    /// 會拿到空字串，還原時就變成把它刪掉（SanctumLoan 也踩過這一腳）。</summary>
    static IEnumerable<string> IntKeys
    {
        get
        {
            // 儀式／白天事件進度
            foreach (var character in Characters)
            {
                yield return character + "_Event_Night";
                yield return character + "_Event_Day";
            }

            yield return "MapIsDay";                 // 時段
            yield return "Game_IsDay";
            yield return "IsDay";
        }
    }

    /// <summary>key 原本不存在時，快照裡記這個值（跟 SanctumLoan 同一套）。</summary>
    const int Absent = -1;

    /// <summary>把現在的進度收成一包。存檔時呼叫。</summary>
    public static RunSnapshotState Capture ()
    {
        var state = new RunSnapshotState();

        foreach (var key in StringKeys)
        {
            state.stringKeys.Add(key);
            state.stringValues.Add(PlayerPrefs.GetString(key, ""));
        }

        foreach (var key in IntKeys)
        {
            state.intKeys.Add(key);
            state.intValues.Add(PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : Absent);
        }

        return state;
    }

    /// <summary>把一包進度寫回去。讀檔時呼叫。存檔當下不存在的 key 會被刪掉——
    /// 「那時候還沒有這個東西」跟「那時候它是空的」對玩家是同一件事。</summary>
    public static void Apply (RunSnapshotState state)
    {
        if (state == null || !state.HasData) return;

        for (var i = 0; i < state.stringKeys.Count && i < state.stringValues.Count; i++)
        {
            var key = state.stringKeys[i];
            var value = state.stringValues[i];
            if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetString(key, value);
        }

        for (var i = 0; i < state.intKeys.Count && i < state.intValues.Count; i++)
        {
            var key = state.intKeys[i];
            var value = state.intValues[i];
            if (value == Absent) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetInt(key, value);
        }

        PlayerPrefs.Save();

        // 這兩個在記憶體裡另有一份快取，不叫醒它們的話還是讀得到舊值
        if (StoryProgressManager.Instance != null)
            StoryProgressManager.Instance.InvalidateCache();
        MapSpecialOverride.Reload();

        Debug.Log($"[RunSnapshot] 已還原這一輪的進度（字串 {state.stringKeys.Count} 筆、" +
                  $"數字 {state.intKeys.Count} 筆）");
    }
}

/// <summary>存進存檔裡的那一包。欄位要能被 Unity 的 JsonUtility 序列化。</summary>
[Serializable]
public class RunSnapshotState
{
    public List<string> stringKeys = new List<string>();
    public List<string> stringValues = new List<string>();
    public List<string> intKeys = new List<string>();
    public List<int> intValues = new List<int>();

    /// <summary>舊存檔裡沒有這一包時會是空的，那種情況一律不動 PlayerPrefs。</summary>
    public bool HasData => stringKeys.Count > 0 || intKeys.Count > 0;
}
