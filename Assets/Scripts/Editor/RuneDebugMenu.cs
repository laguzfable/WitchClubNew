// Assets/Scripts/Editor/RuneDebugMenu.cs
// Usage: Tools > Witch Club > 符文 > ...
// 符文存在 PlayerPrefs，Editor 跟 Play mode 共用同一份，不用進 Play 也能改。
//
// ★ 為什麼要有這個 ★
// 第四章的路線分歧看的是符文數（RunesBlue()>=3 那些），而符文只有打贏夜晚儀式才拿得到。
// 清過進度之後想直接測第四章或蝕之聖典，得先把儀式全部重打一次，太慢了。

using UnityEditor;
using UnityEngine;

public static class RuneDebugMenu
{
    [MenuItem("Tools/Witch Club/符文/全部解鎖（含「曾經拿過」）")]
    static void UnlockAll ()
    {
        foreach (var color in RuneCollection.Colors)
        {
            var key = RuneCollection.KeyFor(color);
            for (var i = 0; i <= RuneCollection.PerColor; i++)
                RunRecord.Add(key, $"{color}{i:00}"); // blue00～blue05，00 是預設那顆
        }

        Debug.Log("[符文] 四色全開，Ever 那份也一起寫了——蝕之聖典借得到。");
        Dump();
    }

    [MenuItem("Tools/Witch Club/符文/清空（連「曾經拿過」一起）")]
    static void ClearAll ()
    {
        foreach (var color in RuneCollection.Colors)
        {
            var key = RuneCollection.KeyFor(color);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey("Ever_" + key);
        }
        PlayerPrefs.Save();

        Debug.Log("[符文] 全部清空。");
        Dump();
    }

    [MenuItem("Tools/Witch Club/符文/印出目前狀態")]
    static void Dump ()
    {
        foreach (var color in RuneCollection.Colors)
            Debug.Log($"[符文] {color}：這一輪 {RuneCollection.Count(color)} 個，曾經拿過 {RuneCollection.CountEver(color)} 個");
    }
}
