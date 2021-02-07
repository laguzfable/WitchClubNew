using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kenaz;

public class BaseCombatUnit : MonoBehaviour
{
    public UnitAttribute HP;

    public BaseCombatUnit target { protected set; get; }

    public bool controllable = false;

    protected CombatSystem combatSystem;

    public CardAttribute bonusAttr;

    List<AbilityEffectRef> effectList = new List<AbilityEffectRef>();
    Dictionary<EAbilityEffectType, System.Action> effectEvent = new Dictionary<EAbilityEffectType, System.Action>();

    protected List<int> hot = new List<int>();

    protected virtual void OnDefeated()
    {

    }

    protected virtual void Init()
    {
        HP.Restore();
    }

    void Start()
    {
        HP.OnValueEmpty += OnDefeated;
        combatSystem = GameObject.FindWithTag("GameController").GetComponent<CombatSystem>();
        Init();
    }

    public void LoadDataFromJsonString(string jsonStr)
    {
        AttributeColletion attrCollection = JsonUtility.FromJson<AttributeColletion>(jsonStr);
        HP = attrCollection.HP;
    }

    public void AddEffectEvent(EAbilityEffectType type, System.Action action)
    {
        if (!effectEvent.ContainsKey(type))
        {
            effectEvent[type] = action;
        }
    }

    public void AddEffect(AbilityEffectRef effect)
    {
        effectList.Add(effect);
        if(effectEvent.ContainsKey(effect.type))
        {
            effectEvent[effect.type].Invoke();
        }
    }

    public bool HasEffect(EAbilityEffectType type)
    {
        return effectList.Any(item => item.type == type);
    }

    public AbilityEffectRef GetEffect(EAbilityEffectType type)
    {
        return effectList.Find(item => item.type == type);
    }

    public virtual void BeforeAction()
    {
        if(hot.Count > 0)
        {
            HP.Value += hot[0];
            hot.RemoveAt(0);
        }
    }

    public void ClearEffect()
    {
        bonusAttr.Init();
        effectList.Clear();
    }

    public virtual void ApplyDamage(float damageValue)
    {

    }
    
    protected float GetAppliedDamage(float damageValue)
    {
        //Debug.Log("damage:" + this.name);
        float finalValue = damageValue;//(damageValue - DEF.GetTotalValue());
        if(finalValue < 0f)
        {
            finalValue = 0f;
        }
        HP.Value -= finalValue;
        combatSystem.combatTxtPanel.EnqueueText(finalValue.ToString("F0"), ECombatTextType.Damage, gameObject.CompareTag("Player"));
        return finalValue;
    }

    public virtual void ApplyHealing(float healingValue)
    {
        float finalValue = healingValue;
        HP.Value += finalValue;
        combatSystem.combatTxtPanel.EnqueueText(finalValue.ToString("F0"), ECombatTextType.Heal, gameObject.CompareTag("Player"));
    }

    /// <summary>
    /// 製造效果 給予自己或敵方 造成直接的影響或加入狀態佇列
    /// </summary>
    /// <param name="newEffects"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public virtual void MakeEffect(AbilityEffect[] newEffects, ECardElement element, bool isItem)
    {
        var pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        foreach (var effect in newEffects)
        {
            switch (effect.type)
            {
                case EAbilityEffectType.IncreaseATK:
                    {
                        bonusAttr.ATK += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.IncreaseDEF:
                    {
                        bonusAttr.DEF += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.IncreaseHeal:
                    {
                        bonusAttr.HEAL += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.LevelUp:
                    {
                        for (int i = 0; i < effect.value; i++)
                        {
                            pc.CardLevelUp();
                        }
                    }
                    break;
                case EAbilityEffectType.DirectDamage:
                    {
                        target.ApplyDamage(effect.value);
                    }
                    break;
                case EAbilityEffectType.Interrupt:
                    {
                        combatSystem.MobGetNewAction();
                    }
                    break;
                case EAbilityEffectType.InstantHeal:
                    {
                        ApplyHealing(effect.value);
                    }
                    break;
                case EAbilityEffectType.IncreaseMaximumHP:
                    {
                        //HP.SetBaseValue(HP.GetTotalValue() + effect.value);
                        HP.buff = effect.value;
                        ApplyHealing(HP.GetTotalValue());
                    }
                    break;
                case EAbilityEffectType.InstantEnergy:
                    {
                        pc.AddEN(effect.value);
                    }
                    break;
                case EAbilityEffectType.Shuffle:
                    {
                        if ((ECardElement)effect.GetValue() != ECardElement.None)
                        {
                            AddEffect(AbilityEffectRef.Create(effect.type, effect));
                        }
                        pc.ReflashCards(false);
                    }
                    break;
                case EAbilityEffectType.HOT:
                    {
                        if (hot.Count > 0)
                        {
                            hot.Clear();
                        }
                        for (int i = 0; i < int.Parse(effect.param); i++)
                        {
                            hot.Add((int)effect.value);
                        }
                    }
                    break;
                case EAbilityEffectType.ChangeEnvironmentEffect:
                    {
                        combatSystem.envEffect.SetCurrentEffect((int)effect.value < 0 ? (EEnvEffectType)Random.Range(0, (int)EEnvEffectType.Length) : (EEnvEffectType)effect.GetValue());
                    }
                    break;
                case EAbilityEffectType.Stun:// not implemented yet
                case EAbilityEffectType.NoArmor:
                    {
                        target.AddEffect(AbilityEffectRef.Create(effect.type, effect));
                    }
                    break;
                case EAbilityEffectType.TheWorld:// not implemented yet
                case EAbilityEffectType.IgnoreElement:
                case EAbilityEffectType.HealingAttack:
                case EAbilityEffectType.Reflect:
                case EAbilityEffectType.LifeSteal:
                case EAbilityEffectType.SelfODExchange:// not implemented yet
                case EAbilityEffectType.Revenge:// not implemented yet
                case EAbilityEffectType.FocusHeal:// not implemented yet
                case EAbilityEffectType.Shield:
                case EAbilityEffectType.IgnoreEnvironmentEffect:
                    {
                        AddEffect(AbilityEffectRef.Create(effect.type, effect));
                    }
                    break;
            }
        }


        #region old
        /*
        foreach (var effect in newEffects)
        {
            BaseCombatUnit[] fxTargetArr = null;
            switch (effect.target)
            {
                case EAbilityEffectTarget.Self:
                    fxTargetArr = new BaseCombatUnit[1];
                    fxTargetArr[0] = this;
                    break;
                case EAbilityEffectTarget.Opponent:
                    fxTargetArr = new BaseCombatUnit[1];
                    fxTargetArr[0] = target;
                    break;
                case EAbilityEffectTarget.Both:
                    fxTargetArr = new BaseCombatUnit[2];
                    fxTargetArr[0] = this;
                    fxTargetArr[1] = target;
                    break;
            }
            if (fxTargetArr == null)
            {
                Debug.LogError("fxTargetArr is null !!!!!!");
            }
            foreach (var fxTarget in fxTargetArr)
            {
                switch (effect.type)
                {
                    //direct
                    case EAbilityEffectType.Damage:
                        {
                            fxTarget.ApplyDamage((ATK.GetTotalValue() + effect.value + (might.Value * MightPower)), element, EDamageSource.Direct);
                        }
                        break;
                    case EAbilityEffectType.Heal:
                        {
                            fxTarget.ApplyHealing(effect.value, true, element);
                        }
                        break;
                    case EAbilityEffectType.Shield:
                        {
                            fxTarget.shield.Value += effect.value;
                            cbtSys.cbtTxtPanel.EnqueueText("+" + effect.value.ToString("F0") + "護盾", ECombatTextType.Buff, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Clean:
                        {
                            fxTarget.CleanState();
                        }
                        break;
                    case EAbilityEffectType.ExchangeHP:
                        {
                            float tmpHPPercent = fxTarget.HP.GetPercent();
                            fxTarget.HP.Value = fxTarget.HP.GetTotalValue() * fxTarget.target.HP.GetPercent();
                            fxTarget.target.HP.Value = fxTarget.target.HP.GetTotalValue() * tmpHPPercent;

                            cbtSys.cbtTxtPanel.EnqueueText("交換HP", ECombatTextType.Other, fxTarget.gameObject.CompareTag("Player"));
                            cbtSys.cbtTxtPanel.EnqueueText("交換HP", ECombatTextType.Other, fxTarget.target.gameObject.CompareTag("Player"));
                        }
                        break;
                    //effect
                    case EAbilityEffectType.CostHP:
                        {
                            fxTarget.HP.Value -= effect.value;
                            cbtSys.cbtTxtPanel.EnqueueText(effect.value.ToString("F0"), ECombatTextType.Damage, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Lifesteal:
                        {
                            float value = fxTarget.HP.GetTotalValue() + effect.value;
                            float stolenValue = fxTarget.ApplyDamage(value, element, EDamageSource.Direct);
                            fxTarget.target.ApplyHealing(stolenValue, true, element);

                            cbtSys.cbtTxtPanel.EnqueueText(stolenValue.ToString("F0"), ECombatTextType.Heal, fxTarget.target.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Might:
                        {
                            fxTarget.might.Value += effect.value;
                            cbtSys.cbtTxtPanel.EnqueueText("+" + effect.value.ToString("F0") + "力量", ECombatTextType.Buff, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Reflection:
                        {
                            fxTarget.reflection.Value += effect.value;
                            cbtSys.cbtTxtPanel.EnqueueText("+" + effect.value.ToString("F0") + "反射", ECombatTextType.Buff, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Poison:
                        {
                            fxTarget.poison.Value += effect.value;
                            cbtSys.cbtTxtPanel.EnqueueText("+" + effect.value.ToString("F0") + "中毒", ECombatTextType.Debuff, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                    case EAbilityEffectType.Stun:
                        {
                            var stunDamage = effect.value * (cbtSys.GetEnvEffect().curType == EEnvEffectType.TripleStun ? 3f : 1f);
                            fxTarget.stun.Value -= stunDamage;
                            cbtSys.cbtTxtPanel.EnqueueText("昏迷傷害 " + stunDamage, ECombatTextType.Debuff, fxTarget.gameObject.CompareTag("Player"));
                        }
                        break;
                }
            }
        }
        */
        #endregion
    }

    public void CastAbility(Ability ability)
    {
        MakeEffect(ability.effect, ability.element, false);
    }
}

public class AbilityEffectRef
{
    public EAbilityEffectType type;
    public AbilityEffect effect;

    static public AbilityEffectRef Create(EAbilityEffectType newType, AbilityEffect newEffect)
    {
        return new AbilityEffectRef() { type = newType, effect = newEffect };
    }
}

[System.Serializable]
public class AttributeColletion
{
    public UnitAttribute HP;
}
