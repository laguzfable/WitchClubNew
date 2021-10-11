using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Kenaz;

[CreateAssetMenu(fileName = "AbilityCollection.asset", menuName = "Witch Club/Ability/AbilityCollection")]
public class AbilityCollection : ScriptableObject
{
    [ListDrawerSettings(NumberOfItemsPerPage = 5)]
    public List<Ability> abilityList;
}

[System.Serializable]
public enum EAbilityEffectType
{
    IncreaseATK,// 攻擊力增加 R
    IncreaseDEF,// 防禦力增加 B
    IncreaseHeal,// 治療量增加 G
    LevelUp,// 卡牌等級提升 Y
    DirectDamage,// 直接傷害目標 R
    Reflect,// 反射受到的傷害 B
    InstantEnergy,// 立刻獲得能量(除了黃色) Y
    LifeSteal,// 恢復成功造成傷害的治療量 R
    SelfODExchange,// 攻擊力與防禦力交換 R    
    Revenge,// 過溢的防禦力轉換為傷害攻擊目標 B
    Interrupt,// 更換目標行動(old:直接使目標的強力技能失效) Y
    Shield,// 使此次的傷害無效 B
    InstantHeal,// 直接進行治療 G
    FocusHeal,// 所有的數值轉換為治療 G
    IncreaseMaximumHP,// 提升HP上限 G
    IgnoreEnvironmentEffect,// 忽略環境效果
    Shuffle,// 洗掉手牌
    NoArmor, // 防禦力為0
    IgnoreElement, //無視卡牌顏色
    HOT, //持續治療
    ChangeEnvironmentEffect, //更換環境效果
    HealingAttack, //治療同時給予傷害
    Stun, //下回合無法行動
    TheWorld,//時間暫停 時間內無限回合
    BreakAction, // 怪物專屬 動作被打斷


                            // 還沒想到
                            //Weak, // 目標攻擊力降低 B
                            //SunderArmor,// 目標防禦力降低 R
                            //OpponentODExchange,// 敵人攻擊力與防禦力交換


    /*
    Damage,//攻擊力的百分比的傷害
    Heal,// 直接治療 百分比
    Shield, // 時間內不受傷害
    Poison, // 時間內持續受到傷害 百分比
    Stun, // 停止活動 回合
    // RegHP, // 持續治療 百分比
    // RegSP, // 持續治療 百分比
    // DMGModify, // 修正傷害 
    // HEALModify, // 修正治療
    Clean, // 清除所有效果
    // GainSP, // 恢復SP
    CostHP, // 消耗HP 百分比
    // DEFModify,// 修正防禦力
    // HITModify,// 修正命中率 不再使用
    // DGEModify,// 修正迴避率 不再使用
    // Ignore,//忽略環境效果
    Might,// 增加攻擊力 直接數據
    // Energy,//集氣速度
    // Wild,//吸收並反射傷害
    // Suffer,//洗掉所有手牌
    // TrueDamage,//無視防禦 直接扣除血量的傷害 百分比
    Reflection, //反射傷害
    Lifesteal, //偷取生命 百分比
    ExchangeHP,//交換生命
    // Deadly, // 降低治療量
    // CrititalPercent,  //能力雙倍的機率
    // DrawPercent,  //吸血機率
    // Unbeatable,  //無敵
    // DrawShild  //吸血盾
    */
}

[System.Serializable]
public class Buff
{
    public UnitAttribute ATK;
    public UnitAttribute DEF;
    public int turn;
    protected int turnCount;
}

public enum EAbilityEffectTarget { Self, Opponent, Both }
[System.Serializable]
public struct AbilityEffect
{
    public EAbilityEffectType type;

    public float value;

    //public int duration;

    //public bool stackable;

    //public EAbilityEffectTarget target;

    public string param;

    public int GetValue()
    {
        return (int)value;
    }

}
/*
public class ActivatedEffect
{
    public AbilityEffectType type;

    public float value;

    public int curDuration;

    public int duration;

    public int stackValue;

    public ActivatedEffect(AbilityEffect effect)
    {
        type = effect.type;
        value = effect.value;
        duration = effect.duration;
        curDuration = 0;
        stackValue = 1;
    }
}
*/

[System.Serializable]
public struct CardAttribute
{
    public int ATK;
    public int DEF;
    public int HEAL;
    public int EN;

    public void Init()
    {
        ATK = 0;
        DEF = 0;
        HEAL = 0;
        EN = 0;
    }
}

[System.Serializable]
public struct Ability
{
    public string id;

    public string name;

    [TextArea]
    public string description;

    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite image;

    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite portrait;

    public GameObject fx;

    public GameObject specialFX;

    public ECardElement element;

    public CardAttribute[] cardAttr;

    public int requireEnergy;

    private bool IsAbility
    {
        get
        {
            return requireEnergy > 0;
        }
    }

    [EnableIf("IsAbility")]
    public AbilityEffect[] effect;
}

