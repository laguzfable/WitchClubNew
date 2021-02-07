using UnityEngine;

[CreateAssetMenu(fileName = "gift.asset", menuName = "Witch Club/Item/GiftItem")]
public class GiftItem : ItemData
{

    [SerializeField]
    GiftTarget[] targetArr;

    public override void UseItem(BaseCombatUnit unit)
    {
        
    }
}

[System.Serializable]
public class GiftTarget
{
    public ENPCPerference target;
    public int perferenceValue;
}
