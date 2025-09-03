using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Battle")]
public class GotoCombatScene : Command, Command.IForceWait
{
    public StringParameter Background;
    public StringParameter Target;
    public StringParameter ScriptName;
    public StringParameter Label;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log("[Battle] 執行自訂指令 GotoCombatScene！");

        // 檢查參數
        Debug.Log($"[Battle] Background: {Background}, Target: {Target}, ScriptName: {ScriptName}, Label: {Label}");

        // 1. 禁用跳略
        Engine.GetService<IScriptPlayer>().SetSkipEnabled(false);

        // 2. 隱藏背景
        var bgMgr = Engine.GetService<IBackgroundManager>();
        if (bgMgr != null)
        {
            var bgActor = bgMgr.GetActor(BackgroundsConfiguration.MainActorId);
            if (bgActor != null)
            {
                bgActor.Visible = false;
                Debug.Log("[Battle] 背景隱藏完成");
            }
            else
            {
                Debug.LogWarning("[Battle] 找不到背景 Actor");
            }
        }
        else
        {
            Debug.LogWarning("[Battle] 找不到 BackgroundManager");
        }

        // 3. 隱藏文字框
        var printerMgr = Engine.GetService<ITextPrinterManager>();
        if (printerMgr != null)
        {
            await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f);
            Debug.Log("[Battle] 文字框隱藏完成");
        }
        else
        {
            Debug.LogWarning("[Battle] 找不到 TextPrinterManager");
        }

        // 4. 補齊劇本名稱
        if (!Assigned(ScriptName))
        {
            ScriptName = Engine.GetService<IScriptPlayer>().PlayedScript.Name;
            Debug.Log($"[Battle] 未指定 ScriptName，自動取得：{ScriptName}");
        }

        // 5. 資料傳遞
        DataService.Instance.scriptParameter = new ScriptParameter()
        {
            background = Background,
            combatTarget = Target,
            scriptName = ScriptName,
            scriptLabel = Label
        };
        Debug.Log("[Battle] 已設定 DataService.Instance.scriptParameter");

        TutorialController.isTutorial = false;
        TutorialController.isTutorial2 = false;

        // 6. 檢查場景是否存在於 Build Settings
        Debug.Log("[Battle] 準備切換到 CombatScene ...");
        if (!Application.CanStreamedLevelBeLoaded("CombatScene"))
        {
            Debug.LogError("[Battle] CombatScene 不在 Build Settings，切換會失敗！");
            return;
        }

        // 7. 切換場景
        await SceneManager.LoadSceneAsync("CombatScene");
        Debug.Log("[Battle] 場景切換完成");
    }
}
