using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 標題選單收起來時，把 Title 場景的裝飾物一起關掉；選單回來時再打開。
    ///
    /// ★ 解決什麼問題 ★
    /// Title 場景的背景、五個角色立繪、女巫俱樂部 logo 都是這個 Unity 場景裡的一般物件，
    /// 不歸 Naninovel 管。從標題「讀取存檔」時，Naninovel 只還原自己的狀態、不會換 Unity 場景，
    /// 所以劇情會直接演在標題美術上面（背景疊著標題畫面、logo 還掛在右上角）。
    /// 開新遊戲不會有這個問題，是因為開場腳本會載入別的場景，整個 Title 場景跟著被卸掉。
    ///
    /// ★ 為什麼用名字找、而且限定在 Title 場景裡找 ★
    /// 「Canvas」這種名字到處都是，用 GameObject.Find 會誤傷其他場景。
    /// 這裡只走 Title 場景的根物件，關掉的也只有下面列的那幾個。
    ///
    /// ★ 會記住原本的開關狀態 ★
    /// ColorWave 在場景裡本來就是關的。如果選單一顯示就無腦全開，等於把它打開了，
    /// 所以這裡記住每個物件一開始的狀態，選單回來時是「還原」而不是「全部打開」。
    /// </summary>
    public static class TitleSceneDecorToggler
    {
        const string TitleSceneName = "Title";

        /// <summary>Title 場景裡純裝飾、離開標題就該收起來的根物件。
        /// 其餘的（Main Camera / Directional Light / EventSystem / Audio /
        /// VisitedNodeManager / loader）都是功能物件，不能碰。</summary>
        static readonly string[] DecorRoots = { "Canvas", "TitleBokeh", "ColorWave" };

        static readonly List<(GameObject go, bool initial)> decor = new List<(GameObject, bool)>();
        static ScriptableUIBehaviour subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindRetryLoop().Forget();
        }

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            if (scene.name != TitleSceneName) return;

            // 場景重載了，之前記住的物件都是舊的（已銷毀），重抓一次
            decor.Clear();
            BindRetryLoop().Forget();
        }

        /// <summary>Title 場景載入的當下 Naninovel 的 UI 不一定就緒，重試到拿得到為止。</summary>
        static async UniTaskVoid BindRetryLoop ()
        {
            const float timeout = 30f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TryBind()) return;
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }
        }

        static bool TryBind ()
        {
            var scene = SceneManager.GetSceneByName(TitleSceneName);
            if (!scene.isLoaded) return true; // 不在標題場景，這次不用做事

            if (decor.Count == 0) Capture(scene);

            var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() as ScriptableUIBehaviour;
            if (titleUI == null) return false;

            // Unity 的 == 會把已銷毀的物件視為 null，所以這個比較也擋得掉「UI 被重建」
            if (subscribed != titleUI)
            {
                if (subscribed) subscribed.OnVisibilityChanged -= Apply;
                subscribed = titleUI;
                subscribed.OnVisibilityChanged += Apply;
            }

            Apply(titleUI.Visible);
            return true;
        }

        static void Capture (Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var name in DecorRoots)
                    if (root.name == name)
                        decor.Add((root, root.activeSelf));
        }

        static void Apply (bool titleVisible)
        {
            foreach (var (go, initial) in decor)
            {
                if (go == null) continue;

                var want = titleVisible && initial;
                if (go.activeSelf != want) go.SetActive(want);
            }
        }
    }
}
