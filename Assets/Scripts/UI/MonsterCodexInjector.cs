using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 遊戲跑起來之後自動把 <see cref="MonsterCodexPanel"/> 掛到 Naninovel 的
    /// CGGalleryUI（回憶模式）上，這樣就不用去改 CGGalleryUI.prefab
    /// ——那是 Naninovel 套件自帶的檔案，之後升級版本比較不會衝突。
    /// 作法跟 <see cref="TitleTowerModeInjector"/> 一樣：重試迴圈等 UI 生出來。
    /// </summary>
    public static class MonsterCodexInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            InjectRetryLoop().Forget();
        }

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            // Naninovel 的 UI 是 DontDestroyOnLoad，正常只會注入一次；
            // 但引擎重新初始化時會重建 UI，所以每次載場景都再確認一遍。
            InjectRetryLoop().Forget();
        }

        static async UniTaskVoid InjectRetryLoop ()
        {
            const float timeout = 30f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (Inject()) return;
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));
                elapsed += 0.25f;
            }
        }

        /// <returns>true 表示已經注入好了（或本來就注入過了），不用再重試。</returns>
        static bool Inject ()
        {
            var gallery = FindGalleryPanel();
            if (gallery == null) return false;

            if (gallery.GetComponentInChildren<MonsterCodexPanel>(true) != null)
                return true;

            var browserPanel = gallery.transform.Find("BrowserPanel");
            if (browserPanel == null)
            {
                Debug.LogError("[MonsterCodexInjector] CGGalleryUI 底下找不到 BrowserPanel，怪物圖鑑分頁沒加上去。");
                return true; // 結構跟預期不一樣，再重試也沒用
            }

            browserPanel.gameObject.AddComponent<MonsterCodexPanel>();
            Debug.Log("[MonsterCodexInjector] ✅ 怪物圖鑑分頁注入完成");
            return true;
        }

        /// <summary>
        /// 回憶模式沒打開時 CGGalleryUI 是隱藏的，FindObjectOfType 抓不到，
        /// 所以用 FindObjectsOfTypeAll；但它連專案裡的 prefab 資產也會撈進來，
        /// 要靠 scene.IsValid() 過濾掉。
        /// </summary>
        static CGGalleryPanel FindGalleryPanel ()
        {
            foreach (var panel in Resources.FindObjectsOfTypeAll<CGGalleryPanel>())
                if (panel != null && panel.gameObject.scene.IsValid())
                    return panel;
            return null;
        }
    }
}
