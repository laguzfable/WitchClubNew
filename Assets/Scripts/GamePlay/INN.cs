using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class INN : MonoBehaviour
{
    [SerializeField]
    GameObject roomObj;
    [SerializeField]
    GameObject Sophia;
    public void GoRoom()
    {
        roomObj.SetActive(true);
        Sophia.SetActive(false);
    }
}

