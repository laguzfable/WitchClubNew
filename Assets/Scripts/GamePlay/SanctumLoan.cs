using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蝕之聖典的「借還」：進節點時把曾經拿過的符文與卡片型態借給玩家，回標題時還回去。
///
/// ★ 為什麼要還 ★
/// 進聖典節點時會把 Ever 還原到「這一輪」的帳上（見 RunRecord），
/// 不然開過新遊戲的人進聖典會發現分歧全關。但那個帳是全域的、不分存檔，
/// 所以「二週目玩到一半 → 跑去聖典點一格 → 回標題繼續二週目」的話，
/// 二週目的符文數會被墊高成歷來最多的那個，chapter4 的分歧提早開。
///
/// 所以借之前先存一份快照，@exitToTitle 回標題時原封不動還回去。
/// 玩家在聖典裡看到的是完整的自己，回到正在跑的那一輪還是那一輪的自己。
///
/// ★ 存在 PlayerPrefs 而不是記憶體 ★
/// 玩家可能在聖典的節點裡直接關掉遊戲。下次開起來回標題時還要還得掉，
/// 純記憶體的話那份快照就永遠消失，帳目對不起來。
/// </summary>
public static class SanctumLoan
{
    const string ActiveKey = "WC/SanctumLoan/Active";
    const string SnapshotPrefix = "Loan_";

    /// <summary>目前是不是借出中。</summary>
    public static bool Active => PlayerPrefs.GetInt(ActiveKey, 0) == 1;

    /// <summary>快照要蓋住的 key：四色符文 + 四色卡片型態。</summary>
    static IEnumerable<string> Keys
    {
        get
        {
            foreach (var color in RuneCollection.Colors)
                yield return RuneCollection.KeyFor(color);

            foreach (var element in CardVariantUnlock.Elements)
                yield return CardVariantUnlock.KeyFor(element);
        }
    }

    /// <summary>
    /// 借出：先存快照，再把 Ever 還原到這一輪。
    /// 已經在借出中就不再存一次快照——那會把「這一輪原本的樣子」換成上一格借完的狀態。
    /// </summary>
    public static void Borrow ()
    {
        if (!Active)
        {
            foreach (var key in Keys)
                PlayerPrefs.SetString(SnapshotPrefix + key, PlayerPrefs.GetString(key, ""));

            PlayerPrefs.SetInt(ActiveKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[SanctumLoan] 已存下這一輪的符文／卡片型態快照");
        }

        RuneCollection.RestoreEver();
        CardVariantUnlock.RestoreEver();
    }

    /// <summary>還回去：把快照寫回「這一輪」的帳，然後把快照清掉。</summary>
    public static void Return ()
    {
        if (!Active) return;

        foreach (var key in Keys)
        {
            var snapshotKey = SnapshotPrefix + key;
            var value = PlayerPrefs.GetString(snapshotKey, "");

            // 原本沒有這個 key 的話要刪掉，不是寫一個空字串進去——
            // 有些地方是用 HasKey 判斷「有沒有初始化過」的。
            if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetString(key, value);

            PlayerPrefs.DeleteKey(snapshotKey);
        }

        PlayerPrefs.SetInt(ActiveKey, 0);
        PlayerPrefs.Save();
        Debug.Log("[SanctumLoan] 已把符文／卡片型態還原成進聖典之前的樣子");
    }

    /// <summary>丟掉快照（開新遊戲時用：整輪都重來了，沒有什麼要還的）。</summary>
    public static void Discard ()
    {
        foreach (var key in Keys)
            PlayerPrefs.DeleteKey(SnapshotPrefix + key);

        PlayerPrefs.SetInt(ActiveKey, 0);
        PlayerPrefs.Save();
    }
}
