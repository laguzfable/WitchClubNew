using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "item.asset", menuName = "Witch Club/Item/UsableItem")]
public class UsableItem : ItemData
{
    public AbilityEffect[] effects;

    public GameObject fx;

    public AudioClip sfx;

    public override void UseItem(BaseCombatUnit unit)
    {
        unit.MakeEffect(effects, ECardElement.None, true);
    }

    public override EItemType GetItemType()
    {
        return EItemType.Usable;
    }
}