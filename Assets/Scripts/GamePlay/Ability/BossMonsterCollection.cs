using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossMonsterCollection.asset", menuName = "Witch Club/Monster/BossMonsterCollection")]
public class BossMonsterCollection : ScriptableObject {
    public List<BossMonsterList> bossMonsterList;
}

[System.Serializable]
public struct BossMonsterList {
    public string id;
    public GameObject prefabObj;
    public MonsterType type;
}

[System.Serializable]
public enum MonsterType
{
    Boss,
    Monster
}