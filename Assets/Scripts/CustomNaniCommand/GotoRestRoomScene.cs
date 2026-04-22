using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// �Ą����M����Ϣ�ҡ��Ɏ� ScriptName/Label �O�� afterChatScript����������֧����
/// ���������������c��MapReturnPoint�����f�� Naninovel��AsyncToken + IForceWait��
/// �÷���@Restroom ScriptName:"yellow01" Label:"night1"
/// </summary>
[CommandAlias("Restroom")]
public class GotoRestRoomScene : Command, Command.IForceWait
{
    [ParameterAlias("ScriptName")] public StringParameter ScriptName;
    [ParameterAlias("Label")]      public StringParameter Label;

    public async override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        var bgm          = Engine.GetService<IBackgroundManager>();
        var printerMgr   = Engine.GetService<ITextPrinterManager>();

        Debug.Log($"[GRS] Restroom called. Assigned? ScriptName={Assigned(ScriptName)}, Label={Assigned(Label)}");

        // �P�]���D�������c���ֿ򣨸���ԭʼ�О�һ�£�
        scriptPlayer.SetSkipEnabled(false);
        if (bgm != null && bgm.GetActor(BackgroundsConfiguration.MainActorId) != null)
            bgm.GetActor(BackgroundsConfiguration.MainActorId).Visible = false;

        if (printerMgr != null && printerMgr.DefaultPrinterId != null)
        {
            try { await printerMgr.GetActor(printerMgr.DefaultPrinterId).ChangeVisibilityAsync(false, 0.1f); }
            catch { }
        }

        // ���Ў��_���������O�� afterChatScript���o��Ϣ������ʹ�ã�
        if (Assigned(ScriptName))
        {
            var sn = ScriptName.Value;
            var lb = Assigned(Label) ? Label.Value : null;
            DataService.Instance.afterChatScript = new ScriptParameter() { scriptName = sn, scriptLabel = lb };
            MapReturnPoint.Set(sn, lb);
            Debug.Log($"[GRS] afterChatScript <- {sn}#{lb}");
        }

        // �Q���Ƿ�������
        var canChat = Assigned(ScriptName).ToString();
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", canChat);
        Debug.Log($"[GRS] Set CanChat={canChat}");

        await SceneManager.LoadSceneAsync("RestRoom");
        Debug.Log("[GRS] Loaded 'RestRoom'.");
    }
}
