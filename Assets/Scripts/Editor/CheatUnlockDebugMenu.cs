// Assets/Scripts/Editor/CheatUnlockDebugMenu.cs
// Usage: Tools > Witch Club > 作弊名字 > ...
// 作弊模式平常是靠 chapter0 輸入名字觸發的，這裡讓你不用重跑一次序章也能開關。

using UnityEditor;
using UnityEngine;

public static class CheatUnlockDebugMenu
{
    const string MenuPath = "Tools/Witch Club/作弊名字/開啟全解鎖（CG／圖鑑／聖典／競技場）";

    [MenuItem(MenuPath)]
    static void Toggle ()
    {
        if (CheatUnlock.IsActive) CheatUnlock.Deactivate();
        else CheatUnlock.Activate();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate ()
    {
        Menu.SetChecked(MenuPath, CheatUnlock.IsActive);
        return true;
    }

    [MenuItem("Tools/Witch Club/作弊名字/印出目前狀態")]
    static void Dump ()
    {
        Debug.Log($"[CheatUnlock] 目前狀態：{(CheatUnlock.IsActive ? "開" : "關")}");
    }
}
