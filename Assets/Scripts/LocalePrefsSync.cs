using UnityEngine;
using Naninovel;

/// <summary>
/// 玩家在設定選單切換語言時，把 Naninovel 的 locale 一併寫回 PlayerPrefs「Language」。
///
/// 為什麼需要這個：
/// 專案裡有兩套語言來源。Naninovel 自己的 locale 由設定選單的語言選項控制；而
/// RuneEnTranslation（符文／卡片／護身符畫面）、TowerHubSceneManager（女巫競技場樓層文字）、
/// TutorialEnTranslation、DemoController 讀的都是 PlayerPrefs「Language」——那個 key
/// 原本只有開場的語言選擇畫面（UISelectLanguage）會寫。
/// 結果就是玩家中途在設定裡改語言，劇情會跟著換，但戰鬥和競技場那些畫面還停在舊語言。
///
/// 反方向（PlayerPrefs → locale）已經由 NaniScriptLoader_HEX 在啟動時處理，
/// 這裡補上正方向，兩邊就不會再分岔；順帶讓設定選單選的語言在下次啟動時也留得住。
///
/// 寫進去的值直接用 locale 代碼。SelectLanguage 場景那三顆按鈕填的就是
/// en / ja / zh-TW，跟 Naninovel 的代碼一模一樣，所有讀取端的
/// StartsWith("zh") / StartsWith("ja") 判斷都相容。
/// </summary>
public static class LocalePrefsSync
{
    const string LanguageKey = "Language";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register ()
    {
        // 不解除訂閱：引擎每次重新初始化都會重建服務，要再掛一次才不會失聯。
        Engine.OnInitializationFinished += Hook;
        if (Engine.Initialized) Hook();
    }

    static void Hook ()
    {
        var localization = Engine.GetService<ILocalizationManager>();
        if (localization == null) return;

        // 先減後加：重複呼叫（引擎重新初始化、或上面兩條路都走到）也只會有一份訂閱。
        localization.OnLocaleChanged -= Save;
        localization.OnLocaleChanged += Save;
    }

    static void Save (string locale)
    {
        if (string.IsNullOrEmpty(locale)) return;
        if (PlayerPrefs.GetString(LanguageKey, "") == locale) return;

        PlayerPrefs.SetString(LanguageKey, locale);
        PlayerPrefs.Save();
        Debug.Log($"[LocalePrefsSync] 語言偏好已同步為 '{locale}'");
    }
}
