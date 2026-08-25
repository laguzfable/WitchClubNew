using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 「這一輪拿到的」和「曾經拿過的」兩份帳。
///
/// ★ 為什麼要兩份 ★
/// 符文和卡片型態同時扮演兩個角色，混在一起會互相打架：
///   ‧ 路線判定看它（RunesGreen()>=3 那些）→ 開新遊戲時必須歸零，
///     不然二週目一開場分歧就全開，路線判定形同虛設。
///   ‧ 蝕之聖典的節點假設你有它 → 不能歸零，不然開過新遊戲的人再進聖典，
///     chapter4 的分歧、chapter5yellow 的救援線全部關上，等於開新遊戲會弄壞聖典。
///
/// 所以拆開：Run 是「這一輪」，新遊戲清掉；Ever 是「你曾經打贏過」，永遠留著，
/// 跟結局紀錄、CG、怪物圖鑑同一類。進聖典節點時把 Ever 還原到 Run
/// （見 NodeButton），書頁上會標出來，玩家知道自己帶著什麼進去。
///
/// ★ 這不是虛擬符文 ★
/// Ever 記的是玩家真的打贏過的儀式，不是憑空給的數字。
/// </summary>
public static class RunRecord
{
    const string EverPrefix = "Ever_";

    // ============================================================
    //  寫入：拿到東西時兩份一起記
    // ============================================================

    /// <summary>把一個 id 加進某個 key 的逗號清單（Run 和 Ever 各一份）。</summary>
    public static void Add (string runKey, string id)
    {
        if (string.IsNullOrEmpty(runKey) || string.IsNullOrEmpty(id)) return;

        AddTo(runKey, id);
        AddTo(EverPrefix + runKey, id);
        PlayerPrefs.Save();
    }

    static void AddTo (string key, string id)
    {
        var list = Split(PlayerPrefs.GetString(key, ""));
        if (list.Contains(id)) return;

        list.Add(id);
        PlayerPrefs.SetString(key, string.Join(",", list.ToArray()));
    }

    // ============================================================
    //  讀取
    // ============================================================

    public static List<string> Split (string raw)
    {
        if (string.IsNullOrEmpty(raw)) return new List<string>();

        return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => x.Trim())
                  .Where(x => x.Length > 0)
                  .Distinct()
                  .ToList();
    }

    /// <summary>「曾經拿過」的數量。</summary>
    public static int CountEver (string runKey)
    {
        return Split(PlayerPrefs.GetString(EverPrefix + runKey, "")).Count;
    }

    // ============================================================
    //  搬移
    // ============================================================

    /// <summary>
    /// 把 Ever 還原到 Run（進聖典節點時用）。
    /// 用聯集而不是覆蓋：這一輪剛拿到、還沒進 Ever 的東西不該被吃掉。
    /// </summary>
    public static void RestoreEverToRun (string runKey)
    {
        var ever = Split(PlayerPrefs.GetString(EverPrefix + runKey, ""));
        if (ever.Count == 0) return;

        var run = Split(PlayerPrefs.GetString(runKey, ""));
        var merged = run.Union(ever).ToList();

        PlayerPrefs.SetString(runKey, string.Join(",", merged.ToArray()));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 把 Run 併進 Ever，然後清掉 Run（開新遊戲時用）。
    /// 先併再清，這樣這次改動之前就存在的舊存檔也不會平白損失紀錄。
    /// </summary>
    public static void PromoteAndClearRun (string runKey)
    {
        foreach (var id in Split(PlayerPrefs.GetString(runKey, "")))
            AddTo(EverPrefix + runKey, id);

        PlayerPrefs.DeleteKey(runKey);
        PlayerPrefs.Save();
    }
}
