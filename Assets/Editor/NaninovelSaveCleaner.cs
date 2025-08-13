using UnityEditor;
using UnityEngine;
using System.IO;

public class NaninovelSaveCleaner
{
    [MenuItem("Tools/Naninovel/Delete All Save Data")]
    public static void DeleteSaves()
    {
        string path = Application.persistentDataPath;
        Debug.Log($"🧹 刪除 Naninovel Save 資料夾: {path}");
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log("✅ 已刪除 Naninovel 儲存資料！");
        }
        else
        {
            Debug.Log("⚠️ 沒有找到資料夾，可能已被刪除。");
        }
    }
}
