using UnityEngine;
using System.Collections;

public enum EEquipmentSlot { LeftHand, RightHand, LeftLeg, RightLeg, None };
[CreateAssetMenu(fileName = "equipment.asset", menuName = "Witch Club/Item/EquipmentItem")]
public class EquipmentItem : ItemData
{
    public EEquipmentSlot slot = EEquipmentSlot.None;
    public EEquipmentEffect[] effect;

    public override void UseItem(BaseCombatUnit unit)
    {
        /*
        if(unit is PlayerUnit)
        {
            var playerUnit = unit as PlayerUnit;
            var oldEqItem = playerUnit.equipSlotArr[(int)slot];
            if(oldEqItem != null)
            {
                playerUnit.inventory.GetDateByID(oldEqItem.ID).slot = EEquipmentSlot.None;
            }
            playerUnit.inventory.GetDateByID(ID).slot = slot;
            playerUnit.equipSlotArr[(int)slot] = this;
            playerUnit.CalculateEquipmentAttribute();
        }
        */
    }

    public override EItemType GetItemType()
    {
        return EItemType.Equipment;
    }
}

public enum EItemValueType
{
    ATK, R_ATK, G_ATK, B_ATK, Y_ATK, SuccessChance, DEF, R_DEF, G_DEF, B_DEF, Y_DEF,
    RestoreHP, RestoreSP, RestoreHP_Percent, RestoreSP_Persent, None
}

[System.Serializable]
public struct EEquipmentEffect
{
    public EItemValueType type;
    public float value;
}