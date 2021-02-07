using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharMutiCgCollection.asset", menuName = "Witch Club/Cg/CharMutiCgCollection")]
public class CharMutiCgCollection : ScriptableObject {
    public List<CharMutiCgList> charMutiCg;
}

[System.Serializable]
public struct CharMutiCgList
{
    public GameObject cgPrefab;
    public EElementState elementState;
    public float showCgTime;
}