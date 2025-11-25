using System.IO;
using System.Text;
using UnityEngine;

public static class LabelResolver
{
    /// <summary>
    /// 從 .nani 腳本讀取文本，找 #labelName 所在行數
    /// </summary>
    public static int FindLabelLine(string scriptName, string label)
    {
        if (string.IsNullOrEmpty(scriptName) || string.IsNullOrEmpty(label))
            return -1;

        // Naninovel 1.x 通常放在 Resources/Naninovel/Scripts/
        string path = Path.Combine(
            Application.dataPath,
            "Resources",
            "Naninovel",
            "Scripts",
            scriptName + ".nani"
        );

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[LabelResolver] 找不到腳本檔案: {path}");
            return -1;
        }

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        string target = "#" + label; // ⭐ 正確搜尋 Nani 的章節 label

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith(target))
            {
                Debug.Log($"[LabelResolver] 找到 label '{label}' at line {i}");
                return i;
            }
        }

        Debug.LogWarning($"[LabelResolver] 找不到 label '{label}'");
        return -1;
    }
}
