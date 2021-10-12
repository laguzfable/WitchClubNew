using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kenaz;

public abstract class BaseCombatUnit : MonoBehaviour
{
    public UnitAttribute HP;

    public BaseCombatUnit target { protected set; get; }

    public bool controllable = false;

    protected CombatSystem combatSystem;

    public CardAttribute bonusAttr;

    List<AbilityEffectRef> effectList = new List<AbilityEffectRef>();
    Dictionary<EAbilityEffectType, System.Action> effectEvent = new Dictionary<EAbilityEffectType, System.Action>();

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

    public void CostEffect(AbilityEffectRef effect, int cost = 1)
    {
        effect.duration -= cost;
        if(effect.duration <= 0)
        {
            RemoveEffect(effect);
        }
    }

    public void RemoveEffect(AbilityEffectRef effect)
    {
        effectList.Remove(effect);
    }

    public bool HasEffect(EAbilityEffectType type)
    {
        return effectList.Any(item => item.type == type);
    }

    public AbilityEffectRef GetEffect(EAbilityEffectType type)
    {
        return effectList.Find(item => item.type == type);
    }

    /// <summary>
    /// 如果有DOT HOT就在這裡作用
    /// </summary>
    public virtual void BeforeAction()
    {
        if(HasEffect(EAbilityEffectType.HOT))
        {
            var eff = GetEffect(EAbilityEffectType.HOT);
            ApplyHealing(eff.value);
            CostEffect(eff);
        }
        if(HasEffect(EAbilityEffectType.DOT))
        {
            var eff = GetEffect(EAbilityEffectType.DOT);
            ApplyDamage(eff.value);
            CostEffect(eff);
        }
    }

    public void ClearEffect()
    {
        bonusAttr.Init();
        effectList.Clear();
    }

    public virtual void ApplyDamage(float damageValue)
    {
        throw new System.NotImplementedException();
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
        combatSystem.SpawnCombatText(finalValue.ToString("F0"), ECombatTextType.Damage, gameObject.CompareTag("Player"));
        return finalValue;
    }

    public virtual void ApplyHealing(float healingValue)
    {
        float finalValue = healingValue;
        HP.Value += finalValue;
        combatSystem.SpawnCombatText(finalValue.ToString("F0"), ECombatTextType.Heal, gameObject.CompareTag("Player"));
    }

    protected virtual void CardLevelUp()
    {
        throw new System.NotImplementedException();
    }

    protected virtual void AddEN(float value)
    {
        throw new System.NotImplementedException();
    }

    protected virtual void ReflashCards()
    {
        throw new System.NotImplementedException();
    }

    protected virtual void OnInterrupt()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 製造效果 給予自己或敵方 造成直接的影響或加入狀態佇列
    /// </summary>
    /// <param name="newEffects"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public virtual void MakeEffect(AbilityEffect[] newEffects, ECardElement element, bool isItem)
    {
        // var pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
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
                            // pc.CardLevelUp();
                            CardLevelUp();
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
                        OnInterrupt();
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
                        //pc.AddEN(effect.value);
                        AddEN(effect.value);
                    }
                    break;
                case EAbilityEffectType.Shuffle:
                    {
                        if ((ECardElement)effect.GetValue() != ECardElement.None)
                        {
                            AddEffect(AbilityEffectRef.Create(effect.type, effect));
                        }
                        // pc.ReflashCards(false);
                        ReflashCards();
                    }
                    break;
                case EAbilityEffectType.HOT:
                case EAbilityEffectType.DOT:
                    {
                        AbilityEffectRef newEffect = null;
                        if(HasEffect(effect.type))
                        {
                            newEffect = GetEffect(effect.type);
                        }
                        else
                        {
                            newEffect = AbilityEffectRef.Create(effect.type, effect);
                            newEffect.value = effect.value;
                            AddEffect(newEffect);
                        }
                        
                        newEffect.duration = int.Parse(effect.param);
                        
                        /*
                        if (hot.Count > 0)
                        {
                            hot.Clear();
                        }
                        for (int i = 0; i < int.Parse(effect.param); i++)
                        {
                            hot.Add((int)effect.value);
                        }
                        */
                    }
                    break;
                    
                case EAbilityEffectType.MagicArmor:
                case EAbilityEffectType.MagicArmorEX:
                    {
                        AbilityEffectRef newEffect = null;
                        if(HasEffect(effect.type))
                        {
                            newEffect = GetEffect(effect.type);
                        }
                        else
                        {
                            newEffect = AbilityEffectRef.Create(effect.type, effect);
                            AddEffect(newEffect);
                        }
                        
                        newEffect.value = Random.Range(0, 4);
                        newEffect.duration = int.Parse(effect.param);
                    }
                    break;
                case EAbilityEffectType.ChangeEnvironmentEffect:
                    {
                        combatSystem.envEffect.SetCurrentEffect((int)effect.value < 0 ? (EEnvEffectType)Random.Range(0, (int)EEnvEffectType.Length) : (EEnvEffectType)effect.GetValue());
                    }
                    break;
                case EAbilityEffectType.Stun:// !not implemented yet
                case EAbilityEffectType.NoArmor:
                    {
                        target.AddEffect(AbilityEffectRef.Create(effect.type, effect));
                    }
                    break;
                case EAbilityEffectType.TheWorld:// !not implemented yet
                case EAbilityEffectType.IgnoreElement:
                case EAbilityEffectType.HealingAttack:
                case EAbilityEffectType.Reflect:
                case EAbilityEffectType.LifeSteal:
                case EAbilityEffectType.SelfODExchange:// !not implemented yet
                case EAbilityEffectType.Revenge:// !not implemented yet
                case EAbilityEffectType.FocusHeal:// !not implemented yet
                case EAbilityEffectType.Shield:
                case EAbilityEffectType.IgnoreEnvironmentEffect:
                    {
                        AddEffect(AbilityEffectRef.Create(effect.type, effect));
                    }
                    break;
            }
        }
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

    public float value;
    public int duration;

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
