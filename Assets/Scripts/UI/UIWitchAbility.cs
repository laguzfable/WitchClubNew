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
    Debug.Log($"🔮🔮🔮 [RUNEDBG] UIWitchAbility.Awake() 執行了！element={element}, gameObject={gameObject.name}, scene={gameObject.scene.name}");

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
        DOTween.Sequence()
            .Append(fullChargedImage.DOFade(1f, 0.15f))
            .Append(transform.DOScale(1.2f, 0.15f))
            .AppendInterval(0.1f)
            .Append(transform.DOScale(1f, 0.2f));
    };

    // ✅ ✅ ✅ 只讀取裝備資料，不再強制覆蓋 usingRuneIDs
    string equipKey = $"Equipped_{element}";
    string savedEquip = PlayerPrefs.GetString(equipKey, "");

    if (!string.IsNullOrEmpty(savedEquip))
    {
        abilityID = savedEquip;
        PlayerData.Instance.usingRuneIDs[(int)element] = savedEquip;
    }
    else
    {
        // ✅ 沒有裝備資料 → 使用 PlayerData 現有值
        abilityID = PlayerData.Instance.usingRuneIDs[(int)element];
    }

    // ✅ 讀取能力資料
    ability = DataService.Instance.GetAbilityById(abilityID);

    // ✅ 設定符文能量
    cost.SetBaseValue(ability.requireEnergy);
    // 高塔模式裝了「起始能量全滿護符」的話，一開場符文就是充好的
    cost.Value = Hexe.TowerMode.TowerModeManager.ShouldStartWithFullRuneEnergy ? cost.GetTotalValue() : 0;

    Debug.Log($"🔮🔮🔮 [RUNEDBG2] element={element} abilityID='{abilityID}' ability.id='{ability.id}' ability.requireEnergy={ability.requireEnergy} cost.GetTotalValue()={cost.GetTotalValue()} cost.GetPercent()={cost.GetPercent()} fillImage.sprite={(fillImage.sprite == null ? "NULL" : fillImage.sprite.name)} fillImage.color={fillImage.color}");

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

            // ★ 效果一定要在這裡就生效，不能等演出播完 ★
            // 舊寫法是把 CastAbility 放在 SpecialFXCoroutine 的最後，而那支協程是 .Forget() 射後不理、
            // 中間還 await 了 1 秒以上的特寫演出，期間玩家完全沒被鎖住。手快的話可以在效果還沒套上去
            // 就選牌出牌，PlayCardAsync 檢查 HasEffect(Reflect/Shield) 全部是 false ——
            // 症狀就是「符文明明按了卻沒作用」，反彈類的還會直接被打死。
            unit.CastAbility(ability);
            pc.CalculateAttr();

            // 演出期間把操作鎖住，不然畫面被黑幕蓋著還能盲按出牌
            pc.SetControllable(false);
            SpecialFXCoroutine().Forget();
        }
    }

    public void RefreshRune()
{
    abilityID = PlayerData.Instance.usingRuneIDs[(int)element];
    ability = DataService.Instance.GetAbilityById(abilityID);
    cost.SetBaseValue(ability.requireEnergy);
    cost.Value = 0; // 重置能量避免舊值影響
}


    async UniTaskVoid SpecialFXCoroutine()
    {
        if (specialFX != null)
        {
            // 嘗試載入符文專屬圖片（monsters/{abilityID}）
            Sprite abilitySprite = Resources.Load<Sprite>($"monsters/{abilityID}");
            if (abilitySprite != null)
            {
                var img = specialFX.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = abilitySprite;
                    img.preserveAspect = true;
                }
            }

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
        // CastAbility 已經在 OnClick 就做掉了（理由見那邊的註解），這裡只負責演出收尾。
        // 演出途中如果戰鬥已經結束（例如直傷符文把敵人打死），就不要把操作解鎖回去。
        if (pc.combatSystem.isContinue)
            pc.SetControllable(true);
    }
}
