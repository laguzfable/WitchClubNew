using UnityEngine;
using UnityEngine.UI;
using Naninovel;

namespace Hexe.UI
{
    /// <summary>
    /// 設定畫面的「跳過已觀看教學」。掛在 SettingsUI2.prefab 的 SkipTutorialDropdown 上，
    /// 那一列是從「略過模式」複製出來的，所以外觀跟其他設定項一致。
    ///
    /// 值存在 <see cref="TutorialRecord"/>（PlayerPrefs），不是 Naninovel 的設定檔——
    /// 教學看過沒有是玩家帳號層級的事，跟著存檔跑反而奇怪。預設是「跳過」。
    ///
    /// 文字自己處理三語：這一列是專案自己加的，標籤上的 ManagedTextProvider 已經拿掉了
    /// （留著的話它會在語系切換時把字蓋回「略過模式」）。
    /// </summary>
    public class SettingsSkipSeenTutorials : MonoBehaviour
    {
        const string LabelName = "SkipTutorialLabel";

        Dropdown dropdown;

        void Awake ()
        {
            dropdown = GetComponent<Dropdown>();
            if (dropdown == null)
            {
                Debug.LogWarning("[SkipSeenTutorials] 這個物件上沒有 Dropdown。");
                return;
            }

            dropdown.onValueChanged.AddListener(OnChanged);
        }

        // 每次設定畫面打開都會 OnEnable，趁這時同步目前的值與語系。
        void OnEnable ()
        {
            if (dropdown == null) return;

            var options = new System.Collections.Generic.List<string> {
                Pick("跳過", "Skip", "スキップ"),          // 0
                Pick("每次播放", "Always Play", "毎回再生"), // 1
            };

            dropdown.onValueChanged.RemoveListener(OnChanged);
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = TutorialRecord.SkipSeenEnabled ? 0 : 1;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnChanged);

            var label = transform.parent != null ? transform.parent.Find(LabelName) : null;
            var text = label != null ? label.GetComponent<Text>() : null;
            if (text != null)
                text.text = Pick("跳過已觀看教學", "Skip Seen Tutorials", "既読チュートリアル");
        }

        void OnDestroy ()
        {
            if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnChanged);
        }

        void OnChanged (int value)
        {
            TutorialRecord.SkipSeenEnabled = value == 0;
            Debug.Log($"[SkipSeenTutorials] 設定 → {(value == 0 ? "跳過已看過的教學" : "每次都播")}");
        }

        /// <summary>語系判斷比照 TitleLabelInjector：以 Naninovel 的 SelectedLocale 為準。</summary>
        static string Pick (string zh, string en, string ja)
        {
            var lang = PlayerPrefs.GetString("Language", "zh-TW").ToLower();
            if (Engine.Initialized)
            {
                var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
                if (!string.IsNullOrEmpty(locale)) lang = locale.ToLower();
            }

            if (lang.StartsWith("ja")) return ja;
            if (lang.StartsWith("zh")) return zh;
            return en;
        }
    }
}
