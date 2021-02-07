using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetDataToStart : MonoBehaviour
{
    void Awake()
    {
        //Toolbox.Instance.GetOrAddComponent<PlayerData>().Reset();
        PlayerData.Instance.Reset();
    }
}
