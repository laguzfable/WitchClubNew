#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DisableLegacyNaniScriptLoader
{
    [MenuItem("Tools/Debug/Disable Legacy NaniScriptLoader (wrap with #if false)")]
    public static void Disable()
    {
        var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        var reClass = new Regex(@"\bclass\s+NaniScriptLoader\b");

        int hit = 0;
        foreach (var abs in files)
        {
            var rel = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
            if (rel.EndsWith("/DisableLegacyNaniScriptLoader.cs")) continue; // 自己

            string text;
            try { text = File.ReadAllText(abs); } catch { continue; }

            if (!reClass.IsMatch(text)) continue;

            // 已經包過就跳過
            if (text.Contains("#if false") && text.Contains("#endif"))
            {
                Debug.Log($"[NFix] Already disabled: {rel}");
                continue;
            }

            // 備份
            File.WriteAllText(abs + ".bak", text);

            // 包起來
            var wrapped = "// auto-disabled by DisableLegacyNaniScriptLoader\n#if false\n" + text + "\n#endif\n";
            File.WriteAllText(abs, wrapped);
            Debug.Log($"[NFix] Disabled legacy NaniScriptLoader in: {rel}");
            hit++;
        }

        if (hit == 0) Debug.Log("[NFix] No legacy NaniScriptLoader found.");
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Debug/Find NaniScriptLoader definitions")]
    public static void FindDefs()
    {
        var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        var reClass = new Regex(@"\bclass\s+NaniScriptLoader\b");
        var reStart = new Regex(@"\bStart\s*\(\s*\)");

        int total = 0;
        foreach (var abs in files)
        {
            var rel = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
            if (rel.EndsWith("/DisableLegacyNaniScriptLoader.cs")) continue;

            string text;
            try { text = File.ReadAllText(abs); } catch { continue; }

            if (reClass.IsMatch(text))
            {
                var startCount = reStart.Matches(text).Count;
                Debug.Log($"[NFix] {rel}  (Start() count: {startCount})");
                total++;
            }
        }

        Debug.Log($"[NFix] Done. files with class NaniScriptLoader: {total}");
    }
}
#endif
