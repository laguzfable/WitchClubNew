using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;
using Kenaz;

public class UIEquipment : CGFadeHelper
{
    UIInventory inventory;

    UIEquipmentItem[] equipSlots = new UIEquipmentItem[4];

    [SerializeField]
    UIEquipmentItem itemPrefab;


    [HideInInspector]
    public UIEquipmentItem dragingObj = null;

    protected override void Init()
    {
        base.Init();
        inventory = GetComponentInChildren<UIInventory>();
        equipSlots = GetComponentsInChildren<UIEquipmentItem>();
    }
    /*
    void Update()
    {
        if(dragingObj != null && Input.GetMouseButton(0))
        {
            dragingObj.transform.position = Input.mousePosition;
        }
        if(Input.GetMouseButtonUp(0))
        {
            var playerData = Toolbox.Instance.GetOrAddComponent<PlayerData>();
            var hit = Physics2D.BoxCast(dragingObj.transform.position, dragingObj.GetComponent<RectTransform>().sizeDelta, 0f, -Vector2.up);
            if(hit.collider != null)
            {
                var eqSlot = hit.collider.gameObject.GetComponent<UIEquipmentItem>();
                if(playerData.equipSlotArr[(int)eqSlot.slot] != null)
                {
                    var eqItem = equipSlots[(int)eqSlot.slot];
                    inventory.GetItemAt(eqItem.data.index).gameObject.SetActive(true);
                    eqItem.data.slot = EEquipmentSlot.None;
                    playerData.equipSlotArr[(int)eqSlot.slot] = null;
                }
                Debug.Log("to slot : " + eqSlot.slot);
                eqSlot.SetData(dragingObj.data);
                dragingObj.data.slot = eqSlot.slot;
                playerData.equipSlotArr[(int)eqSlot.slot] = playerData.inventory.GetItem(dragingObj.data.id) as EquipmentItem;
                inventory.GetItemAt(eqSlot.data.index).gameObject.SetActive(false);
            }
            else
            {
                var inventoryItem = inventory.GetItemAt(dragingObj.data.index);
                inventoryItem.gameObject.SetActive(true);
            }

            Destroy(dragingObj.gameObject);
            dragingObj = null;
        }
    }
    
    public void DragObj(UIEquipmentItem item)
    {
        if(item == null)
        {
            return;
        }

        dragingObj = GameObject.Instantiate<UIEquipmentItem>(itemPrefab, transform);
        
        dragingObj.transform.position = item.transform.position;
        dragingObj.SetData(item.data);
        
        var playerData = Toolbox.Instance.GetOrAddComponent<PlayerData>();
        if(dragingObj.data.slot != EEquipmentSlot.None)
        {
            Debug.Log("from slot : " + dragingObj.data.slot);
            Debug.Log("(int)dragingObj.data.slot : " + (int)dragingObj.data.slot);
            equipSlots[(int)dragingObj.data.slot].SetData(null);
            playerData.equipSlotArr[(int)dragingObj.data.slot] = null;
            dragingObj.data.slot = EEquipmentSlot.None;
        }
        else
        {
            item.gameObject.SetActive(false);
        }
    }

    private void OnDrop(BaseEventData e)
    {
        //Debug.Log("OnDrop");
        Destroy(dragingObj);

        dragingObj = null;
    }
    */

}
