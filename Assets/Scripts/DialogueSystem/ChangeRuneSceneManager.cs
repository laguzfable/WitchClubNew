using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 ChangeRuneScene 任意 GameObject 上。
/// 自動找到場景裡名為 "Button" 的確認按鈕，點擊後透過 MapReturnPoint 返回劇本。
/// </summary>
public class ChangeRuneSceneManager : MonoBehaviour
{
    void Start()
    {
        UnlockDemoRunes();

        // Translate all tab / button labels to English
        foreach (var t in FindObjectsOfType<Text>())
            t.text = RuneEnTranslation.TranslateName(t.text);
        // 依名稱找按鈕，支援「返回」或備援名「Button」
        var btnGO = GameObject.Find("返回") ?? GameObject.Find("Button");
        if (btnGO == null)
        {
            Debug.LogWarning("[ChangeRuneScene] 找不到返回按鈕（'返回' 或 'Button'）");
            return;
        }

        var btn = btnGO.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning("[ChangeRuneScene] 找到物件但沒有 Button 元件");
            return;
        }

        btn.onClick.AddListener(OnConfirm);
        Debug.Log("[ChangeRuneScene] 確認按鈕已綁定");
    }

    void OnConfirm()
    {
        Debug.Log("[ChangeRuneScene] 確認 → GoBackToSavedStory");
        NaniBridgeUtility.GoBackToSavedStory();
    }

    // Unlock demo runes: all except the last 2 of each faction
    static void UnlockDemoRunes()
    {
        // element 0 = Academy (blue), 1 = Blood (red), 2 = Demon (yellow), 3 = Nature (green), 4 = Other (mon)
        SetIfEmpty("UnlockedRunes_0", "blue01,blue02,blue03");
        SetIfEmpty("UnlockedRunes_1", "red01,red02,red03");
        SetIfEmpty("UnlockedRunes_2", "yellow01,yellow02,yellow03");
        SetIfEmpty("UnlockedRunes_3", "green01,green02,green03");
        SetIfEmpty("UnlockedRunes_4", "mon02,mon08,mon09,mon10,mon12");
        PlayerPrefs.Save();
    }

    static void SetIfEmpty(string key, string value)
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(key, "")))
            PlayerPrefs.SetString(key, value);
    }
}
