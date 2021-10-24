using UnityEngine;

[CreateAssetMenu(fileName = "MobData", menuName = "Witch Club/MobData")]
public class MobData : ScriptableObject
{
    public int HP = 100;
    public int EN = 10;

    public int levelUpMaxBonusValue = 3;
    public MobElementData[] elementData = new MobElementData[]{
        new MobElementData(){element = ECardElement.Blue},
        new MobElementData(){element = ECardElement.Red}, 
        new MobElementData(){element = ECardElement.Yellow}, 
        new MobElementData(){element = ECardElement.Green} };
    public MobElementData[] proModeElementData = new MobElementData[]{
        new MobElementData(){element = ECardElement.Blue},
        new MobElementData(){element = ECardElement.Red}, 
        new MobElementData(){element = ECardElement.Yellow}, 
        new MobElementData(){element = ECardElement.Green} };

    public MobAbility[] ability;

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

[System.Serializable]
public class MobAbility
{
    public string abilityId;
    public GameObject fx;

    public int decisionWeight = 1000;
}