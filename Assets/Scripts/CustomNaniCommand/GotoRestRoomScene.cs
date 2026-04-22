using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("Restroom")]
public class GotoRestRoomScene : Command, Command.IForceWait
{
    [ParameterAlias("ScriptName")] public StringParameter ScriptName;
    [ParameterAlias("Label")]      public StringParameter Label;

    public async override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        Debug.Log($"[GRS] ===== @Restroom START =====  ScriptName={Assigned(ScriptName)} Label={Assigned(Label)}");

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        Debug.Log($"[GRS] scriptPlayer={(scriptPlayer == null ? "NULL" : "OK")}");
        scriptPlayer?.SetSkipEnabled(false);

        var bgm = Engine.GetService<IBackgroundManager>();
        Debug.Log($"[GRS] bgm={(bgm == null ? "NULL" : "OK")}");
        if (bgm != null)
        {
            try
            {
                var bgActor = bgm.GetActor(BackgroundsConfiguration.MainActorId);
                if (bgActor != null) bgActor.Visible = false;
                Debug.Log("[GRS] bg hidden OK");
            }
            catch (System.Exception e) { Debug.LogWarning($"[GRS] bg hide failed: {e.Message}"); }
        }

        var printerMgr = Engine.GetService<ITextPrinterManager>();
        Debug.Log($"[GRS] printerMgr={(printerMgr == null ? "NULL" : "OK")}, DefaultPrinterId='{printerMgr?.DefaultPrinterId}'");
        if (printerMgr != null)
        {
            try
            {
                var printer = printerMgr.GetActor(printerMgr.DefaultPrinterId);
                if (printer != null) await printer.ChangeVisibilityAsync(false, 0.1f);
                Debug.Log("[GRS] printer hidden OK");
            }
            catch (System.Exception e) { Debug.LogWarning($"[GRS] printer hide failed: {e.Message}"); }
        }

        if (Assigned(ScriptName))
        {
            var sn = ScriptName.Value;
            var lb = Assigned(Label) ? Label.Value : null;
            DataService.Instance.afterChatScript = new ScriptParameter() { scriptName = sn, scriptLabel = lb };
            MapReturnPoint.Set(sn, lb);
            Debug.Log($"[GRS] afterChatScript & MapReturnPoint <- {sn}#{lb}");
        }
        else
        {
            Debug.Log("[GRS] No ScriptName assigned, skip afterChatScript");
        }

        var varMgr = Engine.GetService<ICustomVariableManager>();
        var canChat = Assigned(ScriptName).ToString();
        varMgr?.SetVariableValue("CanChat", canChat);
        Debug.Log($"[GRS] CanChat={canChat}");

        Debug.Log("[GRS] About to LoadSceneAsync(RestRoom)...");
        await SceneManager.LoadSceneAsync("RestRoom");
        Debug.Log("[GRS] RestRoom loaded.");
    }
}
