using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var itemData = Toolbox.Instance.GetOrAddComponent<DataService>().itemData;
        Debug.Log("length : " + itemData.GetLength());

        itemData.DisplayItemNames();
    }
}
