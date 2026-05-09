using Naninovel;
using UnityEngine;

public class NaniScriptLoader : MonoBehaviour
{
    [Header("外部沒指定時的預設腳本")]
    public string defaultScriptName = "chapter0";

    private async void Start()
    {
        Debug.Log("[NSL] Start()");


        // 清掉 Title 場景的舊 UI（除了自己）
foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
{
    if (obj != this.gameObject) // 不清掉自己
    {
        if (obj.name.Contains("Title") || obj.name.Contains("Canvas") || obj.name.Contains("BackgroundLayer") || obj.name.Contains("Blurred"))
        {
            Debug.Log($"[NSL] Destroy leftover: {obj.name}");
            GameObject.Destroy(obj);
        }
    }
}




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

        var player = Engine.GetService<IScriptPlayer>();
        var ds     = DataService.Instance;
        if (player == null)
        {
            Debug.LogError("[NSL] 找不到 IScriptPlayer。");
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

        // 清除，避免重複使用
        if (ds != null) { ds.startScript = null; ds.scriptParameter = null; Debug.Log("[NSL] ds cleared."); }

        try { if (player.Playing) { Debug.Log("[NSL] player.Stop()"); player.Stop(); } } catch { }

        // ── 直接使用 Naninovel 原生 label 參數，不再用反射找行號 ──
        Debug.Log($"[NSL] ▶ PreloadAndPlayAsync('{scriptName}', label='{jumpMark ?? "(none)"}')");
        try
        {
            await player.PreloadAndPlayAsync(scriptName, label: jumpMark);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NSL] label='{jumpMark}' 播放失敗，嘗試從頭播放。原因：{ex.Message}");
            try
            {
                await player.PreloadAndPlayAsync(scriptName);
            }
            catch (System.Exception ex2)
            {
                Debug.LogError($"[NSL] 從頭播放也失敗：{ex2.Message}");
            }
        }
    }
}
