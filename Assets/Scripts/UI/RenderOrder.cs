using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderOrder : MonoBehaviour
{
    MeshRenderer meshrender;
    [SerializeField]
    int renderOrderID;
    // Start is called before the first frame update
    void Start()
    {
        meshrender = gameObject.GetComponent<MeshRenderer>();
        meshrender.sortingOrder = renderOrderID;
    }
}
