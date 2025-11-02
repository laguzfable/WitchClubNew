using System.Collections.Generic;
using UnityEngine;

public enum RuneType { Blue, Red, Green, Yellow }

[System.Serializable]
public class RuneData
{
    public string runeName;
    public Sprite icon;
    public string desc;
    public RuneType type;
}

[CreateAssetMenu(fileName = "RuneDatabase", menuName = "Rune System/Rune Database")]
public class RuneDatabase : ScriptableObject
{
    public List<RuneData> runes = new List<RuneData>();

    public RuneData GetRune(int index)
    {
        if (index < 0 || index >= runes.Count) return null;
        return runes[index];
    }
}
