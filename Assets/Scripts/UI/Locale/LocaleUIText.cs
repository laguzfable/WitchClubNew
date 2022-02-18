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

        var localization = GameObject.FindWithTag("GameController").GetComponent<CombatSceneLocalization>();
        txt.text = localization.GetLocalizedContent(localeID, txt.text);
    }
}