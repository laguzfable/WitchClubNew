using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatVisualResources : MonoBehaviour
{
    [SerializeField]
    GameObject[] cardFXArr = new GameObject[8];

    //readonly int[] index = new int[] {1, 2, 4, 8, 101, 102, 103, 104 };

    Dictionary<int, int> indexDict = new Dictionary<int, int>();

    Vector3 zeroPos = new Vector3(0, 0, 0);
    Vector3 fxPos = new Vector3(0, 1.7f, -1f);


    private void Awake()
    {
        indexDict.Add(1, 0);
        indexDict.Add(2, 1);
        indexDict.Add(4, 2);
        indexDict.Add(8, 3);
        indexDict.Add(101, 4);
        indexDict.Add(102, 5);
        indexDict.Add(103, 6);
        indexDict.Add(104, 7);
    }

    public void GetCardFX(int index)
    {
        if(indexDict[index] >= cardFXArr.Length || cardFXArr[indexDict[index]] == null)
        {
            return;
        }
        var fx = Instantiate(cardFXArr[indexDict[index]], fxPos, Quaternion.identity);
        Destroy(fx, 1f);
    }

    public Sprite GetBGByName(string name)
    {
        /*
        foreach(var spr in bgArr)
        {
            if(spr.name.Equals(name))
            {
                return spr;
            }
        }

        return null;*/
        return Resources.Load<Sprite>($"background/{name}");
    }

    public Sprite GetMobByName(string name)
    {
        /*
        foreach (var spr in mobArr)
        {
            if (spr.name.Equals(name))
            {
                return spr;
            }
        }
        */

        return Resources.Load<Sprite>($"monsters/{name}");
    }
}
