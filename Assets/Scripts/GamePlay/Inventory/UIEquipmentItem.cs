using UnityEngine;
using System.Collections;
using Kenaz;
using UnityEngine.EventSystems;

public class UIEquipmentItem : UIInventoryItem
{
    public EEquipmentSlot slot;
    /*
    protected override void Init()
    {
        base.Init();
        quantityTxt.enabled = false;

        Utility.CreateEvent(gameObject, EventTriggerType.PointerDown, OnBeginDrag);
    }

    private void OnBeginDrag(BaseEventData e)
    {
        //Debug.Log("OnBeginDrag");
        if (GetComponentInParent<UIEquipment>())
        {
            GetComponentInParent<UIEquipment>().DragObj(this);
        }
    }
    */
    public override void OnClick()
    {
        /*
        data.isEquipped = false;
        data = null;
        UpdateInfo();
        */
    }
}
