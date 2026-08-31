// Assets/Scripts/UI/TitleBgm.cs
//
// 標題畫面的背景音樂。
//
// ★ 為什麼不是寫在 title.nani ★
// 標題是一個 Unity 場景（Scenes/Title.unity），不是 Naninovel 腳本演的——
// ScriptsConfiguration 的 TitleScript 是空的，title.nani 目前沒有任何地方會播它。
// 場景裡也沒有 AudioSource。所以這裡用場景載入事件掛進去，不必改場景檔。
//
// 音樂走 Naninovel 的 IAudioManager，跟其他地方同一套：音量設定、淡入淡出、
// 存檔狀態才會一致。

using Naninovel;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TitleBgm
{
    const string SceneName = "Title";
    const string Track = "mainmenu";      // Sound/BGM/mainmenu.mp3，有註冊在 EditorResources
    const float FadeIn = 0.8f;
    const float FadeOut = 0.4f;

    static bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook ()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;
        PlayAsync().Forget();
    }

    static void OnSceneUnloaded (Scene scene)
    {
        if (scene.name != SceneName || !playing) return;
        playing = false;

        // 只停這一首，不要 StopAllBgm——離開標題往往是進劇本，
        // 那邊的第一行 @bgm 可能已經下去了，全停會把它一起收掉。
        try
        {
            var audio = Engine.Initialized ? Engine.GetService<IAudioManager>() : null;
            audio?.StopBgmAsync(Track, FadeOut).Forget();
        }
        catch (System.Exception e) { Debug.LogWarning($"[TitleBgm] 停音樂失敗：{e.Message}"); }
    }

    /// <summary>引擎有可能還沒起來（遊戲第一次啟動時標題就是第一個場景），等它一下。</summary>
    static async UniTaskVoid PlayAsync ()
    {
        for (var i = 0; i < 300 && !Engine.Initialized; i++)   // 最多等 5 秒
            await UniTask.Yield();

        if (!Engine.Initialized) { Debug.LogWarning("[TitleBgm] 引擎沒起來，放棄播放"); return; }
        if (SceneManager.GetActiveScene().name != SceneName) return;   // 等的期間已經離開了

        try
        {
            var audio = Engine.GetService<IAudioManager>();
            if (audio == null) return;
            await audio.PlayBgmAsync(Track, volume: 1f, fadeTime: FadeIn, loop: true);
            playing = true;
            Debug.Log($"[TitleBgm] 標題音樂：{Track}");
        }
        catch (System.Exception e) { Debug.LogWarning($"[TitleBgm] 播放失敗：{e.Message}"); }
    }
}
