using UnityEngine;

public class RestButton : MonoBehaviour
{
    public void OnRestButtonClicked ()
    {
        if (!MapReturnPoint.HasValid()) return;

        var loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("[RestButton] 找不到 SceneLoader！");
            return;
        }

        loader.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
    }
}
