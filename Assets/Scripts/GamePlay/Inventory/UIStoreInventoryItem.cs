using UnityEngine;
using System.Collections;

public class UIStoreInventoryItem : UIInventoryItem
{
    public override void OnClick()
    {
        if (data.quantity > 0)
        {
            var itemData = GetItem(data.id);
            data.quantity--;
            UpdateInfo();
            var playerData = PlayerData.Instance; ;//Toolbox.Instance.GetOrAddComponent<PlayerData>();
            playerData.money += itemData.price / 2;
            //應該還要跑出確認視窗之後再賣出
        }
    }

}
