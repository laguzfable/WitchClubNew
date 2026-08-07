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

    // 普通卡的 A/B 變體 ID 規則：基礎ID*10 + 1(A)/2(B)，例如 101 → 1011/1012
    public CardData GetCardData(int logicalId)
    {
        var element = ElementFromLogicalId(logicalId);
        if (element == null)
            return cardDict[logicalId];

        string variant = PlayerData.Instance.usingCardVariant[(int)element.Value];
        int resolvedId = variant == "A" ? logicalId * 10 + 1
                        : variant == "B" ? logicalId * 10 + 2
                        : logicalId;

        return cardDict.TryGetValue(resolvedId, out var cd) ? cd : cardDict[logicalId];
    }

    static ECardElement? ElementFromLogicalId(int logicalId)
    {
        switch (logicalId)
        {
            case 101: return ECardElement.Red;
            case 102: return ECardElement.Blue;
            case 103: return ECardElement.Green;
            case 104: return ECardElement.Yellow;
            default: return null;
        }
    }

}