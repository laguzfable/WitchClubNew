using Naninovel;
using UnityEngine;
using System.Reflection;
using System.Collections;

public class NaniScriptLoader : MonoBehaviour
{
    [Header("外部沒指定時的預設腳本")]
    public string defaultScriptName = "chapter0";

    private async void Start()
    {
        if (!Engine.Initialized)
            await RuntimeInitializer.InitializeAsync();

        var player  = Engine.GetService<IScriptPlayer>();
        var scripts = Engine.GetService<IScriptManager>();
        var ds      = DataService.Instance;
        if (player == null || scripts == null)
        {
            Debug.LogError("[NaniScriptLoader] 找不到 IScriptPlayer 或 IScriptManager。");
            return;
        }

        // 讀 SceneLoader 寫進來的目標
        string scriptName = null;
        string jumpMark   = null; // 我們把「label 參數」當成「標記註解字串」
        if (ds != null && ds.scriptParameter != null && !string.IsNullOrEmpty($"{ds.scriptParameter.scriptName}"))
        {
            scriptName = $"{ds.scriptParameter.scriptName}";
            var lbl = $"{ds.scriptParameter.scriptLabel}";
            jumpMark = string.IsNullOrEmpty(lbl) ? null : lbl;
        }
        else if (ds != null && !string.IsNullOrEmpty(ds.startScript))
        {
            scriptName = ds.startScript;
        }
        if (string.IsNullOrEmpty(scriptName))
            scriptName = string.IsNullOrEmpty(defaultScriptName) ? "chapter0" : defaultScriptName;

        // 載入腳本
        var script = await scripts.LoadScriptAsync(scriptName);
        if (script == null)
        {
            Debug.LogError($"[NaniScriptLoader] 找不到腳本：{scriptName}");
            return;
        }

        // 取 Lines 與行數
        var t = script.GetType();
        var pLines = t.GetProperty("Lines");
        var linesObj = pLines?.GetValue(script, null) as System.Collections.IEnumerable;
        int lineCount = (linesObj is System.Collections.ICollection coll) ? coll.Count : 0;

        // 尋找跳點行：優先真 label（若你將來改用 @label 也可），否則以「註解標記」搜尋
        int lineIndex = -1;

        // A) 嘗試舊版 label API（有就用）
        var m1 = t.GetMethod("GetLabelLineIndex");
        var m2 = t.GetMethod("GetLineIndexForLabel");
        if (!string.IsNullOrEmpty(jumpMark) && (m1 != null || m2 != null))
        {
            int tmp = -1;
            if (m1 != null) tmp = (int)m1.Invoke(script, new object[] { jumpMark });
            else            tmp = (int)m2.Invoke(script, new object[] { jumpMark });
            if (tmp >= 0) lineIndex = tmp;
        }

        // B) 若沒有 label 或找不到，就當成「註解跳點」搜尋：一行只有註解；形如 `;#AfterMap`
        if (lineIndex < 0 && !string.IsNullOrEmpty(jumpMark) && linesObj != null)
        {
            int idx = 0;
            foreach (var line in linesObj)
            {
                // 讀行文字（舊版 Line 有 Text 屬性）
                var textProp = line.GetType().GetProperty("Text");
                var text = textProp != null ? (textProp.GetValue(line, null) as string) : null;

                if (!string.IsNullOrEmpty(text))
                {
                    // 僅註解且包含 #標記 名稱，就視為跳點
                    var trimmed = text.Trim();
                    // 允許：";#AfterMap" 或 "; #AfterMap" 或 " ;#AfterMap"
                    if (trimmed.StartsWith(";") && trimmed.Contains("#" + jumpMark))
                    {
                        lineIndex = idx; // 跳點行本身
                        break;
                    }
                }
                idx++;
            }
        }

        // —— 關鍵：實際開始播放的「起始行」——
        // 1) 若有找到跳點：把起始行 = 跳點上一行（避免跳在註解/標籤本身），再夾在 [0, lineCount-1]
        // 2) 若沒指定跳點：從章頭（0）開始
        int startLine = 0;
        if (lineIndex >= 0)
        {
            startLine = lineIndex - 1;
            if (lineCount > 0)
            {
                if (startLine < 0) startLine = 0;
                if (startLine >= lineCount) startLine = lineCount - 1;
            }
        }

        // 停掉殘留播放
        try { if (player.Playing) player.Stop(); } catch {}

        // 為了避開你遇到的 GetCommandAfterLine NRE，從 startLine 起往下掃最多 10 行，直到抓到第一條可執行命令
        bool played = false;
        int tries = 0;
        int maxTries = Mathf.Clamp(lineCount, 1, 10);
        int probe = startLine;

        while (!played && tries < maxTries)
        {
            try
            {
                Debug.Log($"[NaniScriptLoader] ▶ 播放 {scriptName} @ line {probe}（跳點：{(jumpMark ?? "章頭")}）");
                await player.PreloadAndPlayAsync(script, probe); // 舊版簽名：(Script, int)
                played = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NaniScriptLoader] line {probe} 播放失敗，往下一行。原因：{ex.Message}");
                probe++;
                if (lineCount > 0 && probe >= lineCount) break;
            }
            tries++;
        }

        if (!played)
            Debug.LogError($"[NaniScriptLoader] 前 {tries} 行都無法開始播放。檢查檔案開頭是否有無效指令／自訂但未載入的指令。");

        // 清一次性參數
        if (ds != null) { ds.startScript = null; ds.scriptParameter = null; }
    }
}
