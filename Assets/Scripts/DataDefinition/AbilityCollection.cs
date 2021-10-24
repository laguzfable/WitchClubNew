using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Kenaz;

[CreateAssetMenu(fileName = "AbilityCollection.asset", menuName = "Witch Club/AbilityCollection")]
public class AbilityCollection : ScriptableObject
{
    [ListDrawerSettings(NumberOfItemsPerPage = 5)]
    public List<Ability> abilityList;
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

    public GameObject specialFX;

    public ECardElement element;

    public int requireEnergy;
    public AbilityEffect[] effect;
}

[System.Serializable]
public struct AbilityEffect
{
    public EAbilityEffectType type;

    public float value;

    public int duration;

    public string param;

    public int GetValue()
    {
        return (int)value;
    }

}

[System.Serializable]
public enum EAbilityEffectType
{
    /// <summary>
    /// 攻擊力增加
    /// </summary>
    IncreaseATK,

    /// <summary>
    /// 防禦力增加
    /// </summary>
    IncreaseDEF,

    /// <summary>
    /// 治療量增加
    /// </summary>
    IncreaseHeal,

    /// <summary>
    /// 卡牌等級提升
    /// </summary>
    LevelUp,

    /// <summary>
    /// 直接傷害目標 無視防禦
    /// </summary>
    DirectDamage,

    /// <summary>
    /// 反射受到的傷害 無視防禦
    /// </summary>
    Reflect,

    /// <summary>
    /// 立刻獲得能量(除了黃色)
    /// </summary>
    InstantEnergy,

    /// <summary>
    /// 恢復成功造成傷害的治療量
    /// </summary>
    LifeSteal,

    /// <summary>
    /// !攻擊力與防禦力交換
    /// </summary>
    SelfODExchange,

    /// <summary>
    /// !過溢的防禦力轉換為傷害攻擊目標
    /// </summary>
    Revenge,

    /// <summary>
    /// 更換目標行動(old:直接使目標的強力技能失效)
    /// </summary>
    Interrupt,

    /// <summary>
    /// 使此次的傷害無效
    /// </summary>
    Shield,

    /// <summary>
    /// 直接進行治療
    /// </summary>
    InstantHeal,

    /// <summary>
    /// !所有的數值轉換為治療
    /// </summary>
    FocusHeal,

    /// <summary>
    /// 提升HP上限並恢復所有HP
    /// </summary>
    IncreaseMaximumHP,

    /// <summary>
    /// 忽略環境效果
    /// </summary>
    IgnoreEnvironmentEffect,

    /// <summary>
    /// 洗掉手牌
    /// </summary>
    Shuffle,

    /// <summary>
    /// 防禦力為0
    /// </summary>
    NoArmor,

    /// <summary>
    /// 無視卡牌顏色
    /// </summary>
    IgnoreElement,

    /// <summary>
    /// 持續治療
    /// </summary>
    HOT,

    /// <summary>
    /// 更換環境效果
    /// </summary>
    ChangeEnvironmentEffect,

    /// <summary>
    /// 治療同時給予傷害
    /// </summary>
    HealingAttack,

    /// <summary>
    /// 下回合無法行動
    /// </summary>
    Stun,

    /// <summary>
    /// !時間暫停 時間內無限回合
    /// </summary>
    TheWorld,

    /// <summary>
    /// 怪物專屬 動作被打斷
    /// </summary>
    BreakAction,

    /// <summary>
    /// 抗魔裝甲
    /// </summary>
    MagicArmor,

    /// <summary>
    /// 抗魔裝甲EX
    /// </summary>
    MagicArmorEX,

    /// <summary>
    /// 持續傷害
    /// </summary>
    DOT,

    /// <summary>
    /// !詛咒 會無法使用部分手牌
    /// </summary>
    Curse,

    /// <summary>
    /// !玩家卡片元素隨機更改
    /// </summary>
    ElementChange,

    /// <summary>
    /// 本次攻擊力增加20%
    /// </summary>
    IncreaseATK2,



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


/*
[System.Serializable]
public class Buff
{
    public UnitAttribute ATK;
    public UnitAttribute DEF;
    public int turn;
    protected int turnCount;
}
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

public enum EAbilityEffectTarget { Self, Opponent, Both }
*/