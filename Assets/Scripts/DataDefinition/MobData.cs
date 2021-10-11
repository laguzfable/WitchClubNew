using UnityEngine;

[CreateAssetMenu(fileName = "MobData", menuName = "Witch Club/MobData")]
public class MobData : ScriptableObject
{
    public int HP;
    public MobElementData[] elementData;

    public MobAbility[] ability;

}


[System.Serializable]
public class MobElementData
{
    public ECardElement element;
    public CardAttribute attribute;

    public int decisionWeight;

    public GameObject fx;
}

[System.Serializable]
public class MobAbility
{
    public int requireEnergy;
    public AbilityEffect[] effect;
    public GameObject fx;
}