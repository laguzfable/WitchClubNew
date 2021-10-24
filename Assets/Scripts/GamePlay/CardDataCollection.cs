using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CardDataCollection : MonoBehaviour
{
    [SerializeField] CardData[] cardDataArr;

    public Dictionary<int, CardData> cardDict { private set; get; }

    private void Awake()
    {
        cardDict = cardDataArr.ToDictionary(item => int.Parse(item.ID));
    }

}