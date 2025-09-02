using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

/// =========================
///  ① 戰鬥指令本體 (@Battle)
///     - 預設用 @Battle
///     - 若某天 @Battle 被舊檔攔走，用 @BattleFix 測
/// =========================
[CommandAlias("Battle")]
public class GotoCombatScene : Command, Command.IForceWait
{
    // 由子類覆寫（用於精準紀錄當前觸發來源是 Battle 還是 BattleFix）
    protected virtual string AliasName => "Battle";

    [ParameterAlias("background")] public StringParameter Background;
    [ParameterAlias("target")]     public StringParameter Target;
    [ParameterAlias("script")]     public StringParameter ScriptName;
    [ParameterAlias("label")]      public StringParameter Label;

    public async override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        Debug.Log($"[{AliasName} vSTOP] 執行 GotoCombatScene");

        // 0) 服務與參數
        IScriptPlayer       scriptPlayer = null;
        IBackgroundManager  bgMgr        = null;
        ITextPrinterManager printerMgr   = null;

        try { scriptPlayer = Engine.GetService<IScriptPlayer>(); }
        catch (System.Exception ex) { Debug.LogError($"[{AliasName}] 取得 IScriptPlayer 失敗：{ex.Message}"); }

        try { bgMgr = Engine.GetService<IBackgroundManager>(); }
        catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 取得 IBackgroundManager 失敗（略過）：{ex.Message}"); }

        try { printerMgr = Engine.GetService<ITextPrinterManager>(); }
        catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 取得 ITextPrinterManager 失敗（略過）：{ex.Message}"); }

        var bgId       = Assigned(Background) ? Background.Value : null;
        var targetId   = Assigned(Target)     ? Target.Value     : null;
        var scriptName = (Assigned(ScriptName) && !string.IsNullOrEmpty(ScriptName.Value))
                         ? ScriptName.Value
                         : (scriptPlayer != null ? scriptPlayer.PlayedScript?.Name : null);
        var labelName  = Assigned(Label)      ? Label.Value      : null;

        Debug.Log($"[{AliasName}] Params => background:{bgId ?? "(null)"} | target:{targetId ?? "(null)"} | script:{scriptName ?? "(null)"} | label:{labelName ?? "(null)"}");

        // 1) 禁用跳略 + 停止劇本（避免 VN 往下改背景）
        try {
            if (scriptPlayer != null) {
                try { scriptPlayer.SetSkipEnabled(false); } catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] SetSkipEnabled 失敗：{ex.Message}"); }
                try { scriptPlayer.Stop(); Debug.Log($"[{AliasName}] 已停止 ScriptPlayer。"); } catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] Stop 失敗：{ex.Message}"); }
            } else Debug.LogWarning($"[{AliasName}] scriptPlayer 為空（略過停止）。");
        } catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 停止劇本區塊例外：{ex.Message}"); }

        // 2) 隱藏背景
        try {
            if (bgMgr != null) {
                IBackgroundActor bg = null;
                try { bg = bgMgr.GetActor(BackgroundsConfiguration.MainActorId); }
                catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 取 MainBackground 失敗：{ex.Message}"); }
                if (bg != null) { bg.Visible = false; Debug.Log($"[{AliasName}] 背景隱藏完成"); }
                else Debug.Log($"[{AliasName}] 當前沒有 MainBackground（略過）。");
            } else Debug.LogWarning($"[{AliasName}] 找不到 IBackgroundManager（略過）。");
        } catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 隱藏背景例外：{ex.Message}"); }

        // 3) 隱藏文字框
        try {
            if (printerMgr != null) {
                ITextPrinterActor printer = null;
                try { printer = printerMgr.GetActor(printerMgr.DefaultPrinterId); }
                catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 取 DefaultPrinter 失敗：{ex.Message}"); }
                if (printer != null) { await printer.ChangeVisibilityAsync(false, 0.1f); Debug.Log($"[{AliasName}] 文字框隱藏完成"); }
                else Debug.Log($"[{AliasName}] 當前沒有 DefaultPrinter（略過）。");
            } else Debug.LogWarning($"[{AliasName}] 找不到 ITextPrinterManager（略過）。");
        } catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 隱藏文字框例外：{ex.Message}"); }

        // 4) 傳遞資料
        try {
            if (DataService.Instance == null) Debug.LogError($"[{AliasName}] DataService.Instance 為空，無法傳遞參數。");
            else {
                DataService.Instance.scriptParameter = new ScriptParameter {
                    background   = bgId,
                    combatTarget = targetId,
                    scriptName   = scriptName,
                    scriptLabel  = labelName
                };
                Debug.Log($"[{AliasName}] 已設定 DataService.scriptParameter");
            }
        } catch (System.Exception ex) { Debug.LogError($"[{AliasName}] 設定 DataService 失敗：{ex.Message}"); }

        // 5) 教學旗標重置
        try { TutorialController.isTutorial = false; TutorialController.isTutorial2 = false; }
        catch (System.Exception ex) { Debug.LogWarning($"[{AliasName}] 重置教學旗標失敗（可忽略）：{ex.Message}"); }

        // 6) 切換場景（先檢查 Build Settings）
        Debug.Log($"[{AliasName}] 準備切換到 CombatScene ...");
        try {
            bool canLoad = false;
            try { canLoad = Application.CanStreamedLevelBeLoaded("CombatScene"); } catch { /* 舊版沒有此 API 亦可 */ }
            if (!canLoad) { Debug.LogError($"[{AliasName}] CombatScene 不在 Build Settings 或不可載入。"); return; }

            var op = SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Single);
            if (op == null) { Debug.LogError($"[{AliasName}] LoadSceneAsync 回傳 null。"); return; }

            try { await op; } catch { while (!op.isDone) await UniTask.Yield(); }

            Debug.Log($"[{AliasName}] 場景切換完成");
        }
        catch (System.Exception ex) { Debug.LogError($"[{AliasName}] 場景切換例外：{ex.Message}"); }
    }
}

/// =============================
///  ② 測試別名：@BattleFix（繞過舊檔衝突）
/// =============================
[CommandAlias("BattleFix")]
public class GotoCombatScene_BattleFix : GotoCombatScene
{
    protected override string AliasName => "BattleFix";
}

/// ===============================================
///  ③ 純探針：@BattleProbe（只印 log，零相依）
/// ===============================================
[CommandAlias("BattleProbe")]
public class BattleProbe : Command
{
    public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        Debug.Log("[BattleProbe] 指令已被觸發（這支不做任何事，只用來驗證掃描與別名）。");
        return UniTask.CompletedTask;
    }
}

/// ===============================================
///  ④ Boot 回報：一進 Play 就印，證明本檔載入
/// ===============================================
internal static class BattleBootReport
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void __EditorHook__()
    {
        Debug.Log("[BattleReport] (Editor) GotoCombatScene 指令檔已在編輯器載入。");
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void __BattleReport__()
    {
        string asmName = "(unknown)";
        try { asmName = typeof(GotoCombatScene).Assembly.GetName().Name; } catch { }
        Debug.Log("[BattleReport] GotoCombatScene 檔案已載入於組件：" + asmName);
        Debug.Log("[BattleReport] 可用別名：@Battle（本體） / @BattleFix（繞衝突） / @BattleProbe（探針）");
    }
}
