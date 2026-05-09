using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class InfoScreenSortingFixer
{
    [MenuItem("Tools/Witch Club/Fix InfoScreen Sorting")]
    static void FixInfoScreenSorting()
    {
        // Find ALL objects named InfoScreen
        System.Collections.Generic.List<GameObject> found = new System.Collections.Generic.List<GameObject>();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "InfoScreen" && go.scene.isLoaded)
                found.Add(go);
        }

        if (found.Count == 0)
        {
            EditorUtility.DisplayDialog("Fix InfoScreen Sorting", "找不到 InfoScreen！", "OK");
            return;
        }

        // Show all found objects and their child counts
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"找到 {found.Count} 個 InfoScreen：\n");
        for (int i = 0; i < found.Count; i++)
        {
            var go = found[i];
            bool hasImage = go.GetComponent<Image>() != null;
            bool hasText = go.GetComponentInChildren<Text>() != null;
            sb.AppendLine($"[{i}] {GetPath(go)}");
            sb.AppendLine($"     子物件數：{go.transform.childCount}　Image:{hasImage}　Text:{hasText}\n");
        }
        sb.AppendLine("即將修改第 0 個（請確認路徑是否正確）");

        bool ok = EditorUtility.DisplayDialog("Fix InfoScreen Sorting", sb.ToString(), "確認修改", "取消");
        if (!ok) return;

        GameObject infoObj = found[0];
        Undo.RecordObject(infoObj, "Fix InfoScreen Sorting");

        Canvas canvas = infoObj.GetComponent<Canvas>();
        if (canvas == null)
            canvas = Undo.AddComponent<Canvas>(infoObj);

        canvas.overrideSorting = true;
        canvas.sortingOrder = 999;

        if (infoObj.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(infoObj);

        EditorUtility.SetDirty(infoObj);
        EditorUtility.DisplayDialog("Fix InfoScreen Sorting", $"完成！\n{GetPath(infoObj)}\nSort Order = 100", "OK");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
