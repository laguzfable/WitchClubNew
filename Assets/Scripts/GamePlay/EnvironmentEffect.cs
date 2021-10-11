using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Naninovel;

public enum EEnvEffectType { None, Attack, Defense, Heal, Energy, RedSilence, BlueSilence, GreenSilence, YellowSilence, LimitCards, NoCharacter, NoRune, NoHeal, NoDefense, Length };


/// <summary>
/// 日後要將他移除MonoBehaviour 因為沒用到
/// </summary>
public sealed class EnvironmentEffect
{
    public EEnvEffectType curType { private set; get; } = EEnvEffectType.None;

    public EEnvEffectType nextType { private set; get; } = EEnvEffectType.None;

    static readonly string[] envEffDescription = { "無任何效果", "攻擊雙倍", "防禦雙倍", "治療雙倍", "獲得能量雙倍", "禁用血系卡牌", "禁用學院系卡牌", "禁用自然系卡牌", "禁用惡魔系卡牌", "限制最多2張卡牌", "禁用角色卡", "禁用符文", "治療無效", "防禦無效" };

    static readonly string[] envEffName = { "風和日麗", "絳紅之夜", "高塔之暮", "生命之雨", "魔力狂潮", "沉默：紅", "沉默：藍", "沉默：綠", "沉默：黃", "能量束縛", "寂靜破曉", "符文封印", "虛弱結界", "護盾瓦解" };

    static int[] envWeight = {10000, 2000, 2000, 2000, 2000, 500, 500, 500, 500, 500, 500, 500, 500, 500};

    public int remainTurn = 1;

    int nextEffRemainTurn;


    public UnityAction envEffChangeEvent;

    CombatSystem combatSystem;

    public EnvironmentEffect(CombatSystem combatSystem)
    {
        this.combatSystem = combatSystem;

        
        // 黃色卡還沒可以使用之前將魔力狂潮的權重設為0
        envWeight[(int)EEnvEffectType.Energy] = combatSystem.IsEnergyActive() ? 2000 : 0;
    }

    public void SetNextEffect(EEnvEffectType newEffect, int turn = 1)
    {
        nextType = newEffect;
        nextEffRemainTurn = turn;
        remainTurn = 1;
        envEffChangeEvent?.Invoke();
    }

    public void SetCurrentEffect(EEnvEffectType newEffect, int turn = 1)
    {
        curType = newEffect;
        remainTurn = turn;
        envEffChangeEvent?.Invoke();
    }

    public void GetNextEffect()
    {
        /*
        do
        {
            nextType = (EEnvEffectType)Random.Range(0, (int)EEnvEffectType.Length);
        }
        while (nextType == curType);*/

        var rndList = new List<RandomTool.RandomObject>();
        for (int i = 0; i < (int)EEnvEffectType.Length; i++)
        {
            var rndObj = new RandomTool.RandomObject();
            rndObj.SetIndex(i);
            rndObj.Weight = envWeight[i];
            rndList.Add(rndObj);
        }

        var rnd = RandomTool.RandomHelper.GetRandomList(rndList);
        nextType = (EEnvEffectType)rnd.Index;

        nextEffRemainTurn = 1;//Random.Range(1, 4);
        envEffChangeEvent?.Invoke();
    }

    public void SwitchToNextEffect()
    {
        curType = nextType;
        remainTurn = nextEffRemainTurn;
        envEffChangeEvent?.Invoke();
    }

    public string GetCurEffectName(int index)
    {
        return envEffName[index];
    }

    public string GetCurDescription(int index)
    {
        return envEffDescription[index];
    }
}