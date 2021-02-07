using Kenaz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum EDisplayAttribute { HP, SP }

public class UIAttribute : MonoBehaviour {

    [SerializeField]
    string targetTag;

    BaseCombatUnit target;

    Slider slider;

    [SerializeField]
    float smoothSpeed = 1f;

    [SerializeField]
    EDisplayAttribute displayAttr;

    UnitAttribute targetAttr;

    [SerializeField]
    Text txt;

    CombatSystem combatSys;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        combatSys = GameObject.FindWithTag("GameController").GetComponent<CombatSystem>();
        target = GameObject.FindGameObjectWithTag(targetTag).GetComponent<BaseCombatUnit>();

        if (target != null)
        {
            switch (displayAttr)
            {
                case EDisplayAttribute.HP:
                    targetAttr = target.HP;
                    break;
                /*case EDisplayAttribute.SP:
                    targetAttr = target.SP;
                    break;*/
            }

            slider.value = targetAttr.GetPercent();

            targetAttr.OnValueChanged += (value) =>
            {
                slider.DOValue(targetAttr.GetPercent(), 0.3f);
                if (txt != null)
                {
                    txt.text = targetAttr.Value.ToString("N0") + "/" + targetAttr.GetTotalValue().ToString("N0");
                }
                //target.CheckHpStatus();
            };
            if(txt != null)
            {
                txt.text = targetAttr.Value.ToString("N0") + "/" + targetAttr.GetTotalValue().ToString("N0");
            }
        }
    }
}
