// Assets/Scripts/Editor/NaniScriptTool.cs
// Usage: Tools > Witch Club > 劇本工具
//
// 兩件事：
//   ‧ 檢查  ── 把整包 .nani 掃一遍，列出跳轉、選項、註冊的問題，點一下跳到那一行
//   ‧ 產生器 ── 填表格產生選項的語法，不用自己記 set: / goto: 怎麼寫
//
// 這裡只讀寫 Assets/NaniScripts 底下的 .nani 和 EditorResources.asset，不碰執行時的東西。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class NaniScriptTool : EditorWindow
{
    const string ScriptFolder = "Assets/NaniScripts";
    const string ResourcesAsset = "Assets/NaninovelData/EditorResources.asset";

    [MenuItem("Tools/Witch Club/劇本工具")]
    static void Open ()
    {
        GetWindow<NaniScriptTool>("劇本工具").minSize = new Vector2(560, 400);
    }

    enum Tab { Check, Builder }
    Tab tab = Tab.Check;

    // ── 檢查 ──
    class Issue
    {
        public string File;
        public int Line;
        public string Message;
        public bool IsError;   // false = 只是提醒
    }

    List<Issue> issues = new List<Issue>();
    Vector2 issueScroll;
    bool skipBackups = true;
    bool showWarnings = true;

    // ── 產生器 ──
    class OptionRow
    {
        public string Text = "";
        public int CharacterIndex;   // 0 = 不加好感
        public int Amount = 10;
        public string GotoLabel = "";
    }

    static readonly string[] characterNames = { "（不加好感）", "優菲", "梅爾", "薇狄亞", "涅莉", "西碧兒" };
    static readonly string[] characterVars = { "", "affinity_Eup", "affinity_Mel", "affinity_Ved", "affinity_Nel", "affinity_Syb" };

    List<OptionRow> rows = new List<OptionRow> { new OptionRow(), new OptionRow() };
    string builderPreview = "";

    void OnGUI ()
    {
        tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "檢查", "選項產生器" });
        EditorGUILayout.Space();

        if (tab == Tab.Check) DrawCheck();
        else DrawBuilder();
    }

    // ============================================================
    //  檢查
    // ============================================================

    void DrawCheck ()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("掃描全部劇本", GUILayout.Height(28))) Scan();
            skipBackups = GUILayout.Toggle(skipBackups, "略過備份／測試", GUILayout.Width(120));
            showWarnings = GUILayout.Toggle(showWarnings, "顯示提醒", GUILayout.Width(80));
        }

        var errors = issues.Count(i => i.IsError);
        var warns = issues.Count - errors;
        EditorGUILayout.HelpBox(
            issues.Count == 0
                ? "還沒掃描，或是沒有問題。"
                : $"錯誤 {errors} 個，提醒 {warns} 個。錯誤會讓劇本跑不動，提醒不會。",
            errors > 0 ? MessageType.Error : (warns > 0 ? MessageType.Warning : MessageType.Info));

        issueScroll = EditorGUILayout.BeginScrollView(issueScroll);
        foreach (var issue in issues)
        {
            if (!issue.IsError && !showWarnings) continue;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var icon = issue.IsError ? "❌" : "⚠";
                EditorGUILayout.LabelField($"{icon} {Path.GetFileName(issue.File)}:{issue.Line}",
                                           GUILayout.Width(200));
                EditorGUILayout.LabelField(issue.Message);
                if (GUILayout.Button("開啟", GUILayout.Width(50)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.File);
                    if (asset != null) AssetDatabase.OpenAsset(asset, issue.Line);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void Scan ()
    {
        issues.Clear();

        var files = Directory.GetFiles(ScriptFolder, "*.nani", SearchOption.AllDirectories)
                             .Select(f => f.Replace('\\', '/'))
                             .Where(f => !skipBackups || !IsBackup(f))
                             .ToList();

        // 檔名 → 該檔的 label 清單
        var labelsByFile = new Dictionary<string, HashSet<string>>();
        var textByFile = new Dictionary<string, string[]>();
        foreach (var f in files)
        {
            var lines = File.ReadAllLines(f);
            textByFile[f] = lines;
            labelsByFile[Path.GetFileNameWithoutExtension(f)] = new HashSet<string>(
                lines.Where(l => l.TrimStart().StartsWith("#"))
                     .Select(l => l.TrimStart().Substring(1).Trim())
                     .Where(l => l.Length > 0));
        }

        var registered = ReadRegisteredScripts();

        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f);
            var lines = textByFile[f];
            var labels = labelsByFile[name];

            if (registered != null && !registered.Contains(name))
                Add(f, 1, "這支腳本沒有註冊在 EditorResources，Naninovel 找不到它", true);

            var seen = new HashSet<string>();
            foreach (var l in lines.Where(x => x.TrimStart().StartsWith("#")))
            {
                var lab = l.TrimStart().Substring(1).Trim();
                if (lab.Length > 0 && !seen.Add(lab))
                    Add(f, IndexOf(lines, l) + 1, $"label「{lab}」重複了，@goto 只會跳到第一個", true);
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                var no = i + 1;

                if (line.StartsWith(";") || line.Length == 0) continue;

                // 1. goto: 少了 @
                if (line.StartsWith("goto:"))
                    Add(f, no, "少了 @：這行會被當成旁白印出來，不是跳轉", true);

                // 2. 同檔跳轉的目標存不存在
                foreach (Match m in Regex.Matches(line, @"@goto \.(\S+)"))
                    if (!labels.Contains(m.Groups[1].Value))
                        Add(f, no, $"跳不到：這個檔案裡沒有 label「{m.Groups[1].Value}」", true);

                foreach (Match m in Regex.Matches(line, @"goto:\.(\S+)"))
                    if (!labels.Contains(m.Groups[1].Value))
                        Add(f, no, $"跳不到：這個檔案裡沒有 label「{m.Groups[1].Value}」", true);

                // 3. 跨檔跳轉
                foreach (Match m in Regex.Matches(line, @"@goto (\w+)\.(\w+)"))
                {
                    var target = m.Groups[1].Value;
                    var lab = m.Groups[2].Value;
                    if (!labelsByFile.ContainsKey(target))
                        Add(f, no, $"跳不到：找不到腳本「{target}」", true);
                    else if (!labelsByFile[target].Contains(lab))
                        Add(f, no, $"跳不到：「{target}」裡沒有 label「{lab}」", true);
                }

                // 4. 旁白行掛了 if:／set:（那是指令才有的參數）
                if (!line.StartsWith("@") && !line.StartsWith("#") &&
                    Regex.IsMatch(line, @"\s(if|set):\S"))
                    Add(f, no, "旁白行不吃 if:／set:，會整串印在畫面上", true);

                // 5. 選項文字被半形空格截斷
                if (line.StartsWith("@choice "))
                {
                    var body = line.Substring(8);
                    var textPart = Regex.Split(body, @"\s+(set:|goto:|if:)")[0];
                    if (!textPart.StartsWith("\"") && textPart.Contains(" "))
                        Add(f, no, "選項文字有半形空格，畫面上會被截斷（用半形引號包起來）", false);
                }

                // 6. @set 加值時變數名打錯（affinity_Xxx=affinity_Yyy+N）
                var setM = Regex.Match(line, @"affinity_(\w+)=affinity_(\w+)\+");
                if (setM.Success && setM.Groups[1].Value != setM.Groups[2].Value)
                    Add(f, no, $"加錯人了：把 affinity_{setM.Groups[1].Value} 設成 affinity_{setM.Groups[2].Value} 的值", true);
            }

            // 7. 選項群組後面要有 @stop
            //    中間可以夾別的指令（例如 @hide 把說話的人收掉），那些照樣會執行，
            //    所以只要在碰到台詞或 label 之前找得到 @stop 就算過。
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].TrimStart().StartsWith("@choice")) continue;

                var j = i;
                var found = false;
                while (j < lines.Length)
                {
                    var t = lines[j].TrimStart();
                    if (t.StartsWith("@stop")) { found = true; break; }

                    // 台詞或 label 出現＝這組選項已經結束了，還沒看到 @stop
                    if (t.Length > 0 && !t.StartsWith("@") && !t.StartsWith(";") && !t.StartsWith("#")) break;
                    if (t.StartsWith("#")) break;

                    j++;
                }

                if (!found)
                    Add(f, i + 1, "這組選項後面沒有 @stop，劇本會直接往下衝過去", true);

                i = j;
            }
        }

        issues = issues.OrderByDescending(x => x.IsError).ThenBy(x => x.File).ThenBy(x => x.Line).ToList();
        Debug.Log($"[劇本工具] 掃了 {files.Count} 支，錯誤 {issues.Count(x => x.IsError)}、提醒 {issues.Count(x => !x.IsError)}");
    }

    static bool IsBackup (string path)
    {
        var n = Path.GetFileNameWithoutExtension(path);
        return n.Contains("備份") || n.StartsWith("Test") || n.StartsWith("test") ||
               n.StartsWith("111") || n.StartsWith("222") || n.Contains("Test");
    }

    static int IndexOf (string[] lines, string line)
    {
        for (var i = 0; i < lines.Length; i++) if (lines[i] == line) return i;
        return 0;
    }

    void Add (string file, int line, string message, bool isError)
    {
        issues.Add(new Issue { File = file, Line = line, Message = message, IsError = isError });
    }

    /// <summary>EditorResources 裡註冊過的腳本名。讀不到就回 null（那就不檢查這一項）。</summary>
    static HashSet<string> ReadRegisteredScripts ()
    {
        if (!File.Exists(ResourcesAsset)) return null;

        var text = File.ReadAllText(ResourcesAsset);
        var names = new HashSet<string>();
        foreach (Match m in Regex.Matches(text, @"- name: (\S+)\s*\n\s*pathPrefix: Scripts"))
            names.Add(m.Groups[1].Value);

        return names.Count > 0 ? names : null;
    }

    // ============================================================
    //  選項產生器
    // ============================================================

    void DrawBuilder ()
    {
        EditorGUILayout.HelpBox(
            "填好之後按「產生」，語法會複製到剪貼簿，貼進劇本就行。\n" +
            "好感的數字慣例：閒聊 +5、日常事件 +10～25、序章到第二章的選項 +10（保底那幾顆 +20）。",
            MessageType.Info);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"選項 {i + 1}", GUILayout.Width(50));
                    row.Text = EditorGUILayout.TextField(row.Text);
                    if (rows.Count > 1 && GUILayout.Button("－", GUILayout.Width(24)))
                    {
                        rows.RemoveAt(i);
                        return;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("加好感給", GUILayout.Width(60));
                    row.CharacterIndex = EditorGUILayout.Popup(row.CharacterIndex, characterNames, GUILayout.Width(110));

                    using (new EditorGUI.DisabledScope(row.CharacterIndex == 0))
                    {
                        EditorGUILayout.LabelField("＋", GUILayout.Width(20));
                        row.Amount = EditorGUILayout.IntField(row.Amount, GUILayout.Width(45));
                    }

                    EditorGUILayout.LabelField("跳到 label", GUILayout.Width(65));
                    row.GotoLabel = EditorGUILayout.TextField(row.GotoLabel);
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (rows.Count < 4 && GUILayout.Button("＋ 加一個選項")) rows.Add(new OptionRow());
            if (GUILayout.Button("產生", GUILayout.Width(80))) Build();
        }

        if (string.IsNullOrEmpty(builderPreview)) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("產生的語法（已複製到剪貼簿）", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(builderPreview, GUILayout.MinHeight(90));
    }

    void Build ()
    {
        var sb = new StringBuilder();
        var warned = false;

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.Text)) continue;

            var text = row.Text.Trim();
            // 選項文字碰到半形空格會被截斷，自動包引號
            if (text.Contains(" ")) { text = "\"" + text + "\""; warned = true; }

            sb.Append("@choice ").Append(text);

            if (row.CharacterIndex > 0)
            {
                var v = characterVars[row.CharacterIndex];
                sb.Append($" set:{v}={v}+{row.Amount}");
            }

            if (!string.IsNullOrEmpty(row.GotoLabel))
                sb.Append(" goto:.").Append(row.GotoLabel.TrimStart('.'));

            sb.AppendLine();
        }

        sb.AppendLine("@stop");

        // 順手把每個分支的 label 也生出來，省得自己補
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.GotoLabel)) continue;
            sb.AppendLine();
            sb.AppendLine("# " + row.GotoLabel.TrimStart('.'));
        }

        builderPreview = sb.ToString();
        EditorGUIUtility.systemCopyBuffer = builderPreview;

        if (warned)
            Debug.Log("[劇本工具] 有選項文字含半形空格，已自動用引號包起來（不包的話畫面上會被截斷）");
    }
}
