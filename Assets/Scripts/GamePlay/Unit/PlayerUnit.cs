using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Kenaz;
using UniRx.Async;

public class PlayerUnit : BaseCombatUnit 
{
    PlayerController pc;


    protected override void Init()
    {
        base.Init();
        /*
        var playerData = Toolbox.Instance.GetOrAddComponent<PlayerData>();
        //base.Init();
        //equipSlotArr = playerData.equipSlotArr;

        if (playerData.curHP < 0f)
        {
            playerData.curHP = HP.Value;
        }
        else
        {
            HP.Value = playerData.curHP;
        }
        */
        //Debug.Log("HP.Value : " + HP.Value);

        if(target == null)
        {
            target = GameObject.FindGameObjectWithTag("Enemy").GetComponent<BaseCombatUnit>();
        }

        pc = GetComponent<PlayerController>();
    }
    /*
    private void OnDisable()
    {
        var playerData = Toolbox.Instance.GetOrAddComponent<PlayerData>();
        playerData.curHP = HP.Value;
    }
    */
    /*
    public override void Action()
    {
        base.Action();
        Debug.Log("Player Action!");
    }
    */
    public override void ApplyDamage(float damageValue)
    {
        if(HasEffect(EAbilityEffectType.Shield))
        {
            return; 
        }
        float dmg = GetAppliedDamage(damageValue);
        if(dmg > 0f)
        {
            CameraPlay.Hit(Color.red, 0.5f);
            CameraPlay.EarthQuakeShake(0.5f);
        }
        /*if(HP.Value <= 0f)
        {
            combatSys.GameOver(false);
        } */
    }

    async UniTaskVoid TurnScreenBlackWhite()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
        CameraPlay.BlackWhite_ON();
    }

    protected override void OnDefeated()
    {
        combatSystem.isContinue = false;
        combatSystem.GameOver(true);
    }
    /*
    public override void MakeEffect(AbilityEffect[] newEffects, ECardElement element, bool isItem)
    {
        foreach (var effect in newEffects)
        {
            switch (effect.type)
            {
                case EAbilityEffectType.ATK:
                    {
                        bonusAttr.ATK += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.DEF:
                    {
                        bonusAttr.DEF += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.Heal:
                    {
                        bonusAttr.HEAL += (int)effect.value;
                    }
                    break;
                case EAbilityEffectType.LevelUp:
                    {
                        for(int i = 0; i < effect.value; i++)
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
                case EAbilityEffectType.Interupt:
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
                        if((ECardElement)effect.GetValue() != ECardElement.None)
                        {
                            effectList.Add(AbilityEffectRef.Create(effect.type, effect));
                        }
                        pc.ReflashCards(false);
                    }
                    break;
                case EAbilityEffectType.HOT:
                    {
                        if(hot.Count > 0)
                        {
                            hot.Clear();
                        }
                        for(int i = 0; i < int.Parse(effect.param); i++)
                        {
                            hot.Add((int)effect.value);
                        }
                    }
                    break;
                case EAbilityEffectType.ChangeEnvironmentEffect:
                    {
                        combatSystem.envEffect.SetCurrentEffect((int)effect.value < 0? (EEnvEffectType)Random.Range(0, (int)EEnvEffectType.Length) : (EEnvEffectType)effect.GetValue());
                    }
                    break;
                case EAbilityEffectType.Stun:// not implemented yet
                case EAbilityEffectType.NoArmor:
                    {
                        target.effectList.Add(AbilityEffectRef.Create(effect.type, effect));
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
                        effectList.Add(AbilityEffectRef.Create(effect.type, effect));
                    }
                    break;
            }
        }
    }
    */
}
