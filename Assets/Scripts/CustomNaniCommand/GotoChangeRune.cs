using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// @GotoChangeRune scriptName:X label:Y
/// 進入換符文場景（ChangeRuneScene），結束後由確認按鈕透過 MapReturnPoint 返回指定劇本。
/// </summary>
[CommandAlias("GotoChangeRune")]
public class GotoChangeRune : Command, Command.IForceWait
{
    [ParameterAlias("scriptName")] public StringParameter ScriptName;
    [ParameterAlias("label")]      public StringParameter Label;

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var sn = Assigned(ScriptName) ? ScriptName.Value : null;
        var lb = Assigned(Label)      ? Label.Value      : null;

        if (!string.IsNullOrEmpty(sn))
        {
            MapReturnPoint.Set(sn, lb);
            if (DataService.Instance != null)
                DataService.Instance.scriptParameter = new ScriptParameter { scriptName = sn, scriptLabel = lb };
            Debug.Log($"[GotoChangeRune] MapReturnPoint <- {sn}#{lb}");
        }

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer?.SetSkipEnabled(false);

        // NOTE: Application.CanStreamedLevelBeLoaded is a legacy Web-Player API that
        // always returns false for regular scenes in Unity 2019+. Use LoadSceneAsync
        // directly; if the scene is missing from Build Settings it returns null.
        var _op = SceneManager.LoadSceneAsync("ChangeRuneScene");
        if (_op == null)
        {
            Debug.LogWarning("[GotoChangeRune] ChangeRuneScene 不在 Build Settings，直接返回劇本");
            NaniBridgeUtility.GoBackToSavedStory();
            return;
        }

        await _op;
    }
}
