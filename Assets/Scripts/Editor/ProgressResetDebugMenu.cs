using UnityEditor;
using UnityEngine;

/// <summary>
/// 清本地進度的兩顆按鈕。跟遊戲中的 F10／F9 走同一份步驟（見 ProgressResetter），
/// 兩邊共用才不會其中一邊漏清東西。
///
/// Steam 成就不在這裡面，那是雲端的，要清請用 Tools/Witch Club/Steam 成就 底下那幾顆。
/// </summary>
public static class ProgressResetDebugMenu
{
    [MenuItem("Tools/Witch Club/進度/全部清掉（本地）")]
    static void ResetAll ()
    {
        if (!EditorUtility.DisplayDialog(
                "全部清掉",
                "會清掉全部 PlayerPrefs（事件進度、符文／卡片、競技場樓層、結局紀錄）"
                + "與回憶模式 CG。無法復原，要繼續嗎？",
                "清掉", "取消"))
            return;

        ProgressResetter.ResetLocalProgress();
    }

    [MenuItem("Tools/Witch Club/進度/除了女巫競技場以外全部清掉")]
    static void ResetKeepArena ()
    {
        var floor = PlayerPrefs.GetInt("TowerMode.BestFloor", 0);

        if (!EditorUtility.DisplayDialog(
                "除了女巫競技場以外全部清掉",
                $"保留：競技場最高樓層（目前 {floor}）與結局紀錄。\n"
                + "結局紀錄是競技場的進場資格，清掉的話標題上那顆按鈕會縮回去。\n"
                + "副作用：蝕之聖典的入口也看結局紀錄，所以那顆按鈕也會留著（節點解鎖資料還是會清）。\n\n"
                + "清掉：劇情事件進度、符文、卡片型態、怪物圖鑑、回憶模式 CG、"
                + "蝕之聖典節點、教學紀錄、玩家名字、競技場當局狀態。\n\n"
                + "無法復原，要繼續嗎？",
                "清掉", "取消"))
            return;

        ProgressResetter.ResetLocalProgressKeepArena();
    }
}
