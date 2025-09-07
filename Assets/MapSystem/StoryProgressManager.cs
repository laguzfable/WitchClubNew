using System.Collections.Generic;
using UnityEngine;

// 這是一個最基本的 Singleton（單例），確保全遊戲只有一個這個管理器
public class StoryProgressManager : MonoBehaviour
{
    public static StoryProgressManager Instance { get; private set; }

    // 每個角色的白天進度快取
    private Dictionary<string, int> dayProgress = new Dictionary<string, int>();

    // 每個角色的晚上進度快取
    private Dictionary<string, int> nightProgress = new Dictionary<string, int>();

    private const string DayKeySuffix = "_Event_Day";
    private const string NightKeySuffix = "_Event_Night";

    private void Awake()
    {
        // 如果已有一個實例，銷毀多餘的
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 場景切換不會被銷毀
    }

    // 查詢角色白天進度
    public int GetDayProgress(string characterName)
    {
        if (!dayProgress.TryGetValue(characterName, out int value))
        {
            value = PlayerPrefs.GetInt(characterName + DayKeySuffix, 0);
            dayProgress[characterName] = value;
        }
        return value;
    }

    // 更新角色白天進度並寫入 PlayerPrefs
    public void IncrementDayProgress(string characterName)
    {
        int value = GetDayProgress(characterName) + 1;
        dayProgress[characterName] = value;
        PlayerPrefs.SetInt(characterName + DayKeySuffix, value);
        PlayerPrefs.Save();
    }

    // 查詢角色晚上進度
    public int GetNightProgress(string characterName)
    {
        if (!nightProgress.TryGetValue(characterName, out int value))
        {
            value = PlayerPrefs.GetInt(characterName + NightKeySuffix, 0);
            nightProgress[characterName] = value;
        }
        return value;
    }

    // 更新角色晚上進度並寫入 PlayerPrefs
    public void IncrementNightProgress(string characterName)
    {
        int value = GetNightProgress(characterName) + 1;
        nightProgress[characterName] = value;
        PlayerPrefs.SetInt(characterName + NightKeySuffix, value);
        PlayerPrefs.Save();
    }

    // 清除指定角色的進度（除錯用）
    public void ResetProgress(string characterName)
    {
        dayProgress.Remove(characterName);
        nightProgress.Remove(characterName);
        PlayerPrefs.DeleteKey(characterName + DayKeySuffix);
        PlayerPrefs.DeleteKey(characterName + NightKeySuffix);
    }
}
