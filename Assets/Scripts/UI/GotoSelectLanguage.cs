using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 掛在 Title 場景的「Play Demo」按鈕上。
/// 點擊後停止 Naninovel 並跳到 Demo 入口場景（SelectLanguage）。
/// </summary>
public class GotoSelectLanguage : MonoBehaviour
{
    public void OnClick()
    {
        // 停止 Naninovel（避免場景切換後殘留狀態）
        if (Naninovel.Engine.Initialized)
        {
            try
            {
                var player = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                player?.Stop();
            }
            catch { /* engine 尚未完全初始化時忽略 */ }
        }

        SceneManager.LoadSceneAsync("SelectLanguage", LoadSceneMode.Single);
    }
}
