using Naninovel;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 一句要分語言的話：中文、英文、日文各一格。
/// 目前用在怪物的「圖鑑台詞」。要加語言就在這裡加欄位，再補 <see cref="Get"/> 的判斷。
///
/// 某個語言還沒填的話退回中文——怪物名字現在也還沒翻，英文版看到的名字一樣是中文，
/// 台詞跟著退回中文至少是一致的。
/// </summary>
[System.Serializable]
public class LocalizedLine
{
    [LabelText("中文"), TextArea(1, 3)] public string zh;
    [LabelText("English"), TextArea(1, 3)] public string en;
    [LabelText("日本語"), TextArea(1, 3)] public string ja;

    /// <summary>照目前遊戲語言挑一句。三格都空就回傳空字串。</summary>
    public string Current => Get(CurrentLocale());

    public string Get (string locale)
    {
        locale = (locale ?? "").ToLowerInvariant();
        string picked = null;
        if (locale.StartsWith("en")) picked = en;
        else if (locale.StartsWith("ja")) picked = ja;
        return !string.IsNullOrWhiteSpace(picked) ? picked : zh ?? "";
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(zh) && string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(ja);

    // 跟 RuneEnTranslation.GetLang 同一套：以引擎的 SelectedLocale 為準，引擎還沒起來才看 PlayerPrefs。
    static string CurrentLocale ()
    {
        if (Engine.Initialized)
        {
            var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
            if (!string.IsNullOrEmpty(locale)) return locale;
        }
        return PlayerPrefs.GetString("Language", "zh-TW");
    }
}
