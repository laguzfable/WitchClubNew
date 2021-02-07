using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "MobAction.asset", menuName = "Witch Club/MobAction")]
public class MobAction : ScriptableObject
{
    public EMobActionType type;

    public int minATK, maxATK;

    public int minDEF, maxDEF;

    public int minHEAL, maxHEAL;

    public EMobActionDisplayType displayType;

    [EnableIf("IsPowerAction")]
    public int turn;

    [EnableIf("IsPowerAction")]
    public EBreakConditionType breakType;

    [EnableIf("IsPowerAction")]
    public int breakValue;

    public MobAction nextAction;

    private bool IsPowerAction
    {
        get
        {
            return type == EMobActionType.Power;
        }
    }

    public bool isChangeEnvironmentEffect = false;

    [EnableIf("isChangeEnvironmentEffect")]
    public EEnvEffectType targetEnvEffect = EEnvEffectType.None;

    public bool isCastAbility = false;

    [EnableIf("isCastAbility")]
    public string abilityID;
}

public enum EMobActionType { Normal, Power };
public enum EMobActionDisplayType { ShowAll, HideATK, HideDEF, HideHEAL, HideAll };
public enum EBreakConditionType { HP, ATK/*, DEF, HEAL*/ };

