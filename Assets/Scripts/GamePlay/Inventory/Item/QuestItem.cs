using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "quest.asset", menuName = "Witch Club/Item/QuestItem")]
public class QuestItem : ItemData
{
    public override void UseItem(BaseCombatUnit unit)
    {
        Debug.Log("Can't use quest item!");
    }

    public override EItemType GetItemType()
    {
        return EItemType.Quest;
    }
}