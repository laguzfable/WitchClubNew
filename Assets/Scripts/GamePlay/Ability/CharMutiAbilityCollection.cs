using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharMutiAbilityCollection.asset", menuName = "Witch Club/Ability/CharMutiAbilityCollection")]
public class CharMutiAbilityCollection : ScriptableObject {
    public List<CharMutiAbility> charMutiAbility;
}

[System.Serializable]
public struct CharMutiAbility {
    public EElementState elementState;
    public Ability ability;
}