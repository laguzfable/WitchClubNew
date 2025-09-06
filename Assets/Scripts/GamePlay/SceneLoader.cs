using Naninovel;
using UnityEngine;
using System.Reflection;

public class NaniScriptLoader : MonoBehaviour
{
    [Header("外部沒指定時的預設腳本")]
    public string defaultScriptName = "chapter0";

    private async void Start()
    {
        Debug.Log("[NSL] Start()");

        if (!Engine.Initialized)
        {
            Debug.Log("[NSL] Engine not initialized. Initialize …");
            await RuntimeInitializer.InitializeAsync();
        }

        // ---- 保險還原（避免前一場景殘留造成黑畫面/看不到 show）----
        var naniCam = Engine.GetService<ICameraManager>().Camera;
        if (naniCam && !naniCam.enabled) naniCam.enabled = true;

        var pm = Engine.GetService<ITextPrinterManager>();
        var printer = pm != null ? pm.GetActor(pm.DefaultPrinterId) : null;
        if (printer != null && !printer.Visible) await printer.ChangeVisibilityAsync(true, 0f);

        var bgm = Engine.GetService<IBackgroundManager>();
        var mainBg = bgm != null ? bgm.GetActor(BackgroundsConfiguration.MainActorId) : null;
        if (mainBg != null && !mainBg.Visible) mainBg.Visible = true;

        if (Time.timeScale != 1f) Time.timeScale = 1f;
        // -------------------------------------------------------------------

        var player  = Engine.GetService<IScriptPlayer>();
        var scripts = Engine.GetService<IScriptManager>();
        var ds      = DataService.Instance;
        if (player == null || scripts == null)
        {
            Debug.LogError("[NSL] 找不到 IScriptPlayer 或 IScriptManager。");
            return;
        }

        string scriptName = null;
        string jumpMark   = null;

        if (ds != null && ds.scriptParameter != null && !string.IsNullOrEmpty($"{ds.scriptParameter.scriptName}"))
        {
            scriptName = $"{ds.scriptParameter.scriptName}";
            var lbl = $"{ds.scriptParameter.scriptLabel}";
            jumpMark = string.IsNullOrEmpty(lbl) ? null : lbl;
            Debug.Log($"[NSL] from ds.scriptParameter -> script='{scriptName}', label='{jumpMark}'");
        }
        else if (ds != null && !string.IsNullOrEmpty(ds.startScript))
        {
            scriptName = ds.startScript;
            Debug.Log($"[NSL] from ds.startScript -> script='{scriptName}'");
        }
        else
        {
            Debug.Log("[NSL] use default");
        }

        if (string.IsNullOrEmpty(scriptName))
            scriptName = string.IsNullOrEmpty(defaultScriptName) ? "chapter0" : defaultScriptName;

        Debug.Log($"[NSL] LoadScriptAsync('{scriptName}')");
        var script = await scripts.LoadScriptAsync(scriptName);
        if (script == null)
        {
            Debug.LogError($"[NSL] 找不到腳本：{scriptName}");
            return;
        }

        // 以下：尋標籤 & 容錯播放（保持你先前的強化版行為）
        var t = script.GetType();
        var pLines = t.GetProperty("Lines");
        var linesObj = pLines?.GetValue(script, null) as System.Collections.IEnumerable;
        int lineCount = (linesObj is System.Collections.ICollection coll) ? coll.Count : 0;
        Debug.Log($"[NSL] Script '{scriptName}' lineCount={lineCount}, label='{jumpMark ?? "(none)"}'");

        int lineIndex = -1;
        var m1 = t.GetMethod("GetLabelLineIndex");
        var m2 = t.GetMethod("GetLineIndexForLabel");
        if (!string.IsNullOrEmpty(jumpMark) && (m1 != null || m2 != null))
        {
            int tmp = -1;
            if (m1 != null) tmp = (int)m1.Invoke(script, new object[] { jumpMark });
            else            tmp = (int)m2.Invoke(script, new object[] { jumpMark });
            Debug.Log($"[NSL] label search via {(m1!=null?"GetLabelLineIndex":"GetLineIndexForLabel")}('{jumpMark}') -> {tmp}");
            if (tmp >= 0) lineIndex = tmp;
        }

        if (lineIndex < 0 && !string.IsNullOrEmpty(jumpMark) && linesObj != null)
        {
            int idx = 0;
            foreach (var line in linesObj)
            {
                var textProp = line.GetType().GetProperty("Text");
                var text = textProp != null ? (textProp.GetValue(line, null) as string) : null;
                if (!string.IsNullOrEmpty(text))
                {
                    var trimmed = text.Trim();
                    if (trimmed.StartsWith(";") && trimmed.Contains("#" + jumpMark))
                    {
                        lineIndex = idx;
                        Debug.Log($"[NSL] found comment mark at line={lineIndex} for '#{jumpMark}'");
                        break;
                    }
                }
                idx++;
            }
        }

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
        Debug.Log($"[NSL] startLine={startLine} (from lineIndex={lineIndex})");

        try { if (player.Playing) { Debug.Log("[NSL] player.Stop()"); player.Stop(); } } catch {}

        bool played = false;
        int tries = 0;
        int maxTries = Mathf.Clamp(lineCount, 1, 10);
        int probe = startLine;

        while (!played && tries < maxTries)
        {
            try
            {
                Debug.Log($"[NSL] ▶ 播放 {scriptName} @ line {probe}（跳點：{(jumpMark ?? "章頭")}）");
                await player.PreloadAndPlayAsync(script, probe);
                played = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NSL] line {probe} 播放失敗，往下一行。原因：{ex.Message}");
                probe++;
                if (lineCount > 0 && probe >= lineCount) break;
            }
            tries++;
        }

        if (!played)
            Debug.LogError($"[NSL] 無法開始播放（嘗試 {tries} 次）。檢查標籤附近是否有可執行命令。");

        if (ds != null) { ds.startScript = null; ds.scriptParameter = null; Debug.Log("[NSL] ds.startScript/scriptParameter cleared."); }
    }
}
