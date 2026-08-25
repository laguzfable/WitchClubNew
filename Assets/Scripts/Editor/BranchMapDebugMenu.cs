// Assets/Scripts/Editor/BranchMapDebugMenu.cs
// Usage: Tools > Witch Club > 蝕之聖典 > 全解鎖所有節點（除錯）
// debugUnlockAll 是排版用的旁路：勾起來時所有節點都當成走過，方便看版面。
// 它存在 prefab 上，忘記關掉的話玩家會直接看到全部路線，所以拉成有勾選狀態的選單。

using UnityEditor;
using UnityEngine;

public static class BranchMapDebugMenu
{
    const string MenuPath = "Tools/Witch Club/蝕之聖典/全解鎖所有節點（除錯）";
    const string PrefabPath = "Assets/newsystems/BranchMapUI.prefab";

    [MenuItem(MenuPath)]
    static void Toggle ()
    {
        var ui = LoadUI();
        if (ui == null) return;

        ui.debugUnlockAll = !ui.debugUnlockAll;
        EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();

        Debug.Log($"[BranchMap] 全解鎖節點 → {(ui.debugUnlockAll ? "開（除錯中，發布前記得關）" : "關")}");
    }

    // 順便當勾選狀態的來源：Unity 每次要畫這個選單項時都會呼叫一次。
    [MenuItem(MenuPath, true)]
    static bool ToggleValidate ()
    {
        var ui = LoadUI();
        Menu.SetChecked(MenuPath, ui != null && ui.debugUnlockAll);
        return ui != null;
    }

    static BranchMapUI LoadUI ()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[BranchMap] 找不到 {PrefabPath}");
            return null;
        }

        var ui = prefab.GetComponent<BranchMapUI>();
        if (ui == null)
            Debug.LogWarning("[BranchMap] prefab 上沒有 BranchMapUI 元件。");

        return ui;
    }
}
