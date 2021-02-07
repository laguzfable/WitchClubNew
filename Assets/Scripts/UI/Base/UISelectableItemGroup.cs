using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelectableItemGroup : MonoBehaviour {

    protected List<UISelectableItem> items = new List<UISelectableItem>();
    public virtual void Clear()
    {
        items.Clear();
    }

    public virtual void Add(UISelectableItem item)
    {
        items.Add(item);
    }
    
    public virtual void OnSelect(UISelectableItem item)
    {
        for(int i = 0; i < items.Count; i++)
        {
            items[i].Select(item == items[i]);
        }
    }
}
