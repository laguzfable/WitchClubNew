using UnityEngine;
using UnityEngine.UI;
using Naninovel;

namespace Hexe.UI
{
    /// <summary>
    /// 依目前語系自動切換 Image 的圖。標題 logo 這種「圖片本身有文字」的素材用。
    ///
    /// 用法：掛在有 Image 的物件上，把三種語言的圖拖進對應欄位。
    /// 欄位留空的話會退回「中文」那張（zh-TW 是本專案的來源語系）。
    /// 日文版如果打算沿用英文 logo，就把日文欄位也指到英文那張。
    ///
    /// 語系判斷以 Naninovel 的 SelectedLocale 為準——設定選單的語言選項改的是它，
    /// PlayerPrefs 的 Language 是由 LocalePrefsSync 事後同步的，只讀 PlayerPrefs 會受
    /// 事件順序影響讀到舊值。引擎還沒起來時才退回 PlayerPrefs。
    /// （同 RuneEnTranslation.GetLang / TitleLabelInjector 的判法。）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizedSprite : MonoBehaviour
    {
        [Header("各語言的圖（留空＝用中文那張）")]
        [SerializeField] Sprite chinese;
        [SerializeField] Sprite english;
        [SerializeField] Sprite japanese;

        [Header("選項")]
        [Tooltip("換圖後是否自動套用該圖的原始尺寸。三張圖尺寸不一樣時勾起來，" +
                 "但版面是靠固定尺寸排的話就別勾。")]
        [SerializeField] bool setNativeSize = false;

        Image image;
        ILocalizationManager localization;

        void Awake ()
        {
            image = GetComponent<Image>();
        }

        void OnEnable ()
        {
            Apply();

            // 引擎可能還沒初始化完（這時 SelectedLocale 還讀不到），
            // 所以先用 PlayerPrefs 套一次，等引擎好了再套一次。
            if (Engine.Initialized) Hook();
            else Engine.OnInitializationFinished += Hook;
        }

        void OnDisable ()
        {
            Engine.OnInitializationFinished -= Hook;
            if (localization != null) localization.OnLocaleChanged -= HandleLocaleChanged;
            localization = null;
        }

        void Hook ()
        {
            Engine.OnInitializationFinished -= Hook;

            localization = Engine.GetService<ILocalizationManager>();
            if (localization != null) localization.OnLocaleChanged += HandleLocaleChanged;

            Apply();
        }

        void HandleLocaleChanged (string locale) => Apply();

        void Apply ()
        {
            if (image == null) return;

            var sprite = Pick();
            if (sprite == null || image.sprite == sprite) return;

            image.sprite = sprite;
            if (setNativeSize) image.SetNativeSize();
        }

        Sprite Pick ()
        {
            var lang = GetLang();

            if (lang.StartsWith("ja") && japanese != null) return japanese;
            if (lang.StartsWith("zh")) return chinese;
            if (!lang.StartsWith("zh") && english != null) return english;

            return chinese;   // 找不到對應語言的圖就用來源語系那張
        }

        static string GetLang ()
        {
            if (Engine.Initialized)
            {
                var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
                if (!string.IsNullOrEmpty(locale)) return locale.ToLower();
            }

            // 沒存過語言偏好時要當作中文，不能落到空字串——空字串不是 "zh" 開頭，會被當成英文
            return PlayerPrefs.GetString("Language", "zh-TW").ToLower();
        }
    }
}
