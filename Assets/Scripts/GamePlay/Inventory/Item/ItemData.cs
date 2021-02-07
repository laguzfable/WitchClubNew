using UnityEngine;
using Sirenix.OdinInspector;

public enum EItemType { Usable, Equipment, Quest, Other };
public abstract class ItemData : ScriptableObjectID
{
    [TextArea]
    public string discription;

    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite image;

    public int price;

    public bool isTradeable;

    public bool isStackable;

    public abstract void UseItem(BaseCombatUnit unit);

    public virtual EItemType GetItemType()
    {
        return EItemType.Other;
    }
}
