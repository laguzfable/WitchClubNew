using System.Linq;
using Naninovel;
using Naninovel.Commands;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

[CommandAlias("GotoUnityScene")]
public class GotoUnityScene : Command, Command.IForceWait
{
    public StringParameter SceneName;

    private const string TAG = "[GOTOSCENE]";

    public async override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log($"{TAG} ExecuteAsync START, target scene='{SceneName?.Value}'");
        try
        {
            // 1. Hide continue indicator (may be inactive, e.g. after @hidePrinter —
            //    FindObjectOfType() alone would miss it and NullReferenceException here
            //    would silently abort the whole scene switch, leaving the screen stuck).
            var continueInputUI = Resources.FindObjectsOfTypeAll<ContinueInputUI>()
                .FirstOrDefault(x => x.gameObject.scene.IsValid());
            if (continueInputUI != null)
                continueInputUI.Visible = false;
            Debug.Log($"{TAG} step 1 done (continueInputUI found={continueInputUI != null})");

            // 2. Stop script player.
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            scriptPlayer.Stop();
            Debug.Log($"{TAG} step 2 done (scriptPlayer stopped)");

            // 3. Reset state，保留 CustomVariable（affinity 等不能被清除）
            var stateManager = Engine.GetService<IStateManager>();
            await stateManager.ResetStateAsync(new[] { typeof(ICustomVariableManager) });
            Debug.Log($"{TAG} step 3 done (state reset)");

            // 4. Switch cameras.
            //var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
            //advCamera.enabled = true;
            var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
            if (naniCamera != null)
                naniCamera.enabled = false;
            Debug.Log($"{TAG} step 4 done (naniCamera found={naniCamera != null})");

            if (string.IsNullOrEmpty(SceneName?.Value))
            {
                Debug.LogError($"{TAG} ABORT: SceneName parameter is empty! Check the `@GotoUnityScene sceneName:...` call.");
                return;
            }

            Debug.Log($"{TAG} calling SceneManager.LoadSceneAsync('{SceneName.Value}')...");
            await SceneManager.LoadSceneAsync(SceneName.Value);
            Debug.Log($"{TAG} ExecuteAsync END — scene load awaited successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{TAG} EXCEPTION: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
    
}