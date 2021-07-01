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
        if(!Engine.Initialized)
        {
            await RuntimeInitializer.InitializeAsync();
            //先用很爛的方式初始化
            PlayerData.Instance.usingRuneIDs[(int)ECardElement.Red] = "艾妮(血系)";
            PlayerData.Instance.usingRuneIDs[(int)ECardElement.Green] = "樹女";
            PlayerData.Instance.usingRuneIDs[(int)ECardElement.Blue] = "赫菲";
        }

        Debug.Log("WTF?!");
        if (Engine.Initialized) DoMyCustomWork();
        else Engine.OnInitializationFinished += DoMyCustomWork;
    }

    private void DoMyCustomWork()
    {
        // Engine is initialized here, it's safe to use the APIs.
        var player = Engine.GetService<IScriptPlayer>();
        
        var scriptParameter = Toolbox.Instance.GetOrAddComponent<DataService>().scriptParameter;
        if (scriptParameter != null && !string.IsNullOrEmpty(scriptParameter.scriptName))
        {
            if(!string.IsNullOrEmpty(scriptParameter.scriptLabel))
            {
                player.PreloadAndPlayAsync(scriptParameter.scriptName, label:scriptParameter.scriptLabel).Forget();
            }
            else
            {
                player.PreloadAndPlayAsync(scriptParameter.scriptName).Forget();
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
