using Naninovel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaniScriptLoader : MonoBehaviour
{
    public string defaultScript = "chapter0";

    // Start is called before the first frame update
    async void Start()
    {
        if(!Engine.Initialized)
        {
            await RuntimeInitializer.InitializeAsync();
            //先用很爛的方式初始化
            PlayerData.Instance.Reset();
        }

        // Debug.Log("WTF?!");
        if (Engine.Initialized) LoadNaniScript();
        else Engine.OnInitializationFinished += LoadNaniScript;
    }

    private void LoadNaniScript()
    {
        LoadNaniScriptAsync().Forget();
    }

    private async UniTaskVoid LoadNaniScriptAsync()
    {
        if(PlayerPrefs.HasKey("Language"))
        {
            var locale = PlayerPrefs.GetString("Language");
            if(locale != "Inited")
            {
                await Engine.GetService<ILocalizationManager>().SelectLocaleAsync(locale);
                locale = "Inited";
            }
        }

        // Engine is initialized here, it's safe to use the APIs.
        var player = Engine.GetService<IScriptPlayer>();

        var dataService = DataService.Instance;
        
        var scriptParameter = dataService.scriptParameter;
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

            if (!string.IsNullOrEmpty(dataService.startScript))
            {
                gotoScript = dataService.startScript;
            }
            player.PreloadAndPlayAsync(gotoScript).Forget();
        }

        // Debug.Log("Hey just play this shit!");
    }
}
