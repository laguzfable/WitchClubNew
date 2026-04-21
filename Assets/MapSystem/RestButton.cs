using Naninovel;
using UnityEngine;

public class RestButton : MonoBehaviour
{
    public async void OnRestButtonClicked ()
    {
        var player = Engine.GetService<IScriptPlayer>();
        if (MapReturnPoint.HasValid())
            await player.PreloadAndPlayAsync(
                MapReturnPoint.ScriptName, label: MapReturnPoint.Label);
    }
}
