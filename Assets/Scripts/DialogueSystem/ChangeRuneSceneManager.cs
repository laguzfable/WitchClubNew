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

    // Unlock all demo runes every time (always overwrite for demo)
    // Key format must match SelectRuneCard: $"UnlockedRunes_{ability.element}" → enum name
    static void UnlockDemoRunes()
    {
        PlayerPrefs.SetString("UnlockedRunes_Blue",   "blue01,blue02,blue03,blue04,blue05");
        PlayerPrefs.SetString("UnlockedRunes_Red",    "red01,red02,red03,red04,red05");
        PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
        PlayerPrefs.SetString("UnlockedRunes_Green",  "green01,green02,green03,green04,green05");
        PlayerPrefs.SetString("UnlockedRunes_None",   "mon02,mon04,mon08,mon09,mon10,mon12");
        PlayerPrefs.Save();
        Debug.Log("[ChangeRuneScene] 全部符文已解鎖 (Blue/Red/Yellow/Green/None)");
    }
}
