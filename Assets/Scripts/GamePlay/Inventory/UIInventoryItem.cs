using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryItem : MonoBehaviour {

    protected Button btn;

    protected Image img;

    protected Text quantityTxt, nameTxt;

    public InventoryData data { protected set; get; }
    protected AudioSource audioSource;


    void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
        audioSource = gameObject.AddComponent<AudioSource>();
        quantityTxt = transform.Find("QuantityText").GetComponent<Text>();
        nameTxt = transform.Find("ItemNameText").GetComponent<Text>();

        btn.onClick.AddListener(OnClick);

        UpdateInfo();
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

    public virtual void UpdateInfo()
    {
        if (data != null)
        {
            if (quantityTxt != null && nameTxt != null)
            {
                quantityTxt.text = data.quantity.ToString();
                //get item data from service
                var itemData = GetItem(data.id);
                nameTxt.text = itemData.displayName;
                img.sprite = itemData.image;
                img.enabled = true;
            }
        }
        else
        {
            img.enabled = false;
            quantityTxt.enabled = false;
            nameTxt.enabled = false;
        }
    }

    public void SetData(InventoryData newData)
    {
        if(data != null)
        {
            data.OnQuantityUpdate -= UpdateInfo;
        }
        if(newData != null)
        {
            newData.OnQuantityUpdate += UpdateInfo;
        }
        data = newData;
        UpdateInfo();
    }

    public virtual void OnClick()
    {
        var cbtSys = GameObject.FindGameObjectWithTag("GameController").GetComponent<CombatSystem>();

        if(data.quantity > 0)
        {
            var itemData = GetItem(data.id);
            if (itemData.GetItemType() == EItemType.Usable)
            {
                var usableItem = itemData as UsableItem;
                audioSource.clip = usableItem.sfx;
                usableItem.UseItem(GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().GetPlayerUnit());
                audioSource.Play();
                data.quantity--;
                //UpdateInfo();
                if (data.quantity <= 0)
                {
                    //destroy this;
                    Destroy(gameObject);
                }
            }
        }
    }
}
