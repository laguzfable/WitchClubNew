using UnityEngine;

[CreateAssetMenu(fileName = "MobData", menuName = "Witch Club/MobData")]
public class MobData : ScriptableObject
{
    public int HP;
    public MobElementData elementData;

}


[System.Serializable]
public class MobElementData
{
    public ECardElement element;
    public CardAttribute attribute;

    public int decisionWeight;

    public GameObject fx;
}