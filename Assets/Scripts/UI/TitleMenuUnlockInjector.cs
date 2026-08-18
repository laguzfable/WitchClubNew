using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 控制標題選單上兩顆「要解鎖才看得到」的按鈕。條件沒到就整顆隱藏（不是變灰）：
    ///
    ///   ‧ 女巫競技場（<see cref="TitleTowerModeButton"/>）→ 跑過**任一**結局（壞結局也算）
    ///   ‧ 蝕之聖典（GameObject 名 BranchMapUI）→ 必須拿到 **ACH_END_07「輪迴の鑰匙」**
    ///     （chapter5yellow 的 #eclipse）。是這一個特定結局，不是任一結局。
    ///
    /// ★ 判斷依據是 EndingRecord 不是 Steam ★
    /// AchievementManager.Unlock 在 Steam 沒初始化時會直接 return，拿它當條件的話
    /// 離線／Editor／還沒登入 Steam 就永遠解不開。EndingRecord 是同一個入口另外留的
    /// PlayerPrefs 紀錄，這些情況都算得出來。
    ///
    /// ★ 按鈕要自己在 TitleUI2.prefab 裡做好 ★
    /// 這支程式只負責開關，找不到按鈕只會印一行警告，不會幫你生。
    /// 實際生效的是 TitleUI2.prefab——TitleUI.prefab 沒有註冊在 EditorResources 裡，改它沒有用。
    /// 兩顆按鈕在 prefab 裡的預設狀態是「關閉」，解鎖了才由這裡打開，
    /// 這樣才不會在 UI 生出來到這支程式跑到之間閃一下。
    ///
    /// ★ 文字不歸這裡管 ★
    /// 標籤的三語切換由 <see cref="TitleLabelInjector"/> 處理（它找得到隱藏中的物件）。
    /// </summary>
    public static class TitleMenuUnlockInjector
    {
        /// <summary>蝕之聖典按鈕身上沒有可辨識的自訂元件（只有 Naninovel 的 PlayScript），
        /// 只能靠 GameObject 名字找。這個名字在 TitleUI2.prefab 裡是唯一的
        /// （TitleLabelInjector 也是用同一個名字找它）。</summary>
        const string CodexButtonName = "BranchMapUI";

        /// <summary>解開蝕之聖典的那個結局：ACH_END_07「輪迴の鑰匙」。</summary>
        const string CodexUnlockEnding = AchievementManager.ACH_END_07;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyRetryLoop().Forget();
        }

        /// <summary>
        /// 除錯用：進度被外部清掉之後（ProgressResetter 的 F10），立刻重新判斷一次。
        /// 平常是靠 sceneLoaded 觸發，但清進度時玩家人就站在標題畫面上、不會有場景切換，
        /// 沒有這個入口的話按鈕會一直停在「已解鎖」的樣子直到下次換場景。
        /// </summary>
        public static void Refresh () => ApplyRetryLoop().Forget();

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            // TitleUI 是 DontDestroyOnLoad，正常只會處理一次；但引擎重新初始化時會重建 UI，
            // 而且玩家可能在這一輪剛拿到解鎖用的結局，所以每次載場景都重新判斷一次。
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

            Debug.LogWarning("[TitleMenuUnlockInjector] 30 秒內都拿不到 TitleUI，隱藏模式按鈕的顯示狀態沒設定。");
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
            // 條件沒到時它本來就是關的，用預設參數會永遠找不到、也就永遠開不回來。
            var arena = titleUI.GetComponentInChildren<TitleTowerModeButton>(true);
            if (arena != null)
                SetVisible(arena.gameObject, EndingRecord.Count > 0, "女巫競技場",
                           $"目前結局數 {EndingRecord.Count}");
            else
                Debug.LogWarning("[TitleMenuUnlockInjector] TitleUI 底下找不到掛著 TitleTowerModeButton 的按鈕。" +
                                 "請在 TitleUI2.prefab 的 ButtonsPanel 裡放一顆按鈕並掛上該元件。");

            var codex = FindByName(titleUI.gameObject, CodexButtonName);
            if (codex != null)
                SetVisible(codex, EndingRecord.Has(CodexUnlockEnding), "蝕之聖典",
                           $"需要 {CodexUnlockEnding}");
            else
                Debug.LogWarning($"[TitleMenuUnlockInjector] TitleUI 底下找不到名為 {CodexButtonName} 的按鈕。");

            return true; // 找不到是結構問題，重試也沒用
        }

        static void SetVisible (GameObject button, bool unlocked, string label, string detail)
        {
            if (button.activeSelf == unlocked) return;

            button.SetActive(unlocked);
            Debug.Log($"[TitleMenuUnlockInjector] {label}按鈕 → {(unlocked ? "顯示" : "隱藏")}（{detail}）");
        }

        static GameObject FindByName (GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
