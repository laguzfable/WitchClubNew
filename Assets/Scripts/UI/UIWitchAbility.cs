using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Kenaz;
using Naninovel;

public class UIWitchAbility : MonoBehaviour
{
    [SerializeField]
    ECardElement element;

    public string abilityID;

    public Ability ability { private set; get; }

    public UnitAttribute cost;

    //Slider slider;
    [SerializeField]
    Image fillImage;

    [SerializeField]
    float smoothSpeed = 1f;

    BaseCombatUnit unit;
    //public BaseCombatUnit unit;

    //[SerializeField]
    //GameObject fx;


    [SerializeField]
    GameObject specialFX;
    [SerializeField]
    float specialFXTime = 1f;
    [SerializeField]
    CGFadeHelper blackScreen;

    [HideInInspector]
    public bool isControllable = true;

    PlayerController pc;

    [SerializeField]
    Image fullChargedImage;

    private void Awake()
    {
        //slider = GetComponent<Slider>();
        cost.OnValueChanged += (value) =>
        {
            fillImage.DOFillAmount(cost.GetPercent(), 0.3f);
            if(cost.GetPercent() != 1f && fullChargedImage.color.a > 0f)
            {
                fullChargedImage.DOFade(0f, 0.15f);
            }
        };
        cost.OnValueFull += ()=>
        {
            var seq = DOTween.Sequence();
            seq.Append(fullChargedImage.DOFade(1f, 0.15f))
                .Append(transform.DOScale(1.2f, 0.15f))
                .AppendInterval(0.1f)
                .Append(transform.DOScale(1f, 0.2f));
            /*transform.DOScale(1.2f, 0.15f).onComplete += ()=>
            {
                transform.DOScale(1f, 0.2f);
            };*/
        };
        //Check(0f);
        abilityID = PlayerData.Instance.usingRuneIDs[(int)element];

        ability = DataService.Instance.GetAbilityById(abilityID);
        //Debug.Log($"abilityID : {abilityID} ability.requireEnergy : {ability.requireEnergy}");
        cost.SetBaseValue(ability.requireEnergy);
        fullChargedImage.DOFade(0f, 0.15f);
    }

    void Start()
    {
        //cost.Restore();
        pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        unit = pc.GetPlayerUnit();
    }

    /*
    public void Check(float newValue)
    {
        fx.SetActive(cost.Value == cost.GetTotalValue());
    /
    
    private void Update()
    {
        fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, 1 - cost.GetPercent(), smoothSpeed * Time.deltaTime);
    }
    */
    public void OnClick()
    {
        if (TutorialController.isTutorial2)
        {
            var tutorObj = pc.combatSystem.tutorController.curTutorialObj;
            if (tutorObj != null)
            {
                // 檢查id
                if(tutorObj.customActionID != "playRune" || element != ECardElement.Yellow)
                {
                    pc.combatSystem.SpawnSystemText("TUTORIAL_WARNING");
                    return;
                }
            }
            pc.combatSystem.tutorController.canGoNext = true;
        }
        //cost ability
        if (cost.Value == cost.GetTotalValue() && isControllable)
        {
            if(!pc.GetPlayerUnit().HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && pc.combatSystem.envEffect.curType == EEnvEffectType.NoRune)
            {
                pc.combatSystem.SpawnSystemText("CANT_USE_RUNE");
                return;
            }
            //cost.Value = 0f;
            pc.CostEN(cost.Value);
            SpecialFXCoroutine().Forget();
        }
    }

    async UniTaskVoid SpecialFXCoroutine()
    {
        if (specialFX != null)
        {
            CameraPlay.Shockwave(0.9f, 0.5f, 1.25f, 2f);
            CameraPlay.WidescreenH_ON(0.2f);
            blackScreen.FadeIn(0.15f);
            specialFX.SetActive(true);
            await UniTask.Delay(System.TimeSpan.FromSeconds(specialFXTime));
            specialFX.SetActive(false);
            blackScreen.FadeOut(0.15f);
            CameraPlay.WidescreenH_OFF(0.2f);
        }
        if (ability.specialFX != null)
        {
            var displayFX = ability.specialFX.GetComponent<FXSequence>();
            if (displayFX != null)
            {
                await FXSequence.PlayFX(displayFX);
            }
        }
        unit.CastAbility(ability);
        pc.CalculateAttr();
    }
}
