using Naninovel;
using Naninovel.UI;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class NaniScriptLoader_HEX : MonoBehaviour
{
    [Header("播畢是否清一次性入口參數")]
    public bool clearOneShotParameter = false;

    private const string TAG = "[NSL-KICK11]";

    private async void Start()
    {
        Debug.Log($"{TAG} Start");

        if (!Engine.Initialized)
        {
            try { await RuntimeInitializer.InitializeAsync(); }
            catch (Exception e) { Debug.LogWarning($"{TAG} Init error: {e.Message}"); }
        }

        await WaitServicesReadyAsync();
        HideLoadingIfAny();

        var ds = DataService.Instance;
        var sp = ds != null ? ds.scriptParameter : null;

        string scriptName = null;
        string label = null;

        if (sp != null && !string.IsNullOrEmpty(sp.scriptName))
        {
            scriptName = sp.scriptName;
            label      = string.IsNullOrEmpty(sp.scriptLabel) ? null : sp.scriptLabel;
            Debug.Log($"{TAG} got scriptParameter: name='{scriptName}', label='{(label ?? "<null>")}'");
        }
        else if (ds != null && !string.IsNullOrEmpty(ds.startScript))
        {
            scriptName = ds.startScript;
            Debug.Log($"{TAG} got startScript: '{scriptName}'");
        }
        else
        {
            Debug.Log($"{TAG} no entry param; do nothing.");
            return;
        }

        // 支援 "xxx#label"
        if (string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(scriptName))
        {
            var i = scriptName.IndexOf('#');
            if (i > 0 && i < scriptName.Length - 1)
            {
                label = scriptName.Substring(i + 1);
                scriptName = scriptName.Substring(0, i);
                Debug.Log($"{TAG} parsed inline label -> name='{scriptName}', label='{label}'");
            }
        }

        var scripts = Engine.GetService<IScriptManager>();
        var player  = Engine.GetService<IScriptPlayer>();
        if (scripts == null || player == null)
        {
            Debug.LogWarning($"{TAG} ScriptManager or ScriptPlayer missing. Abort.");
            return;
        }

        DumpPlayLikeMethods("IScriptPlayer",  player.GetType());
        DumpPlayLikeMethods("IScriptManager", scripts.GetType());

        // 取 Script 物件
        object scriptObj = null;
        try { scriptObj = await scripts.LoadScriptAsync(scriptName); }
        catch (Exception e) { Debug.LogWarning($"{TAG} LoadScriptAsync exception: {e.Message}"); }

        // 找 label 對應行號（可選）
        int? lineIndexFromLabel = null;
        if (!string.IsNullOrEmpty(label) && scriptObj != null)
            lineIndexFromLabel = TryGetLineIndexByLabel(scriptObj, label);

        // 若需要（某些舊版才用得到）
        var playlistCandidate = BuildPlaylistIfPossible(scriptName, scriptObj, label, lineIndexFromLabel);

        // 先試「含行號/標籤」→ 再退回 (0,0)
        bool played =
            await TryPlayOnTarget(player,  "IScriptPlayer",  scriptName, scriptObj, label, lineIndexFromLabel, playlistCandidate, preferZeroArg:false, preferJump:true) ||
            await TryPlayOnTarget(scripts, "IScriptManager", scriptName, scriptObj, label, lineIndexFromLabel, playlistCandidate, preferZeroArg:false, preferJump:true);

        if (!played)
            played = await SetScriptThenZeroArgPlay(player, scriptObj, scriptName);

        if (!played)
        {
            Debug.LogError($"{TAG} All play attempts failed.");
            return;
        }

        if (clearOneShotParameter && ds != null)
        {
            ds.startScript = null;
            ds.scriptParameter = null;
            Debug.Log($"{TAG} cleared one-shot params.");
        }

        Debug.Log($"{TAG} End");
    }

    private async Task WaitServicesReadyAsync()
    {
        int frames = 0;
        while ((Engine.GetService<IScriptPlayer>() == null ||
                Engine.GetService<IScriptManager>() == null) && frames < 300)
        {
            await Task.Yield();
            frames++;
        }
        Debug.Log($"{TAG} services ready (waited {frames} frames).");
        await Task.Yield(); await Task.Yield();
    }

    private void HideLoadingIfAny()
    {
        try { Engine.GetService<IUIManager>()?.GetUI<ILoadingUI>()?.Hide(); } catch {}
        var go = GameObject.Find("Canvas_LoadingPage");
        if (go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
            go.SetActive(false);
        }
    }

    private void DumpPlayLikeMethods(string who, Type t)
    {
        try
        {
            var ms = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m =>
                {
                    if (m.IsSpecialName) return false;
                    var n = m.Name.ToLowerInvariant();
                    if (!n.Contains("play")) return false;
                    if (n.Contains("hasplayed") || n.Contains("isplaying") || n.Contains("onplay")) return false;
                    return true;
                })
                .OrderBy(m => m.Name)
                .ToArray();

            Debug.Log($"{TAG} {who}={t.FullName} play-like methods ({ms.Length}):");
            foreach (var m in ms)
            {
                var pars = m.GetParameters();
                var sig = string.Join(", ", pars.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Debug.Log($"{TAG}   {who}.{m.Name}({sig})  [{(m.IsPublic ? "public" : "non-public")}]");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{TAG} Dump methods failed ({who}): {e.Message}");
        }
    }

    private static readonly string[] AllowedNames = new[]
    {
        "Play","PlayAsync",
        "PreloadAndPlay","PreloadAndPlayAsync",
        "LoadAndPlay","LoadAndPlayAsync",
        "ResetAndPlay","ResetAndPlayAsync",
        "PlayScript","PlayPlaylist","PlayList","StartPlay","StartPlaying","Start"
    };

    private bool IsAllowedPlayMethod(MethodInfo m, bool preferZeroArg)
    {
        if (m.IsSpecialName) return false;
        var name = m.Name;
        bool nameOk = AllowedNames.Any(allow =>
            name.Equals(allow, StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("." + allow, StringComparison.OrdinalIgnoreCase));
        if (!nameOk)
        {
            var lower = name.ToLowerInvariant();
            if (!lower.Contains("play")) return false;
            if (lower.Contains("hasplayed") || lower.Contains("isplaying") || lower.Contains("onplay")) return false;
        }
        if (preferZeroArg) return m.GetParameters().Length == 0;
        return m.GetParameters().Length >= 1 && m.GetParameters().Length <= 4;
    }

    private async Task<bool> TryPlayOnTarget(
        object target, string targetName,
        string scriptName, object scriptObj,
        string label, int? lineIndexFromLabel,
        object playlistCandidate,
        bool preferZeroArg,
        bool preferJump)
    {
        if (target == null) return false;

        var t = target.GetType();
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .Where(m => IsAllowedPlayMethod(m, preferZeroArg))
                       .ToArray();

        Debug.Log($"{TAG} {targetName} usable methods: {string.Join(", ", methods.Select(m => m.Name).Distinct())}");

        var cands = new List<object[]>();

        if (!preferZeroArg)
        {
            // ★ 先用「行號／標籤」的組合（讓它真的跳到 label）★
            if (lineIndexFromLabel.HasValue)
            {
                int li = lineIndexFromLabel.Value;
                cands.Add(new object[]{ scriptObj,   li, 0, null });
                cands.Add(new object[]{ scriptName,  li, 0, null });
                cands.Add(new object[]{ scriptObj,   li, 0 });
                cands.Add(new object[]{ scriptName,  li, 0 });
            }
            if (!string.IsNullOrEmpty(label))
            {
                cands.Add(new object[]{ scriptObj,   0, 0, label });
                cands.Add(new object[]{ scriptName,  0, 0, label });
                cands.Add(new object[]{ scriptObj,   0, label });
                cands.Add(new object[]{ scriptName,  0, label });
            }

            // 能帶 playlist 就帶（某些舊版會吃）
            if (playlistCandidate != null)
            {
                cands.Add(new object[]{ playlistCandidate, 0 });
                cands.Add(new object[]{ playlistCandidate });
            }

            // ★ 最後才退回「從 0 行開始」★
            cands.Add(new object[]{ scriptObj,   0, 0, null });
            cands.Add(new object[]{ scriptName,  0, 0, null });
            cands.Add(new object[]{ scriptObj,   0, 0 });
            cands.Add(new object[]{ scriptName,  0, 0 });
            cands.Add(new object[]{ scriptName });
            cands.Add(new object[]{ scriptObj   });
        }
        else
        {
            cands.Add(Array.Empty<object>()); // 零參數 Play/Start…
        }

        foreach (var args in cands)
        {
            if (args.Length >= 1 && args[0] == null) continue;
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length != args.Length) continue;
                if (!ArgsMatch(ps, args)) continue;
                try
                {
                    Debug.Log($"{TAG} invoke {targetName}.{m.Name}({string.Join(", ", args.Select(FormatArg))}) [{(m.IsPublic ? "public" : "non-public")}]");
                    var ret = m.Invoke(target, args);
                    if (ret is Task task) await task;
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{TAG} {targetName}.{m.Name} exception: {e.Message}");
                }
            }
        }

        return false;
    }

    private async Task<bool> SetScriptThenZeroArgPlay(object player, object scriptObj, string scriptName)
    {
        try
        {
            if (player == null) return false;
            var pt = player.GetType();

            // 設定 PlayedScript
            bool setOk = false;
            var prop = pt.GetProperty("PlayedScript", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite && scriptObj != null)
            {
                Debug.Log($"{TAG} set PlayedScript via property setter.");
                prop.SetValue(player, scriptObj, null);
                setOk = true;
            }
            else
            {
                var setter = pt.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .FirstOrDefault(m => !m.IsSpecialName &&
                                   m.Name.ToLowerInvariant().Contains("set") &&
                                   m.Name.ToLowerInvariant().Contains("played") &&
                                   m.GetParameters().Length == 1);
                if (setter != null && scriptObj != null)
                {
                    Debug.Log($"{TAG} invoke {setter.Name}(Script)");
                    setter.Invoke(player, new object[] { scriptObj });
                    setOk = true;
                }
            }

            // 記錄 / 還原 AutoPlay
            bool? autoBefore = null;
            var getAuto = pt.GetMethod("get_AutoPlayActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setAuto = pt.GetMethod("set_AutoPlayActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getAuto != null) autoBefore = (bool)getAuto.Invoke(player, null);

            bool played = await TryPlayOnTarget(player, "IScriptPlayer", scriptName, scriptObj, null, null, null, preferZeroArg:true, preferJump:false);

            if (!played)
            {
                var toggle = pt.GetMethod("ToggleAutoPlay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (toggle != null)
                {
                    Debug.Log($"{TAG} invoke ToggleAutoPlay()");
                    toggle.Invoke(player, null);
                    await Task.Yield();
                    played = true;
                }
            }

            if (autoBefore.HasValue && setAuto != null && getAuto != null)
            {
                bool now = (bool)getAuto.Invoke(player, null);
                if (now != autoBefore.Value)
                {
                    Debug.Log($"{TAG} restore AutoPlayActive -> {autoBefore.Value}");
                    setAuto.Invoke(player, new object[] { autoBefore.Value });
                }
            }

            return played;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{TAG} SetScriptThenZeroArgPlay exception: {e.Message}");
            return false;
        }
    }

    private bool ArgsMatch(ParameterInfo[] ps, object[] args)
    {
        for (int i = 0; i < ps.Length; i++)
        {
            var pT = ps[i].ParameterType;
            var a  = args[i];

            if (a == null)
            {
                if (pT.IsValueType && Nullable.GetUnderlyingType(pT) == null) return false;
                continue;
            }

            var aT = a.GetType();
            if (pT == typeof(int?) && aT == typeof(int)) continue;

            if (pT == typeof(int) || pT == typeof(int?))
            { if (aT != typeof(int)) return false; continue; }

            if (pT == typeof(string))
            { if (aT != typeof(string)) return false; continue; }

            if (!pT.IsAssignableFrom(aT)) return false;
        }
        return true;
    }

    private string FormatArg(object o) => o is string s ? $"\"{s}\"" : (o?.ToString() ?? "null");

    private int? TryGetLineIndexByLabel(object scriptObj, string label)
    {
        try
        {
            var st = scriptObj.GetType();

            var mi = st.GetMethod("GetLineIndexForLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
            if (mi != null)
            {
                var ret = mi.Invoke(scriptObj, new object[] { label });
                if (ret is int i && i >= 0) { Debug.Log($"{TAG} GetLineIndexForLabel('{label}') -> {i}"); return i; }
            }

            var prop = st.GetProperty("Lines", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                var linesObj = prop.GetValue(scriptObj, null) as IEnumerable;
                if (linesObj != null)
                {
                    int idx = 0;
                    foreach (var line in linesObj)
                    {
                        if (line == null) { idx++; continue; }
                        var lt = line.GetType();
                        if (lt.Name.ToLowerInvariant().Contains("label"))
                        {
                            var pLabel = lt.GetProperty("Label",     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                     ?? lt.GetProperty("LabelText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                     ?? lt.GetProperty("Text",      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (pLabel != null)
                            {
                                var val = pLabel.GetValue(line, null) as string;
                                if (!string.IsNullOrEmpty(val) && string.Equals(val, label, StringComparison.OrdinalIgnoreCase))
                                { Debug.Log($"{TAG} scan Lines found label '{label}' at index {idx}"); return idx; }
                            }
                        }
                        idx++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{TAG} TryGetLineIndexByLabel exception: {e.Message}");
        }
        return null;
    }

    private object BuildPlaylistIfPossible(string scriptName, object scriptObj, string label, int? lineIndex)
    {
        try
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                      .FirstOrDefault(a => a.GetName().Name.Contains("Naninovel"));
            if (asm == null) return null;

            var playlistType = asm.GetTypes()
                .FirstOrDefault(t => t.Name.IndexOf("Playlist", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                     (t.Name.IndexOf("Script", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      t.FullName.IndexOf("Script", StringComparison.OrdinalIgnoreCase) >= 0));
            if (playlistType == null) return null;

            object playlist = null;

            var ctor1 = (scriptObj != null) ? playlistType.GetConstructor(new[] { scriptObj.GetType() }) : null;
            if (ctor1 != null) playlist = ctor1.Invoke(new[] { scriptObj });

            if (playlist == null)
            {
                var ctorStr = playlistType.GetConstructor(new[] { typeof(string) });
                if (ctorStr != null) playlist = ctorStr.Invoke(new object[] { scriptName });
            }

            if (playlist == null && scriptObj != null)
            {
                var arr = Array.CreateInstance(scriptObj.GetType(), 1);
                arr.SetValue(scriptObj, 0);
                var ctorArr = playlistType.GetConstructor(new[] { arr.GetType() });
                if (ctorArr != null) playlist = ctorArr.Invoke(new object[] { arr });
            }

            if (playlist == null)
            {
                var strArr = new string[] { scriptName };
                var ctorStrArr = playlistType.GetConstructor(new[] { strArr.GetType() });
                if (ctorStrArr != null) playlist = ctorStrArr.Invoke(new object[] { strArr });
            }

            if (playlist == null) return null;

            if (!string.IsNullOrEmpty(label))
            {
                var p = playlistType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? playlistType.GetProperty("StartLabel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanWrite) p.SetValue(playlist, label, null);
            }
            if (lineIndex.HasValue)
            {
                var p = playlistType.GetProperty("StartLineIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? playlistType.GetProperty("LineIndex",       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanWrite) p.SetValue(playlist, lineIndex.Value, null);
            }

            Debug.Log($"{TAG} built playlist of type {playlistType.FullName}");
            return playlist;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{TAG} BuildPlaylistIfPossible exception: {e.Message}");
            return null;
        }
    }
}
