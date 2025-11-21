using System.Collections.Generic;
using UnityEngine;

public static class MapSpecialOverride
{
    private static Dictionary<string, MapSpecialOverrideData> dict 
        = new Dictionary<string, MapSpecialOverrideData>();

    public static void SetOverride(MapSpecialOverrideData data)
    {
        dict[data.characterName] = data;
        Debug.Log($"[Override] 設定：{data.characterName} → {data.eventName}");
    }

    public static bool TryGet(string characterName, out MapSpecialOverrideData data)
    {
        return dict.TryGetValue(characterName, out data);
    }

    public static void Clear(string characterName)
    {
        if (dict.ContainsKey(characterName))
            dict.Remove(characterName);
    }
}
