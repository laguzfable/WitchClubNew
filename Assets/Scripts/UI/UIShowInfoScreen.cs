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
        if(!string.IsNullOrEmpty(abilityID))
        {
            Ability cardAbility = DataService.Instance.GetAbilityById(GetComponent<UIWitchAbility>().abilityID);
            infoStr = cardAbility.description;
        }
    }

    public void ShowInfoScreen()
    {
        if(!isDisplay)
        {
            return;
        }
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
