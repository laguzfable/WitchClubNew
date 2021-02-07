using Naninovel;
using System.Collections;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;

public class NaniTestShit : MonoBehaviour
{
    public string defaultScript = "chapter0";

    // Start is called before the first frame update
    async void Start()
    {
        await RuntimeInitializer.InitializeAsync();

        Debug.Log("WTF?!");
        if (Engine.Initialized) DoMyCustomWork();
        else Engine.OnInitializationFinished += DoMyCustomWork;
    }

    private void DoMyCustomWork()
    {
        // Engine is initialized here, it's safe to use the APIs.
        var player = Engine.GetService<IScriptPlayer>();
        
        var paramArr = Toolbox.Instance.GetOrAddComponent<DataService>().paramArr;
        if (paramArr != null && !string.IsNullOrEmpty(paramArr[2]))
        {
            if(!string.IsNullOrEmpty(paramArr[3]))
            {
                player.PreloadAndPlayAsync(paramArr[2], label:paramArr[3]).Forget();
            }
            else
            {
                player.PreloadAndPlayAsync(paramArr[2]).Forget();
            }
        }
        else
        {
            var gotoScript = defaultScript;

            if (!string.IsNullOrEmpty(Toolbox.Instance.GetOrAddComponent<DataService>().startScript))
            {
                gotoScript = Toolbox.Instance.GetOrAddComponent<DataService>().startScript;
            }
            player.PreloadAndPlayAsync(gotoScript).Forget();
        }

        Debug.Log("Hey just play this shit!");
    }
}
