using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EUIInventoryMode { Normal, Store, Equipment };

public class UIInventory : CGFadeHelper
{
    //get data reference from player controller.
    Inventory inventory;

    [SerializeField]
    UIInventoryItem itemPrefab;
    [SerializeField]
    GameObject pagePrefab;
    public List<UIInventoryItem> itemList = new List<UIInventoryItem>();  //教學模式需要開pulbie來用
    [SerializeField]
    Slider pageSlider;
    [SerializeField]
    List<GameObject> itemPages = new List<GameObject>();

    int curPage;
    int maxPage = 1;

    [SerializeField]
    EUIInventoryMode mode;

    DataCollection<ItemData> itemTable;

    public void _TeachModeItem()
    {
        //教學模式清除Item並重置
        for(int i = itemList.Count - 1; i >= 0; i--)
        {
            Destroy(itemList[i].gameObject);
            itemList.Remove(itemList[i]);
        }
        Init();
    }

    protected override void Init()
    {
        inventory = PlayerData.Instance.inventory;

        var go = Instantiate(pagePrefab, transform);
        itemPages.Add(go);

        if (mode == EUIInventoryMode.Store)
        {
            inventory.OnAddItem += AddItem;
            inventory.OnRemoveItem += RemoveItem;
            Debug.Log("add Event");
            gameObject.SetActive(false);
        }

        if (itemPrefab == null)
        {
            gameObject.SetActive(false);
            return;
        }
        if (pagePrefab == null)
        {
            gameObject.SetActive(false);
            return;
        }
        //container = transform.Find("Scroll View/Viewport/Content");

        itemTable = Toolbox.Instance.GetOrAddComponent<DataService>().itemData;

        if (inventory.GetList() != null)
        {
            itemList.Clear();
            if (mode == EUIInventoryMode.Store)
            {
                int i = 0;
                foreach (var data in inventory.GetList())
                {
                    var itemData = itemTable.GetDataByName(data.id);
                    if (null == itemData)
                    {
                        itemData = itemTable.GetDataByID(data.id);
                    }
                    if (itemData.isTradeable)
                    {
                        //這邊的itemPrefab應該要是UIStoreInventoryItem
                        UIInventoryItem item = Instantiate<UIInventoryItem>(itemPrefab, itemPages[maxPage - 1].transform);
                        data.index = itemList.Count;
                        item.SetData(data);
                        itemList.Add(item);
                        item.gameObject.SetActive(true);
                        i++;
                        if (i > 16)
                        {
                            var newPage = Instantiate(pagePrefab, transform);
                            itemPages.Add(newPage);
                            maxPage++;
                            i = 1;
                        }
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (var data in inventory.GetList())
                {
                    UIInventoryItem item = Instantiate<UIInventoryItem>(itemPrefab, itemPages[maxPage - 1].transform);
                    data.index = itemList.Count;
                    item.SetData(data);
                    itemList.Add(item);
                    item.gameObject.SetActive(true);
                    i++;
                    if (i > 16)
                    {
                        var newPage = Instantiate(pagePrefab, transform);
                        itemPages.Add(newPage);
                        maxPage++;
                        i = 1;
                    }
                }
            }
        }
    }
    /*
    private void OnDestroy()
    {
        Toolbox.Instance.GetOrAddComponent<PlayerData>().SetInvData(invData);
    }
    */
    //打開道具頁面會歸0
    public void ResetPage()
    {
        for (int i = 0; i < itemPages.Count; i++)
        {
            itemPages[i].SetActive(false);
        }
        itemPages[0].SetActive(true);
        pageSlider.value = 0;
    }

    //切換不同道具欄頁面
    public void ChangePage()
    {
        for (int i = 0; i < itemPages.Count; i++)
        {
            itemPages[i].SetActive(false);
        }
        float sliderValue = 1.0f / (itemPages.Count - 1);
        curPage = (int)(Mathf.Round(pageSlider.value / sliderValue));
        if (curPage < 0)
        {
            curPage = 0;
        }
        else if (curPage >= maxPage - 1)
        {
            curPage = maxPage - 1;
        }
        itemPages[curPage].SetActive(true);
    }

    public void ChackPageSliderValue()
    {
        float sliderValue = 1.0f / (itemPages.Count - 1);
        pageSlider.value = curPage * sliderValue;
    }

    public void AddItem(InventoryData data)
    {
        if (itemList.Count + 1 % 16 == 1)
        {
            var newPage = Instantiate(pagePrefab, transform);
            itemPages.Add(newPage);
            maxPage++;
        }
        UIInventoryItem item = Instantiate<UIInventoryItem>(itemPrefab, itemPages[maxPage - 1].transform);
        item.SetData(data);
        itemList.Add(item);
        item.gameObject.SetActive(true);
    }

    public void RemoveItem(InventoryData data)
    {
        foreach(var item in itemList)
        {
            if(item.data.id.Equals(data.id))
            {
                itemList.Remove(item);
                Destroy(item.gameObject);
                if (itemList.Count % 16 == 0)
                {
                    Destroy(itemPages[maxPage - 1].transform);
                    maxPage--;
                }
                break;
            }
        }
    }

    public InventoryData GetDataAt(int index)
    {
        if(index < 0 || index >= inventory.GetList().Count)
        {
            return null;
        }
        return inventory.GetList()[index];
    }

    public UIInventoryItem GetItemAt(int index)
    {
        if(index < 0 || index >= itemList.Count)
        {
            return null;
        }
        return itemList[index];
    }
}
