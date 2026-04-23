using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hexe.UI
{
    public class TitleTestEndingsButton : ScriptableButton
    {
        private TitleMenu titleMenu;
        private IScriptPlayer scriptPlayer;
        private IScriptManager scriptManager;
        private IStateManager stateManager;

        protected override void Awake()
        {
            base.Awake();
            titleMenu = GetComponentInParent<TitleMenu>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();
            scriptManager = Engine.GetService<IScriptManager>();
            stateManager = Engine.GetService<IStateManager>();
        }

        protected override async void OnButtonClick()
        {
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj == null) continue;
                if (obj.name.Contains("Title") || obj.name.Contains("Canvas"))
                    obj.SetActive(false);
            }

            titleMenu?.Hide();

            await stateManager.ResetStateAsync();
            var script = await scriptManager.LoadScriptAsync("test_endings");
            if (script == null)
            {
                Debug.LogError("[TitleTestEndingsButton] 找不到 test_endings 腳本，確認已登錄在 EditorResources。");
                return;
            }
            await scriptPlayer.PreloadAndPlayAsync(script, 0);
        }
    }
}
