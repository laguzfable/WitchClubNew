using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 控制標題選單「女巫競技場」按鈕的顯示與否：跑過任一結局才會出現（壞結局也算）。
    ///
    /// ★ 這支程式以前是「複製一顆按鈕出來」，現在改成「找到 prefab 裡既有的那顆並開關它」★
    /// 原因：複製出來的按鈕沒有自己的位置，完全靠 ButtonsPanel 的 Vertical Layout Group
    /// 自動排版。一旦想手動排版而關掉那個 LayoutGroup，複製品會繼承範本的座標、
    /// 直接疊在範本上面。改成用 prefab 裡真實存在的按鈕之後，位置、文字、樣式
    /// 都跟其他按鈕一樣可以在 Prefab Mode 裡直接排，不用記「有一顆是程式生的」。
    ///
    /// ★ 按鈕要自己在 TitleUI.prefab 裡做好 ★
    /// 這支程式只負責開關，找不到按鈕只會印一行警告，不會幫你生。
    /// 那顆按鈕身上要掛 <see cref="TitleTowerModeButton"/>，這裡就是靠這個元件型別找它的。
    ///
    /// ★ 文字不歸這裡管 ★
    /// 標籤的三語切換由 <see cref="TitleLabelInjector"/> 處理（它認得 TitleTowerModeButton
    /// 這個型別，而且找得到隱藏中的物件）。
    /// </summary>
    public static class TitleTowerModeInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyRetryLoop().Forget();
        }

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            // TitleUI 是 DontDestroyOnLoad，正常只會處理一次；但引擎重新初始化時會重建 UI，
            // 而且玩家可能在這一輪剛拿到第一個結局，所以每次載場景都重新判斷一次。
            ApplyRetryLoop().Forget();
        }

        static async UniTaskVoid ApplyRetryLoop ()
        {
            const float timeout = 30f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TryApply()) return;
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            Debug.LogWarning("[TitleTowerModeInjector] 30 秒內都拿不到 TitleUI，女巫競技場按鈕的顯示狀態沒設定。");
        }

        /// <returns>true 表示處理完了（含「找不到按鈕」這種再試也沒用的情況）。</returns>
        static bool TryApply ()
        {
            // 跟引擎要它掛在畫面上的那個 TitleUI，不要自己去場景裡撈：
            // Resources.FindObjectsOfTypeAll + scene.IsValid() 在 TitleUI 開在 Prefab Mode 時
            // 會抓到預覽副本，改到的是編輯器裡那份、不是遊戲中的。
            var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() as MonoBehaviour;
            if (titleUI == null) return false;

            // 用 true 這個參數才找得到「目前是隱藏狀態」的按鈕——
            // 沒有結局時它本來就是關的，用預設參數會永遠找不到、也就永遠開不回來。
            var button = titleUI.GetComponentInChildren<TitleTowerModeButton>(true);
            if (button == null)
            {
                Debug.LogWarning("[TitleTowerModeInjector] TitleUI 底下找不到掛著 TitleTowerModeButton 的按鈕。" +
                                 "請在 TitleUI.prefab 的 ButtonsPanel 裡放一顆按鈕並掛上該元件。");
                return true; // 結構問題，重試也沒用
            }

            // 壞結局也算：EndingRecord 只認 ACH_END_ 前綴，不分好壞
            var unlocked = EndingRecord.Count > 0;
            if (button.gameObject.activeSelf != unlocked)
            {
                button.gameObject.SetActive(unlocked);
                Debug.Log($"[TitleTowerModeInjector] 女巫競技場按鈕 → {(unlocked ? "顯示" : "隱藏")}" +
                          $"（目前結局數 {EndingRecord.Count}）");
            }

            return true;
        }
    }
}
