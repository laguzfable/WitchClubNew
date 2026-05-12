using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocaleUIText : MonoBehaviour
{
    public string localeID;

    Text txt;

    // Use this for initialization
    void Start()
    {
        txt = GetComponent<Text>();
        ApplyLocale();
    }

    public void ApplyLocale()
    {
        if (txt == null) txt = GetComponent<Text>();

        // 先用 Tag 找，找不到改用 FindObjectOfType（場景結構不同時仍可運作）
        var localization = GameObject.FindWithTag("GameController")
                               ?.GetComponent<CombatSceneLocalization>();
        if (localization == null)
            localization = FindObjectOfType<CombatSceneLocalization>();

        if (localization == null)
        {
            Debug.LogWarning($"[LocaleUIText] CombatSceneLocalization not found for id={localeID}");
            return;
        }

        txt.text = localization.GetLocalizedContent(localeID, txt.text);
    }
}