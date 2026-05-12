using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIShowInfoScreen : MonoBehaviour
{
    [SerializeField]
    GameObject infoScreenObj;

    [HideInInspector]
    public string infoStr;

    [SerializeField]
    Text infoText;

    [SerializeField]
    Vector3 pos;

    GameObject currentObj;

    [SerializeField]
    string abilityID;

    [SerializeField]
    bool isDisplay = true;

    private void Start()
    {
        // 不在 Start 設定 infoStr，改為每次顯示時即時取得（確保語言正確）
    }

    private string GetLocalizedDesc()
    {
        if (string.IsNullOrEmpty(abilityID)) return "";

        var witchAbility = GetComponent<UIWitchAbility>();
        if (witchAbility == null) return "";

        Ability cardAbility = DataService.Instance.GetAbilityById(witchAbility.abilityID);

        // 先用 Tag 找，找不到改用 FindObjectOfType（場景結構不同時仍可運作）
        var localization = GameObject.FindWithTag("GameController")
                               ?.GetComponent<CombatSceneLocalization>();
        if (localization == null)
            localization = FindObjectOfType<CombatSceneLocalization>();

        string localeKey = $"ABILITY_{cardAbility.id}_DESC";
        string result = localization != null
            ? localization.GetLocalizedContent(localeKey, cardAbility.description)
            : cardAbility.description;

        string playerPrefsLang = PlayerPrefs.GetString("Language", "(none)");
        Debug.Log($"[InfoScreen] id={cardAbility.id} | key={localeKey} | PlayerPrefs.Language={playerPrefsLang} | naniLocale={localization?.GetCurLanguage()} | result={result} | localizationNull={localization==null}");
        return result;
    }

    public void ShowInfoScreen()
    {
        if(!isDisplay)
        {
            return;
        }
        infoStr = GetLocalizedDesc();
        infoScreenObj.SetActive(true);
        infoScreenObj.GetComponent<RectTransform>().localPosition = pos;
        infoText.text = infoStr;
        currentObj = gameObject;
    }

    public void HideInfoScreen()
    {
        if (!isDisplay)
        {
            return;
        }
        if (currentObj.name == gameObject.name)
        {
            infoScreenObj.SetActive(false);
            currentObj = null;
        }
    }
}
