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

    protected override void CardLevelUp()
    {
        pc.CardLevelUp();
    }

    protected override void AddEN(float value)
    {
        pc.AddEN(value);
    }

    protected override void ReflashCards()
    {
        pc.ReflashCards(false);
    }

    protected override void OnInterrupt()
    {
        pc.ReflashCards(false);
    }
}
