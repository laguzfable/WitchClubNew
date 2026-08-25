using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hexe.UI
{
    /// <summary>
    /// 把 <see cref="NewGameReset"/> 掛到標題的「新遊戲」按鈕上。
    ///
    /// ★ 為什麼要繞這一圈 ★
    /// 最直觀的做法是直接在 TitleNewGameButton.OnButtonClick 裡呼叫，但那支在
    /// Elringus.Naninovel.Runtime 這個 assembly definition 底下，看不到我們自己的
    /// 程式（相依方向是單向的：我們引用 Naninovel，不是反過來）。硬寫會編不過。
    /// 所以改成從我們這邊找到那顆按鈕、掛一個 onClick 監聽。
    ///
    /// ★ 執行順序 ★
    /// Naninovel 自己的監聽是在 Awake/OnEnable 掛的，比我們早，所以會先跑。
    /// 但它的處理是 async：真正開始播開場腳本要等 ResetStateAsync 完成，
    /// 而我們這邊全是同步的 PlayerPrefs 寫入，在那之前就做完了。
    ///
    /// ★ 只掛新遊戲，不掛回標題 ★
    /// @exitToTitle 也會 ResetState，但那不是開新一輪——玩家可能只是回標題去看聖典。
    /// 所以不能掛在 ResetState 上，一定要掛在這顆按鈕上。
    /// </summary>
    public static class NewGameResetHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += (_, __) => AttachRetryLoop().Forget();
            AttachRetryLoop().Forget();
        }

        static async UniTaskVoid AttachRetryLoop ()
        {
            const float timeout = 30f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TryAttach()) return;
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            Debug.LogWarning("[NewGameResetHook] 30 秒內找不到新遊戲按鈕，" +
                             "單周目進度不會在開新遊戲時清除。");
        }

        static bool TryAttach ()
        {
            var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() as MonoBehaviour;
            if (titleUI == null) return false;

            var newGame = titleUI.GetComponentInChildren<TitleNewGameButton>(true);
            if (newGame == null)
            {
                Debug.LogWarning("[NewGameResetHook] TitleUI 底下找不到 TitleNewGameButton。");
                return true; // 結構問題，重試也沒用
            }

            // UI 重建時會跑到新的物件上，所以每次都重掛；先減再加避免同一顆掛兩次。
            newGame.OnButtonClicked -= OnNewGame;
            newGame.OnButtonClicked += OnNewGame;
            return true;
        }

        static void OnNewGame ()
        {
            NewGameReset.Run();
        }
    }
}
