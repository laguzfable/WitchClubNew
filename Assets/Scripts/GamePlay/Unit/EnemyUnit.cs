using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Text;
using System.Linq;
using Kenaz;
using Naninovel;
using Hexe.TowerMode;

public class EnemyUnit : BaseCombatUnit
{
    float theta = 0f;

    [SerializeField] float speed = 1f;

    [SerializeField] float offset = 0.5f;

    float orgY;

    public bool isMovable = true;

    public Transform fxPos;
    

    [SerializeField]
    FXSequence fxSeq;

    [SerializeField]
    List<AudioClip> audioClipList;
    AudioSource audioSource;

    public SpriteRenderer sprRend {private set; get; }

    MobData mobData;

    static readonly int MaxCardCount = 5;

    [SerializeField] UIStatus status;
    
    class MobCard
    {
        public ECardElement element;
        public bool isUse = false;
        public CardAttribute levelUpBonusAttr;
        public int level = 1;

        public void Reset()
        {
            levelUpBonusAttr.Init();
            level = 1;
            isUse = false;
        }

        public CardAttribute GetAttr(CardAttribute dataValue)
        {
            levelUpBonusAttr.ATK += dataValue.ATK;
            levelUpBonusAttr.DEF += dataValue.DEF;
            levelUpBonusAttr.HEAL += dataValue.HEAL;
            levelUpBonusAttr.EN += dataValue.EN;

            return levelUpBonusAttr;
        }

        public void LevelUp(MobData mobData)
        {
            if(level == 5)
            {
                return;
            }
            level++;

            var rndEleList = new List<int>{0, 1};

            if(element == ECardElement.Green)
            {
                rndEleList.Add(2);
                // rndEleList = new List<int>{1, 2};
            }
            // else if(element == ECardElement.Yellow)
            // {
            //     rndEleList.Add(3);
            // }

            var rndIndex = Random.Range(0, rndEleList.Count);
            switch(rndEleList[rndIndex])
            {
                case 0:
                    levelUpBonusAttr.ATK += Random.Range(1, mobData.levelUpMaxBonusValue);
                    break;
                case 1:
                    levelUpBonusAttr.DEF += Random.Range(1, mobData.levelUpMaxBonusValue);
                    break;
                case 2:
                    levelUpBonusAttr.HEAL += Random.Range(1, mobData.levelUpMaxBonusValue);
                    break;
                case 3:
                    levelUpBonusAttr.EN += 1;
                    break;
            }
        }
    }

    MobCard[] cardArr = new MobCard[MaxCardCount];

    MobActionResult actResult = new MobActionResult();

    // ability energy
    UnitAttribute EN = new UnitAttribute();

    // AI決定元素用權重表
    Dictionary<ECardElement, RandomTool.RandomObject> eleDecisionRndMap = new Dictionary<ECardElement, RandomTool.RandomObject>();

    // 洗卡牌用權重表
    List<RandomTool.RandomObject> cardRndList = new List<RandomTool.RandomObject>();

    private void Awake()
    {
        audioSource = this.GetOrAddComponent<AudioSource>();
        sprRend = GetComponent<SpriteRenderer>();

        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerUnit>();
        }

        AddOnAddEffectEvent(EAbilityEffectType.Stun, () => combatSystem.UpdateMobActionInfo(GetActionString()));
        AddOnAddEffectEvent(EAbilityEffectType.NoArmor, () =>
        {
            actResult.attr.DEF = 0;
            combatSystem.UpdateMobActionInfo(GetActionString());
        });
        // AddEffectEvent(EAbilityEffectType.BreakAction, () => combatSystem.SpawnCombatText("成功打斷行動", ECombatTextType.Debuff, false));
        void RemovedMagicArmor()
        {
            actResult.Reset();

            var eff = AbilityEffectRef.Create(EAbilityEffectType.BreakAction);
            // eff.duration = 1;
            AddEffect(eff);

            //中斷的下一回合會昏迷 所以
            var stunEff = AbilityEffectRef.Create(EAbilityEffectType.Stun);
            stunEff.duration = 2;
            AddEffect(stunEff);
        }
        AddOnRemoveEffectEvent(EAbilityEffectType.MagicArmor, RemovedMagicArmor);
        //AddOnRemoveEffectEvent(EAbilityEffectType.MagicArmorEX, RemovedMagicArmor);
    }

    [SerializeField] string testMobName = "TestMobData";
    protected override void Init()
    {
        orgY = transform.position.y;
        if(!TutorialController.isTutorial && !TutorialController.isTutorial2 && !combatSystem.IsTestMode)
        {
            var sp = DataService.Instance?.scriptParameter;
            var mobName = sp?.combatTarget?.Value ?? "";
            Debug.Log($"[EnemyUnit.Init] combatTarget='{mobName}'  scriptParameter={(sp==null?"null":"ok")}");

            var data = string.IsNullOrEmpty(mobName) ? null : Resources.Load<MobData>($"MobData/{mobName}");
            mobData = data != null? data : Resources.Load<MobData>($"MobData/{testMobName}");
            Debug.Log($"mobData : {(mobData==null?"null":mobData.name)}");
            if (mobData == null)
            {
                Debug.LogError($"[EnemyUnit.Init] 找不到 MobData！mobName='{mobName}'  testMobName='{testMobName}'  → 強制使用預設值");
                base.Init();
                return;
            }
            // 結局王的血量會被第二章賣掉哪本書影響，其他敵人原樣照 MobData。
            HP.SetBaseValue(BossPower.ApplyHP(mobData.name, mobData.HP));
            EN.SetBaseValue(mobData.EN);

            sprRend.sprite = mobData.sprite;//combatSystem.visualResource.GetMobByName(mobName);
            sprRend.enabled = true;

            if (TowerModeManager.IsActive)
            {
                // 競技場只跟池子借立繪，數值改走競技場自己的基準表（見 GetArenaMobData 的說明）。
                // 順序有意義：先把立繪吃進來，再換掉 mobData，這樣抽到的還是池子裡那隻的外觀。
                var towerSprite = TowerModeManager.RollMonsterSprite();
                if (towerSprite != null) sprRend.sprite = towerSprite;

                var arenaData = TowerModeManager.GetArenaMobData();
                if (arenaData != null) mobData = arenaData;
                else Debug.LogError("[EnemyUnit.Init] 載不到 Resources/TowerMode/ArenaMob，競技場會退回用池子裡那隻的數值");

                HP.SetBaseValue(TowerModeManager.RollMonsterHP(mobData.HP));
                EN.SetBaseValue(mobData.EN);

                // 競技場的立繪是另外抽的，看畫面認不出實際在打誰，測試時以這行為準
                Debug.Log($"[EnemyUnit.Init] 競技場: 立繪來自 '{mobName}'，數值來自 '{mobData.name}' (HP {HP.GetTotalValue()})");
            }
            else
            {
                // 高塔（女巫競技場）的怪是隨機湊出來的，不算劇情上「遇過」，所以不解鎖圖鑑。
                Hexe.UI.MonsterCodex.RecordSeen(mobData.name);
            }
        }
        else
        {   // 測試用
            mobData = Resources.Load<MobData>($"MobData/{testMobName}");
            HP.SetBaseValue(mobData.HP);
            EN.SetBaseValue(mobData.EN);
            sprRend.enabled = combatSystem.IsTestMode;
        }
        
        base.Init();

        //卡牌部分初始化
        for(var i = 0; i < MaxCardCount; i++)
        {
            cardArr[i] = new MobCard();
        }
        
        for (int i = 0; i < (int)ECardElement.None; i++)
        {
            var rndObj = new RandomTool.RandomObject();
            rndObj.SetIndex((int)mobData.elementData[i].element);
            rndObj.Weight = mobData.elementData[i].randomWeight;
            cardRndList.Add(rndObj);
        }
        
        for(var i = 0; i < (int)ECardElement.None; i++)
        {
            eleDecisionRndMap[(ECardElement)i] = new RandomTool.RandomObject();
            eleDecisionRndMap[(ECardElement)i].SetIndex(i);
        }

        ShuffleCards(true);

        SayIfAny(mobData.talk?.battleStart);
    }

    // ── 怪物講話 ────────────────────────────────────────────────
    // 台詞在 MobData 的「台詞」欄位。時機分開場／出手前／被打到／剩下不多／被打倒，
    // 每次隨機挑一句。出手前跟被打到不是每次都講——每回合都吵一句會很煩。

    /// <summary>出手前開口的機率。</summary>
    const float ActTalkChance = 0.35f;
    /// <summary>被打到開口的機率。</summary>
    const float HurtTalkChance = 0.3f;
    /// <summary>兩句之間至少隔幾秒，免得連續被打時洗版。</summary>
    const float TalkGap = 4f;

    float nextTalkTime;
    bool saidLowHp;

    void SayIfAny (string[] lines, bool force = true)
    {
        if (lines == null || lines.Length == 0) return;
        if (!force && Time.time < nextTalkTime) return;

        nextTalkTime = Time.time + TalkGap;
        MonsterTalkBubble.Say(transform, lines[Random.Range(0, lines.Length)]);
    }

    protected override void OnDefeated()
    {
        combatSystem.isContinue = false;
        isMovable = false;
        SayIfAny(mobData.talk?.defeated);
        sprRend.DOFade(0f, 1f).OnComplete(() => combatSystem.GameOver(false));
        //combatSystem.GameOver(false);
    }

    public void SetOrgY(float y)
    {
        orgY = y;
    }
    
    void Update()
    {
        if (isMovable)
        {
            theta += Time.deltaTime * speed;
            transform.position = new Vector3(transform.position.x, orgY + offset * Mathf.Sin(theta), transform.position.z);
        }
    }

    /// <summary>
    /// 洗牌
    /// </summary>
    /// <param name="isAll">是不是全洗</param>
    public void ShuffleCards(bool isAll)
    {
        for(var i = 0; i < MaxCardCount; i++)
        {
            var card = cardArr[i];
            if(isAll || card.isUse)
            {
                var rnd = RandomTool.RandomHelper.GetRandomList(cardRndList);
                cardArr[i].element = (ECardElement)rnd.Index;
                // cardArr[i].isUse = false;
                cardArr[i].Reset();
            }
            else
            {
                cardArr[i].LevelUp(mobData);
            }
        }
    }

    // 流程: GetNewAction > GetActString > GetActResult

    public class MobActionResult
    {
        public CardAttribute attr;
        // public int turnRemain;
        // public EEnvEffectType targetEnvEffect;

        public void Reset()
        {
            attr.Init();
            // targetEnvEffect = EEnvEffectType.None;
        }
    }

    /// <summary>
    /// 取得敵人行動文字資訊
    /// TODO 日後要改成圖片搭配文字
    /// </summary>
    /// <returns></returns>
    public string GetActionString()
    {
        // var act = actResult.curAct;

        // if (act.displayType == EMobActionDisplayType.HideAll)
        // {
        //     if (act.type == EMobActionType.Power)
        //     {
        //         var str = "??? ";

        //         switch(act.breakType)
        //         {
        //             case EBreakConditionType.HP:
        //                 {
        //                     str = str + $" 受到{actResult.breakValue}傷害後打斷行動";
        //                 }
        //                 break;
        //             case EBreakConditionType.ATK:
        //                 {
        //                     str = str + $" 需要{act.breakValue}ATK打斷行動";
        //                 }
        //                 break;
        //             /*case EBreakConditionType.DEF:
        //                 break;
        //             case EBreakConditionType.HEAL:
        //                 break;*/
        //         }
        //         return str;
        //         //return $"??? Break ATK:{act.breakCondition_ATK}";
        //     }
        //     else
        //     {
        //         return "???";
        //     }
        // }

        string atkStr = actResult.attr.ATK > 0 && !HasEffect(EAbilityEffectType.Stun) ? $"ATK:{actResult.attr.ATK} " : "";
        string defStr = actResult.attr.DEF > 0 ? $"DEF:{actResult.attr.DEF} " : "";
        string healStr = actResult.attr.HEAL > 0 && !HasEffect(EAbilityEffectType.Stun) ? $"HEAL:{actResult.attr.HEAL} " : "";

        status.Reset();

        if(actResult.attr.ATK > 0 && !HasEffect(EAbilityEffectType.Stun))
        {
            status.SetATK(actResult.attr.ATK);
        }

        if(actResult.attr.DEF > 0)
        {
            status.SetDEF(actResult.attr.DEF);
        }
        if(actResult.attr.HEAL > 0 && !HasEffect(EAbilityEffectType.Stun))
        {
            status.SetHEAL(actResult.attr.HEAL);
        }

        // if(act.displayType == EMobActionDisplayType.HideATK && actResult.attr.ATK > 0)
        // {
        //     var atkSB = new StringBuilder();
        //     atkSB.Append(atkStr.Substring(0, 5));
        //     for (int i = 1; i < actResult.attr.ATK.ToString().Length; i++)
        //     {
        //         atkSB.Append("?");
        //     }
        //     atkStr = atkSB.ToString();
        // }
        // if (act.displayType == EMobActionDisplayType.HideDEF && actResult.attr.DEF > 0)
        // {
        //     var defSB = new StringBuilder();
        //     defSB.Append(defStr.Substring(0, 5));
        //     for (int i = 1; i < actResult.attr.DEF.ToString().Length; i++)
        //     {
        //         defSB.Append("?");
        //     }
        //     defStr = defSB.ToString();
        // }
        // if (act.displayType == EMobActionDisplayType.HideHEAL && actResult.attr.HEAL > 0)
        // {
        //     var healSB = new StringBuilder();
        //     healSB.Append(healStr.Substring(0, 6));
        //     for (int i = 1; i < actResult.attr.HEAL.ToString().Length; i++)
        //     {
        //         healSB.Append("?");
        //     }
        //     healStr = healSB.ToString();
        // }

        string TransElementToString(ECardElement element)
        {
            switch (element)
            {
                case ECardElement.Blue:
                    return "藍";
                case ECardElement.Red:
                    return "紅";
                case ECardElement.Yellow:
                    return "黃";
                case ECardElement.Green:
                    return "綠";

            }
            return string.Empty;
        }

        string conditionStr = "";
        if(HasEffect(EAbilityEffectType.MagicArmor))
        {
            var eff = GetEffect(EAbilityEffectType.MagicArmor);
            conditionStr = $"抗魔裝甲({TransElementToString((ECardElement)eff.value)}):{eff.duration}";
        }
        //if(HasEffect(EAbilityEffectType.MagicArmorEX))
        //{
        //    var eff = GetEffect(EAbilityEffectType.MagicArmorEX);
        //    conditionStr = $"抗魔裝甲EX({TransElementToString((ECardElement)eff.value)}):{eff.duration}";
        //}
        // if (act.type == EMobActionType.Power)
        // {
        //     switch (act.breakType)
        //     {
        //         case EBreakConditionType.HP:
        //             {
        //                 conditionStr = $" 受到{actResult.breakValue}傷害後打斷行動";
        //             }
        //             break;
        //         case EBreakConditionType.ATK:
        //             {
        //                 conditionStr = $" 需要{act.breakValue}ATK打斷行動";
        //             }
        //             break;
        //             /*case EBreakConditionType.DEF:
        //                 break;
        //             case EBreakConditionType.HEAL:
        //                 break;*/
        //     }
        // }
        if(HasEffect(EAbilityEffectType.Stun))
        {
            conditionStr = "昏迷";
        }

        var finalStr = atkStr + defStr + healStr + conditionStr;
        Debug.Log($"Update Status Info: {finalStr}");
        return finalStr;
    }

    public void GetNewAction()
    {
        EN.Value += actResult.attr.EN;
        actResult.Reset();
        if(!HasEffect(EAbilityEffectType.Stun))
        {
            if (Random.value < ActTalkChance) SayIfAny(mobData.talk?.act, force: false);

            DecideCostAbility();
            ShuffleCards(false);
            actResult.attr = SelectCards(MakeDecision());
            // actResult.targetEnvEffect = act.targetEnvEffect;
        }
    }
    
    /// <summary>
    /// 決策是否用技能
    /// 這部分通常就是EN到就用
    /// ! 日後可能會變更使用技能的執行順序
    /// </summary>
    void DecideCostAbility()
    {
        if(mobData.ability != null && mobData.ability.Length > 0)
        {
            var abilityRndList = new List<RandomTool.RandomObject>();
            // var abilityList = new List<Ability>();
            for(var i = 0; i < mobData.ability.Length; i++)
            {
                var abilityData = mobData.ability[i];
                var ability = DataService.Instance.GetAbilityById(mobData.ability[i].abilityId);
                if(DataService.IsEmpty(ability))
                {
                    continue;
                }
                if(EN.Value >= ability.requireEnergy)
                {
                    var rndObj = new RandomTool.RandomObject();
                    rndObj.SetIndex(i);
                    rndObj.Weight = abilityData.decisionWeight;
                    abilityRndList.Add(rndObj);
                }
                // abilityList.Add(ability);
            }

            if(abilityRndList.Count > 0)
            {
                var index = RandomTool.RandomHelper.GetRandomList(abilityRndList).Index;
                if(index >= mobData.ability.Length)
                {
                    Debug.LogWarning($"編號錯誤！index:{index}, mobData.ability.Length: {mobData.ability.Length}");
                }
                var useAbility = DataService.Instance.GetAbilityById(mobData.ability[index].abilityId); //abilityList[RandomTool.RandomHelper.GetRandomList(abilityRndList).Index];
                EN.Value -= useAbility.requireEnergy;
                CastAbility(useAbility);
                Debug.LogWarning($"使用了{useAbility.id}!!");
                combatSystem.SpawnCombatText("MOB_USE_RUNE", ECombatTextType.Buff, false);
            }
        }
    }
    
    /// <summary>
    /// 透過權重選擇這一次的元素
    /// </summary>
    /// <returns>所選擇的元素</returns>
    public ECardElement MakeDecision()
    {
        //reset weight
        for(var i = 0; i < (int)ECardElement.None; i++)
        {
            eleDecisionRndMap[(ECardElement)i].Weight = 0;
        }

        var eleWeightCountArr = new int[] {0, 0, 0, 0};


        foreach(var card in cardArr)
        {
            var index = (int)card.element;
            if(eleWeightCountArr[index] < mobData.maxSelectCardCount)
            {
                eleDecisionRndMap[card.element].Weight += mobData.elementData[index].decisionWeight;
                eleWeightCountArr[index] += 1;
            }
        }

        var envType = combatSystem.envEffect.curType;
        switch(envType)
        {
            case EEnvEffectType.BlueSilence:
                eleDecisionRndMap[ECardElement.Blue].Weight = 0;
                break;
            case EEnvEffectType.RedSilence:
                eleDecisionRndMap[ECardElement.Red].Weight = 0;
                break;
            case EEnvEffectType.YellowSilence:
                eleDecisionRndMap[ECardElement.Yellow].Weight = 0;
                break;
            case EEnvEffectType.GreenSilence:
                eleDecisionRndMap[ECardElement.Green].Weight = 0;
                break;
            case EEnvEffectType.Energy:
                eleDecisionRndMap[ECardElement.Yellow].Weight *= 2;
                break;
        }

        return (ECardElement)RandomTool.RandomHelper.GetRandomList(eleDecisionRndMap.Values.ToList()).Index;
    }

    /// <summary>
    /// 選擇卡片 所選到的卡片改為已經使用的狀態
    /// </summary>
    /// <param name="selectElement"></param>
    /// <returns>返回素質的總和</returns>
    public CardAttribute SelectCards(ECardElement selectElement)
    {
        Debug.Log($"MobUnitCards this turn use element: {selectElement}");

        var attr = GetBonusAttr();// bonusAttr;
        
        var targetAttr = mobData.elementData[(int)selectElement].attribute;

        var isLimitedCards = combatSystem.envEffect.curType == EEnvEffectType.LimitCards;

        var isIgnoreElement = HasEffect(EAbilityEffectType.IgnoreElement);

        var selectCardCount = 0;
        for(var i = 0; i < MaxCardCount; i++)
        {
            var card = cardArr[i];
            if(isIgnoreElement || card.element == selectElement)
            {
                card.isUse = true;
                selectCardCount++;

                var totalAttr = card.GetAttr(targetAttr);

                attr.ATK += totalAttr.ATK;
                attr.DEF += totalAttr.DEF;
                attr.HEAL += totalAttr.HEAL;
                attr.EN += totalAttr.EN;

                if((selectCardCount == mobData.maxSelectCardCount) || (!isIgnoreElement && isLimitedCards && selectCardCount == 2))
                {
                    break;
                }
            }
        }

        Debug.Log($"MobUnitCards selectCardCount: {selectCardCount}");
        for(var i = 0; i < MaxCardCount; i++)
        {
            var card = cardArr[i];
            Debug.Log($"MobUnitCards: ele:{card.element}, isUse? {card.isUse}");
        }


        if (!HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect))
        {
            var envEff = combatSystem.envEffect;
            switch (envEff.curType)
            {
                case EEnvEffectType.Attack:
                    {
                        attr.ATK = attr.ATK * 2;
                    }
                    break;
                case EEnvEffectType.Defense:
                    {
                        attr.DEF = attr.DEF * 2;
                    }
                    break;
                case EEnvEffectType.Heal:
                    {
                        attr.HEAL = attr.HEAL * 2;
                    }
                    break;
                case EEnvEffectType.Energy:
                    {
                        attr.EN = attr.EN * 2;
                    }
                    break;
                case EEnvEffectType.NoHeal:
                    {
                        attr.HEAL = 0;
                    }
                    break;
                case EEnvEffectType.NoDefense:
                    {
                        attr.DEF = 0;
                    }
                    break;
            }
        }
        if(HasEffect(EAbilityEffectType.NoArmor))
        {
            attr.DEF = 0;
        }
        if(HasEffect(EAbilityEffectType.IncreaseATK2))
        {
            attr.ATK = Mathf.FloorToInt((float)attr.ATK * 1.2f);
        }
        //if(HasEffect(EAbilityEffectType.MagicArmorEX))
        //{
        //    attr.ATK = Mathf.FloorToInt((float)attr.ATK * 1.2f);
        //    attr.ATK = Mathf.FloorToInt((float)attr.HEAL * 1.2f);
        //}

        if (TowerModeManager.IsActive)
            attr.ATK = Mathf.FloorToInt(attr.ATK * TowerModeManager.GetMonsterAtkScale());

        return attr;
    }

    public MobActionResult GetActionResult() => actResult;

    //敵人被攻擊
    public override void ApplyDamage(float damageValue)
    {
        float dmg = GetAppliedDamage(damageValue);
        if (dmg > 0f)
        {
            isMovable = false;

            transform.DOShakePosition(0.7f, new Vector3(2f, 0f, 0f)).onComplete += ()=> isMovable = true;
            
            DamagedFlash().Forget();

            // 血量第一次掉到三成以下：講「剩下不多」那組，優先於一般的挨打台詞
            var lowNow = HP.GetTotalValue() > 0 && HP.Value / HP.GetTotalValue() <= 0.3f;
            if (lowNow && !saidLowHp && mobData.talk?.lowHp != null && mobData.talk.lowHp.Length > 0)
            {
                saidLowHp = true;
                SayIfAny(mobData.talk.lowHp);
            }
            else if (Random.value < HurtTalkChance) SayIfAny(mobData.talk?.hurt, force: false);

            if (audioClipList.Count > 0)
            {
                int rnd = Random.Range(0, audioClipList.Count);
                audioSource.clip = audioClipList[rnd];
                audioSource.Play();
            }
            //animator.SetTrigger("beHit");

            //enemyAnimation.Play(enemyAnis[2].name);
            //StartCoroutine(PlayIdleAnimation());
        }
    }

    async UniTaskVoid DamagedFlash()
    {
        sprRend.material.SetFloat("_FlashAmount", 0.8f);
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.15f));
        sprRend.material.SetFloat("_FlashAmount", 0f);
    }
    
    public void CheckCostMagicArmor(ECardElement playerElement, int count)
    {
        if(!HasEffect(EAbilityEffectType.MagicArmor)/* && !HasEffect(EAbilityEffectType.MagicArmorEX)*/)
        {
            return;
        }

        AbilityEffectRef effect = GetEffect(EAbilityEffectType.MagicArmor);// HasEffect(EAbilityEffectType.MagicArmor)? GetEffect(EAbilityEffectType.MagicArmor) : GetEffect(EAbilityEffectType.MagicArmorEX);

        var armorElement = (ECardElement)effect.value;

        Debug.Log($"armorElement? {armorElement}, playerElement? {playerElement}, count? {count}");
        if(playerElement == ECardElement.None || armorElement == playerElement)
        {
            CostEffect(effect, count);
        }
    }

    protected override void CardLevelUp()
    {
        for(var i = 0; i < MaxCardCount; i++)
        {
            cardArr[i].LevelUp(mobData);
        }
    }

    protected override void AddEN(float value)
    {
        EN.Value += value;
    }

    protected override void ReflashCards()
    {
        ShuffleCards(true);
    }

    protected override void OnInterrupt()
    {
        
        combatSystem.MobGetNewAction();
    }
}
