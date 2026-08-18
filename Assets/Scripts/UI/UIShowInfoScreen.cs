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

        // 說明現在多了一行能量需求，而場景裡那個框是設「垂直截斷」的，
        // 說明本身長一點（會折行）就剛好把新那行切掉，所以放寬成往下溢出。
        if (infoText != null) infoText.verticalOverflow = VerticalWrapMode.Overflow;
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
        string desc = localization != null
            ? localization.GetLocalizedContent(localeKey, cardAbility.description)
            : cardAbility.description;

        // 說明底下再補一行「要多少能量才按得動」
        string result = desc + "\n" + GetEnergyCostLine(localization, witchAbility);

        string playerPrefsLang = PlayerPrefs.GetString("Language", "(none)");
        Debug.Log($"[InfoScreen] id={cardAbility.id} | key={localeKey} | PlayerPrefs.Language={playerPrefsLang} | naniLocale={localization?.GetCurLanguage()} | result={desc} | localizationNull={localization==null}");
        return result;
    }

    /// <summary>
    /// 「需要能量：X 點」那一行。
    /// 數字以 cost 為準而不是資料表的 requireEnergy——OnClick 判斷按不按得動比的就是 cost，
    /// requireEnergy 只是它的基底值（之後如果有東西加減符文費用，這裡會自動跟著對）。
    /// </summary>
    private string GetEnergyCostLine(CombatSceneLocalization localization, UIWitchAbility witchAbility)
    {
        int cost = Mathf.RoundToInt(witchAbility.cost.GetTotalValue());

        const string defaultTemplate = "需要能量：{0} 點";
        string template = localization != null
            ? localization.GetLocalizedContent("RUNE_ENERGY_COST", defaultTemplate)
            : defaultTemplate;

        // CSV 那筆被改壞（少了佔位符）的話，至少還看得到數字
        if (!template.Contains("{0}")) template += " {0}";

        return string.Format(template, cost);
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
