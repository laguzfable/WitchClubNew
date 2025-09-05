using Naninovel;
using UnityEngine;

public class RestButton : MonoBehaviour
{
    public async void OnRestButtonClicked ()
    {
        var player = Engine.GetService<IScriptPlayer>();
        if (!string.IsNullOrEmpty(MapReturnData.ScriptName))
            await player.PreloadAndPlayAsync(
                MapReturnData.ScriptName, MapReturnData.LineIndex);
    }
}
