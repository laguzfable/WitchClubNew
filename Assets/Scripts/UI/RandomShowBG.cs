using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RandomShowBG : MonoBehaviour
{
    [SerializeField]
    Sprite[] backgroupSpr;
    int RandomInt;
    Image image;
     // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        RandomInt = Random.Range(0, backgroupSpr.Length);
        Debug.Log("RandomInt = " + RandomInt);
        image.sprite = backgroupSpr[RandomInt];
    }

}
