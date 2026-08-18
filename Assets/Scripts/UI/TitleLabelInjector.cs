using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Naninovel;
using Naninovel.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 標題選單裡「沒有 ManagedTextProvider」的那兩顆按鈕的多國語系：蝕之聖典、女巫競技場。
    ///
    /// ★ 其他按鈕不歸這裡管 ★
    /// NEW GAME / CONTINUE / SETTINGS / EXIT / 回憶模式 / 結局測試 / TIPS / EXTERNAL SCRIPTS
    /// 的標籤上都掛著 Naninovel 的 ManagedTextProvider，它會在引擎初始化與語系切換時，
    /// 用 GetRecordValue(key, "DefaultUI") 的結果（查不到就用元件上的 defaultValue）
    /// 透過 UnityEvent 呼叫 Text.set_text 覆蓋掉標籤——prefab 的 m_Text 在執行期是無效的。
    /// 那個 UnityEvent 是 RuntimeOnly，所以編輯器裡看到的是 m_Text、遊戲裡看到的是 provider 寫的值，
    /// 兩邊會不一致。那些字現在改由 Resources/Naninovel/Text/DefaultUI.txt 及
    /// Localization/<locale>/Text/DefaultUI.txt 提供，不要在這裡重複一份。
    ///
    /// 這兩顆之所以要自己處理：
    /// ‧ 蝕之聖典（BranchMapUI）是從 ContinueButton 複製出來的，身上的 provider 被移掉了。
    /// ‧ 女巫競技場（TitleTowerModeButton）在 prefab 裡預設是關閉的，由
    ///   <see cref="TitleMenuUnlockInjector"/> 判斷結局條件後才打開，身上同樣沒有 provider。
    ///
    /// ★ 找按鈕的方式 ★
    /// 蝕之聖典用 GameObject 名字找：它身上沒有可辨識的自訂元件（只有 Naninovel 的 PlayScript），
    /// 而它的標籤跟真正的 ContinueButton 一樣叫 ContinueLabel，用名字找標籤會抓錯。
    /// BranchMapUI 這個名字在 prefab 裡是唯一的。女巫競技場則用它自己的元件型別找。
    /// </summary>
    public static class TitleLabelInjector
    {
        // 給 TitleMenuUnlockInjector 用（按鈕被打開的當下就要有正確的字）
        public static string Arena => Pick("女巫競技場", "Witch Arena", "魔女闘技場");
        static string Codex => Pick("蝕之聖典", "Codex of the Eclipse", "蝕の聖典");

        /// <summary>
        /// 語系以 Naninovel 的 SelectedLocale 為準——設定選單的語言選項改的是它，
        /// 而 PlayerPrefs 的 Language 只有開場的語言選擇畫面會寫。只讀 PlayerPrefs 的話，
        /// 玩家在設定裡切語言，這兩顆按鈕不會跟著變，選單就會中英文混雜。
        /// 引擎還沒起來時才退回 PlayerPrefs（順序同 CombatSceneLocalization.GetCurLanguage）。
        /// </summary>
        static string GetLang ()
        {
            if (Engine.Initialized)
            {
                var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
                if (!string.IsNullOrEmpty(locale)) return locale.ToLower();
            }

            // 沒存過語言偏好時要當作中文，不能落到空字串——空字串不是 "zh" 開頭，會被當成英文。
            return PlayerPrefs.GetString("Language", "zh-TW").ToLower();
        }

        static string Pick (string zh, string en, string ja)
        {
            var lang = GetLang();
            if (lang.StartsWith("ja")) return ja;
            if (lang.StartsWith("zh")) return zh;
            return en;
        }

        static GameObject titleRoot;
        static ScriptableUIBehaviour subscribedUI;
        static ILocalizationManager subscribedLoc;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindRetryLoop().Forget();
        }

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            // TitleUI 是 DontDestroyOnLoad，正常只會綁一次；
            // 但引擎重新初始化時會重建 UI，所以每次載場景都再確認一遍。
            BindRetryLoop().Forget();
        }

        /// <summary>
        /// 每幀試一次，直到拿得到 TitleUI。刻意不拉間隔——間隔多久，未翻譯的原文就會被看到多久。
        /// </summary>
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

            Debug.LogWarning("[TitleLabelInjector] 30 秒內都拿不到 TitleUI，蝕之聖典／女巫競技場維持原文。");
        }

        static bool TryBind ()
        {
            // 跟引擎要它掛在畫面上的那個 TitleUI，不要自己去場景裡撈：
            // Resources.FindObjectsOfTypeAll + scene.IsValid() 在「TitleUI 開在 Prefab Mode」時
            // 會抓到預覽副本（預覽場景的 IsValid() 也是 true），log 顯示成功、畫面卻沒變。
            var titleUI = Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() as ScriptableUIBehaviour;
            if (titleUI == null) return false;

            titleRoot = titleUI.gameObject;

            // Unity 的 == 會把已銷毀的物件視為 null，所以這個比較也擋得掉「UI 被重建」的情況
            if (subscribedUI != titleUI)
            {
                if (subscribedUI) subscribedUI.OnVisibilityChanged -= OnTitleVisibilityChanged;
                subscribedUI = titleUI;
                subscribedUI.OnVisibilityChanged += OnTitleVisibilityChanged;
            }

            // 玩家在設定裡切語言時立刻跟著換。ManagedTextProvider 也是掛這個事件，
            // 所以整排按鈕會在同一時間一起變，不會有人慢半拍。
            var loc = Engine.GetService<ILocalizationManager>();
            if (loc != null && !ReferenceEquals(loc, subscribedLoc))
            {
                if (subscribedLoc != null) subscribedLoc.OnLocaleChanged -= OnLocaleChanged;
                subscribedLoc = loc;
                subscribedLoc.OnLocaleChanged += OnLocaleChanged;
            }

            ApplyAll();
            return true;
        }

        static void OnTitleVisibilityChanged (bool visible)
        {
            // SetVisibility 是在該幀繪製前呼叫的，所以在這裡改字不會被玩家看到中間狀態。
            if (visible) ApplyAll();
        }

        static void OnLocaleChanged (string locale) => ApplyAll();

        static void ApplyAll ()
        {
            if (titleRoot == null) return;

            Apply(ByName("BranchMapUI"), Codex);
            Apply(ByType<TitleTowerModeButton>(), Arena);
        }

        static GameObject ByType<T> () where T : MonoBehaviour
        {
            var c = titleRoot.GetComponentInChildren<T>(true);
            return c ? c.gameObject : null;
        }

        static GameObject ByName (string name)
        {
            foreach (var t in titleRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        /// <summary>找不到就安靜跳過：女巫競技場要跑過結局才會被注入，沒有它是正常狀態。</summary>
        static void Apply (GameObject button, string label)
        {
            if (button == null) return;

            var text = button.GetComponentInChildren<Text>(true);
            if (text == null || text.text == label) return;

            text.text = label;
        }
    }
}
