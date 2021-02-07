using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Inventory
{
    List<InventoryData> invData = new List<InventoryData>();

    public event System.Action<InventoryData> OnAddItem;
    public event System.Action<InventoryData> OnRemoveItem;

    public void AddItem(string id, int quantity = 1)
    {
        if (quantity == 0)
        {
            return;
        }
        var data = GetDateByID(id);
        if (data != null && data.isStackable)
        {
            data.quantity += quantity;
        }
        else
        {
            var stackable = GetItem(id).isStackable;
            if (stackable)
            {
                var newData = new InventoryData { id = id, quantity = quantity, isStackable = true, index = invData.Count };
                invData.Add(newData);
                OnAddItem?.Invoke(newData);
            }
            else
            {
                for (int i = 0; i < quantity; i++)
                {
                    var newData = new InventoryData { id = id, quantity = 1, isStackable = false, index = invData.Count };
                    invData.Add(newData);
                    OnAddItem?.Invoke(newData);
                }
            }
        }
    }

    public ItemData GetItem(string key)
    {
        var ic = Toolbox.Instance.GetOrAddComponent<DataService>().itemData;
        var itemData = ic.GetDataByName(key);
        if (null == itemData)
        {
            itemData = ic.GetDataByID(key);
        }
        return itemData;
    }

    public void RemoveItem(string id, int quantity)
    {
        if (quantity == 0)
        {
            return;
        }
        var data = GetDateByID(id);
        if (data != null && data.quantity >= quantity)
        {
            data.quantity -= quantity;
            if(data.quantity <= 0)
            {
                //不要移除東西保持index的正確性
                //invData.Remove(data);
                OnRemoveItem?.Invoke(data);
            }
        }
    }

    public InventoryData GetDateByID(string id)
    {
        foreach (var data in invData)
        {
            if (data.id.Equals(id))
            {
                return data;
            }
        }
        return null;
    }

    public List<InventoryData> GetList()
    {
        return invData;
    }

    public void Reset()
    {
        invData.Clear();
    }
}

[System.Serializable]
public class InventoryData
{
    public event System.Action OnQuantityUpdate;

    [Header("ID請輸入物品的檔案名稱 搜尋較快")]
    public string id;
    //public int quantity;
    private int _quantity;
    public int quantity
    {
        set
        {
            _quantity = value;
            OnQuantityUpdate?.Invoke();
        }
        get
        {
            return _quantity;
        }
    }
    public int index;
    public bool isStackable;

    public EEquipmentSlot slot = EEquipmentSlot.None;
}
