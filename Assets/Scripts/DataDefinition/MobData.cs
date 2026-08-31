using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "MobData", menuName = "Witch Club/MobData")]
public class MobData : ScriptableObject
{
    public int HP = 100;
    public int EN = 10;

    public int levelUpMaxBonusValue = 3;

    public int maxSelectCardCount = 1;

    // 王等級的怪，數值是照劇情戰鬥調的，不能混進女巫競技場的隨機池
    public bool isBoss = false;

    public string displayName;
    
    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite sprite;

    public MobElementData[] elementData = new MobElementData[]{
        new MobElementData(){element = ECardElement.Blue},
        new MobElementData(){element = ECardElement.Red}, 
        new MobElementData(){element = ECardElement.Yellow}, 
        new MobElementData(){element = ECardElement.Green} };

    public MobAbility[] ability;

    [Title("台詞")]
    [InfoBox("戰鬥中怪物會在這些時機講話。每一格填幾句，實際會隨機挑一句；留空就不講。")]
    public MobTalkLines talk = new MobTalkLines();

    public MobData()
    {

    }
}


[System.Serializable]
public class MobElementData
{
    public ECardElement element;
    public CardAttribute attribute;

    public int randomWeight = 1000;
    public int decisionWeight = 1000;

    public GameObject fx;
}

/// <summary>怪物在戰鬥中會講的話。每個時機各自一組，隨機挑一句講。</summary>
[System.Serializable]
public class MobTalkLines
{
    [LabelText("開場")] public string[] battleStart;

    [LabelText("出手前")]
    [Tooltip("決定這回合要做什麼的時候。每回合有機率講，不會每次都講。")]
    public string[] act;

    [LabelText("被打到")]
    [Tooltip("受到傷害時。有機率講，而且兩句之間會隔一段時間，不會一直吵。")]
    public string[] hurt;

    [LabelText("剩下不多")]
    [Tooltip("血量第一次掉到三成以下時講一次。")]
    public string[] lowHp;

    [LabelText("被打倒")] public string[] defeated;

    public bool Any =>
        (battleStart != null && battleStart.Length > 0) ||
        (act != null && act.Length > 0) ||
        (hurt != null && hurt.Length > 0) ||
        (lowHp != null && lowHp.Length > 0) ||
        (defeated != null && defeated.Length > 0);
}

[System.Serializable]
public class MobAbility
{
    public string abilityId;
    public GameObject fx;

    public int decisionWeight = 1000;
}