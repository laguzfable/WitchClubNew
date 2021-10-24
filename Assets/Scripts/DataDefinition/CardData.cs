using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "CardData", menuName = "Witch Club/CardData")]
public class CardData : ScriptableObjectID
{
    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite image;

    public ECardElement element;

    public CardAttribute[] cardAttr = new CardAttribute[5];

    public CardAttribute proModeAttr;
    public CardAttribute proModeAttrLevelUpWeight;


    public bool IsCharacter()
    {
        return int.Parse(ID) < 10;
    }
    
}