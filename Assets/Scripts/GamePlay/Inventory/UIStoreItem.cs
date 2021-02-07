using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIStoreItem : MonoBehaviour
{
    public bool isBuy = true;// false is sell item to store.

    public string itemID;

    protected Button btn;

    protected Image img;

    protected Text priceTxt, nameTxt;

    ItemData itemData;

    [SerializeField]
    Text discTxt;

    public bool isSold = false; // for buy item.

    [SerializeField]
    GameObject soldImg;

    void Start()
    {
        btn = GetComponent<Button>();
        img = transform.Find("Image").GetComponent<Image>();
        priceTxt = transform.Find("PriceText").GetComponent<Text>();
        nameTxt = transform.Find("ItemNameText").GetComponent<Text>();

        if(!string.IsNullOrEmpty(itemID))
        {
            itemData = GetItem(itemID);
            btn.onClick.AddListener(OnClick);
            Kenaz.Utility.CreateEvent(gameObject, EventTriggerType.PointerEnter, OnMouseEnter);
            Kenaz.Utility.CreateEvent(gameObject, EventTriggerType.PointerExit, OnMouseExit);
        }

        UpdateInfo();        
    }

    void OnMouseEnter(BaseEventData e)
    {
        //display discription
        //discTxt.text = itemData.discription;
    }

    void OnMouseExit(BaseEventData e)
    {

    }

    protected ItemData GetItem(string key)
    {
        var ic = Toolbox.Instance.GetOrAddComponent<DataService>().itemData;
        var itemData = ic.GetDataByName(key);
        if (null == itemData)
        {
            itemData = ic.GetDataByID(key);
        }
        return itemData;
    }

    public void UpdateInfo()
    {
        if (itemData != null && priceTxt != null && nameTxt != null)
        {
            //get item data from service
            priceTxt.text = itemData.price.ToString();
            nameTxt.text = itemData.displayName;
            img.sprite = itemData.image;
            img.gameObject.SetActive(true);
        }
        else
        {
            priceTxt.text = "";
            nameTxt.text = "";
            img.gameObject.SetActive(false);
        }
    }

    public void OnClick()
    {
        var playerData = PlayerData.Instance;// Toolbox.Instance.GetOrAddComponent<PlayerData>();
        if(!isSold && playerData.money >= itemData.price)
        {
            //Debug.Log("買!!");
            //先出邏輯
            playerData.money -= itemData.price;
            isSold = true;
            soldImg.SetActive(true);
            playerData.inventory.AddItem(itemID, 1);
        }
    }
}
