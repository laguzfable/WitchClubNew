using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 在 Title 場景載入完成後，自動複製一顆現有的標題按鈕，改造成「高塔模式」按鈕。
    /// 這樣就不用手動去改 TitleUI.prefab。
    /// 如果找不到範本按鈕，會在 Console 印警告，需要的話可以手動在 Title 場景加一顆
    /// Button，掛上 TitleTowerModeButton.cs 就好。
    /// </summary>
    public static class TitleTowerModeInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            Debug.Log("[TitleTowerModeInjector] Register() 已執行");
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 如果 Title 就是遊戲開機第一個場景，這個方法執行的當下它的
            // sceneLoaded 事件其實已經發生過了、上面訂閱會來不及接到，
            // 所以這裡再補檢查一次目前場景。
            Debug.Log($"[TitleTowerModeInjector] 目前場景：{SceneManager.GetActiveScene().name}");
            if (SceneManager.GetActiveScene().name == "Title")
                InjectRetryLoop().Forget();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[TitleTowerModeInjector] sceneLoaded: {scene.name}");
            if (scene.name != "Title") return;
            InjectRetryLoop().Forget();
        }

        /// <summary>
        /// TitleMenu 的按鈕有可能是 Naninovel 非同步建置的，2 幀不一定夠，
        /// 這裡改成最多重試 5 秒（每 0.2 秒試一次）。
        /// </summary>
        static async UniTaskVoid InjectRetryLoop()
        {
            const float timeout = 5f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (Inject()) return;
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f));
                elapsed += 0.2f;
            }

            Debug.LogWarning("[TitleTowerModeInjector] 5 秒內都找不到可複製的範本按鈕，放棄自動注入。" +
                "請手動在 Title 場景加一顆按鈕並掛上 TitleTowerModeButton.cs。");
        }

        /// <returns>true 表示已經成功注入（或本來就注入過了），不用再重試。</returns>
        static bool Inject()
        {
            if (Object.FindObjectOfType<TitleTowerModeButton>() != null)
            {
                Debug.Log("[TitleTowerModeInjector] 已經注入過了，略過");
                return true;
            }

            var template = FindTemplateButton();
            if (template == null)
            {
                Debug.Log("[TitleTowerModeInjector] 這輪還找不到範本按鈕，稍後重試...");
                return false;
            }

            Debug.Log($"[TitleTowerModeInjector] 找到範本按鈕：{template.name}，開始複製");

            var clone = Object.Instantiate(template, template.transform.parent);
            clone.name = "TitleTowerModeButton";

            // 把範本原本掛的所有 ScriptableButton 類型腳本都拔掉，換成我們自己的
            foreach (var btn in clone.GetComponents<ScriptableButton>())
                Object.Destroy(btn);

            clone.AddComponent<TitleTowerModeButton>();

            var text = clone.GetComponentInChildren<Text>();
            if (text != null) text.text = "高塔模式";
            else Debug.LogWarning("[TitleTowerModeInjector] 複製出來的按鈕找不到 Text 子物件，文字沒改到");

            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

            Debug.Log("[TitleTowerModeInjector] ✅ 高塔模式按鈕注入完成");
            return true;
        }

        static GameObject FindTemplateButton()
        {
            // 優先找「結局測試」那顆按鈕當範本
            var testEndings = Object.FindObjectOfType<TitleTestEndingsButton>();
            if (testEndings != null) return testEndings.gameObject;

            // 備案：找任何一顆掛在 TitleMenu 底下的 ScriptableButton 來當範本
            var titleMenu = Object.FindObjectOfType<TitleMenu>();
            if (titleMenu != null)
            {
                var anyButton = titleMenu.GetComponentInChildren<ScriptableButton>();
                if (anyButton != null) return anyButton.gameObject;
            }

            return null;
        }
    }
}
