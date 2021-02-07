using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/*
    沒在用
*/
[CreateAssetMenu(fileName ="Card.asset", menuName = "Witch Club/Card/CardBase")]
public class CardBase : ScriptableObject
{
    public string id;

    public string cardName;

    [PreviewField(80, ObjectFieldAlignment.Left)]
    public Sprite image;

    [TextArea, BoxGroup("Card")]
    public string normalDiscription;
    [BoxGroup("Card")]
    public ECardElement element;
    [BoxGroup("Card")]
    public float cost;
    [BoxGroup("Card")]
    public AbilityEffect[] cardEffect;
    [BoxGroup("Card")]
    public GameObject fx;

    [BoxGroup("Ability")]
    public string abilityName;
    [TextArea, BoxGroup("Ability")]
    public string discription;
    [PreviewField(80, ObjectFieldAlignment.Left), BoxGroup("Ability")]
    public Sprite abilityImage;
    [BoxGroup("Ability")]
    public int requireEnergy;
    [BoxGroup("Ability")]
    public AbilityEffect[] effect;
    [BoxGroup("Ability")]
    public GameObject specialFX;

    public virtual void Cast(BaseCombatUnit caster, BaseCombatUnit target)
    {
        //MakeEffect(ability.effect, ability.element);
    }
}