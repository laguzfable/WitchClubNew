using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Text;

public class EnemyUnit : BaseCombatUnit
{
    float theta = 0f;

    [SerializeField] float speed = 1f;

    [SerializeField] float offset = 0.5f;

    float orgY;

    bool isMovable = true;

    Animator animator;

    public Transform fxPos;
    

    [SerializeField]
    FXSequence fxSeq;

    [SerializeField]
    List<AudioClip> audioClipList;
    AudioSource audioSource;

    [SerializeField]
    MobAction[] defActionArr;


    MobAction[] actionArr;
    int curActIndex = 0;

    SpriteRenderer sprRend;

    private void Awake()
    {
        audioSource = this.GetOrAddComponent<AudioSource>();
        orgY = transform.position.y;
        animator = GetComponent<Animator>();
        sprRend = GetComponent<SpriteRenderer>();

        actionArr = defActionArr;
        ShuffleAction();
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerUnit>();
        }

        AddEffectEvent(EAbilityEffectType.Stun, () => combatSystem.UpdateMobActionInfo(GetActionString()));
        AddEffectEvent(EAbilityEffectType.NoArmor, () =>
        {
            actResult.attr.DEF = 0;
            combatSystem.UpdateMobActionInfo(GetActionString());
        });
        AddEffectEvent(EAbilityEffectType.BreakAction, () => combatSystem.combatTxtPanel.EnqueueText("成功打斷行動", ECombatTextType.Debuff, false));
    }

    protected override void Init()
    {
        base.Init();

        Debug.Log($"Toolbox.Instance.GetOrAddComponent<DataService>().paramArr[1] : {Toolbox.Instance.GetOrAddComponent<DataService>().paramArr[1]}");
        sprRend.sprite = combatSystem.visualResource.GetMobByName(Toolbox.Instance.GetOrAddComponent<DataService>().paramArr[1]);
        sprRend.enabled = true;

        HP.Restore();
    }

    protected override void OnDefeated()
    {
        isMovable = false;
        sprRend.DOFade(0f, 1f).OnComplete(() => combatSystem.GameOver(false));
        //combatSystem.GameOver(false);
    }
    
    void Update()
    {
        if (isMovable)
        {
            theta += Time.deltaTime * speed;
            transform.position = new Vector3(transform.position.x, orgY + offset * Mathf.Sin(theta), transform.position.z);
        }
    }

    void ShuffleAction()
    {
        int i = actionArr.Length - 1;
        while (i != 0)
        {
            int rnd = Random.Range(0, actionArr.Length);
            var tmp = actionArr[i];
            actionArr[i] = actionArr[rnd];
            actionArr[rnd] = tmp;
            i--;
        }
    }

    public void CheckIsBreak(float damageValue)
    {
        if(actResult.curAct.type == EMobActionType.Power && !HasEffect(EAbilityEffectType.BreakAction) && damageValue >= actResult.breakValue)
        {
            AddEffect(AbilityEffectRef.Create(EAbilityEffectType.BreakAction, new AbilityEffect() { type = EAbilityEffectType.BreakAction }));
        }
    }

    public class MobActionResult
    {
        public CardAttribute attr;
        public int turnRemain;
        public EBreakConditionType breakType;
        public int breakValue;
        public MobAction curAct;
        public EEnvEffectType targetEnvEffect;
    }

    MobActionResult actResult = null;

    public MobAction GetNewAction(bool isBreak)
    {
        MobAction act = null;
        
        if(HasEffect(EAbilityEffectType.Stun) || HasEffect(EAbilityEffectType.BreakAction))
        {
            isBreak = true;
        }
        ClearEffect();
        if (actResult != null && actResult.turnRemain > 0 && !isBreak)
        {
            actResult.turnRemain--;
            return actResult.curAct;
        }
        if (actResult != null && !isBreak && actResult.curAct.turn > 0 && actResult.turnRemain <= 0)
        {
            act = actResult.curAct.nextAction;
        }
        else if(curActIndex > actionArr.Length -1)
        {
            ShuffleAction();
            curActIndex = 0;
        }

        if(act == null)
        {
            act = actionArr[curActIndex];
        }

        actResult = new MobActionResult();
        actResult.attr = bonusAttr;
        actResult.attr.ATK += Random.Range(act.minATK, act.maxATK);
        actResult.attr.DEF += Random.Range(act.minDEF, act.maxDEF);
        actResult.attr.HEAL += Random.Range(act.minHEAL, act.minHEAL);
        actResult.turnRemain = act.turn-1;
        actResult.breakType = act.breakType;
        actResult.breakValue = act.breakValue;
        actResult.curAct = act;
        actResult.targetEnvEffect = act.targetEnvEffect;
        curActIndex++;

        var envEff = combatSystem.envEffect;
        switch (envEff.curType)
        {
            case EEnvEffectType.Attack:
                {
                    actResult.attr.ATK = actResult.attr.ATK * 2;
                }
                break;
            case EEnvEffectType.Defense:
                {
                    actResult.attr.DEF = actResult.attr.DEF * 2;
                }
                break;
            case EEnvEffectType.Heal:
                {
                    actResult.attr.HEAL = actResult.attr.HEAL * 2;
                }
                break;
            case EEnvEffectType.NoHeal:
                {
                    actResult.attr.HEAL = 0;
                }
                break;
            case EEnvEffectType.NoDefense:
                {
                    actResult.attr.DEF = 0;
                }
                break;
        }
        /*if (HasEffect(EAbilityEffectType.NoArmor))
        {
            actResult.attr.DEF = 0;
        }*/
        return act;
    }

    public string GetActionString()
    {
        var act = actResult.curAct;

        if (act.displayType == EMobActionDisplayType.HideAll)
        {
            if (act.type == EMobActionType.Power)
            {
                var str = "??? ";

                switch(act.breakType)
                {
                    case EBreakConditionType.HP:
                        {
                            str = str + $" 受到{actResult.breakValue}傷害後打斷行動";
                        }
                        break;
                    case EBreakConditionType.ATK:
                        {
                            str = str + $" 需要{act.breakValue}ATK打斷行動";
                        }
                        break;
                    /*case EBreakConditionType.DEF:
                        break;
                    case EBreakConditionType.HEAL:
                        break;*/
                }
                return str;
                //return $"??? Break ATK:{act.breakCondition_ATK}";
            }
            else
            {
                return "???";
            }
        }

        string atkStr = actResult.attr.ATK > 0 && !HasEffect(EAbilityEffectType.Stun) ? $"ATK:{actResult.attr.ATK} " : "";
        string defStr = actResult.attr.DEF > 0 ? $"DEF:{actResult.attr.DEF} " : "";
        string healStr = actResult.attr.HEAL > 0 && !HasEffect(EAbilityEffectType.Stun) ? $"HEAL:{actResult.attr.HEAL} " : "";

        if(act.displayType == EMobActionDisplayType.HideATK && actResult.attr.ATK > 0)
        {
            var atkSB = new StringBuilder();
            atkSB.Append(atkStr.Substring(0, 5));
            for (int i = 1; i < actResult.attr.ATK.ToString().Length; i++)
            {
                atkSB.Append("?");
            }
            atkStr = atkSB.ToString();
        }
        if (act.displayType == EMobActionDisplayType.HideDEF && actResult.attr.DEF > 0)
        {
            var defSB = new StringBuilder();
            defSB.Append(defStr.Substring(0, 5));
            for (int i = 1; i < actResult.attr.DEF.ToString().Length; i++)
            {
                defSB.Append("?");
            }
            defStr = defSB.ToString();
        }
        if (act.displayType == EMobActionDisplayType.HideHEAL && actResult.attr.HEAL > 0)
        {
            var healSB = new StringBuilder();
            healSB.Append(healStr.Substring(0, 6));
            for (int i = 1; i < actResult.attr.HEAL.ToString().Length; i++)
            {
                healSB.Append("?");
            }
            healStr = healSB.ToString();
        }

        string conditionStr = "";
        if (act.type == EMobActionType.Power)
        {
            switch (act.breakType)
            {
                case EBreakConditionType.HP:
                    {
                        conditionStr = $" 受到{actResult.breakValue}傷害後打斷行動";
                    }
                    break;
                case EBreakConditionType.ATK:
                    {
                        conditionStr = $" 需要{act.breakValue}ATK打斷行動";
                    }
                    break;
                    /*case EBreakConditionType.DEF:
                        break;
                    case EBreakConditionType.HEAL:
                        break;*/
            }
        }
        if(HasEffect(EAbilityEffectType.Stun))
        {
            conditionStr = "昏迷";
        }

        return atkStr + defStr + healStr + conditionStr;
    }

    public MobActionResult GetActionResult()
    {
        return actResult;
    }

    //敵人被攻擊
    public override void ApplyDamage(float damageValue)
    {
        CheckIsBreak(damageValue);
        float dmg = GetAppliedDamage(damageValue);
        if (dmg > 0f)
        {
            isMovable = false;

            transform.DOShakePosition(0.7f, new Vector3(2f, 0f, 0f)).onComplete += OnShakeComplete;
            
            StartCoroutine(DamagedFlash());

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

    IEnumerator DamagedFlash()
    {
        sprRend.material.SetFloat("_FlashAmount", 0.8f);
        yield return new WaitForSeconds(0.15f);
        sprRend.material.SetFloat("_FlashAmount", 0f);
    }

    void OnShakeComplete()
    {
        isMovable = true;
    }
}
