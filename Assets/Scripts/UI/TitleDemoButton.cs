using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 掛在 TitleUI prefab 的「Play Demo」按鈕上。
/// 點擊後重置 Naninovel 狀態，跳轉到 Demo 入口場景（SelectLanguage）。
/// </summary>
public class TitleDemoButton : ScriptableButton
{
    private IStateManager stateManager;
    private IScriptPlayer scriptPlayer;
    private TitleMenu titleMenu;

    protected override void Awake()
    {
        base.Awake();
        stateManager  = Engine.GetService<IStateManager>();
        scriptPlayer  = Engine.GetService<IScriptPlayer>();
        titleMenu     = GetComponentInParent<TitleMenu>();
    }

    protected override async void OnButtonClick()
    {
        // 先把 TitleMenu 藏起來，避免殘留在畫面上
        titleMenu?.Hide();

        // 停止目前劇本、重置狀態（不帶 save）
        scriptPlayer?.Stop();
        if (stateManager != null)
            await stateManager.ResetStateAsync();

        SceneManager.LoadSceneAsync("SelectLanguage", LoadSceneMode.Single);
    }
}
