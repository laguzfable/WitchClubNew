using System.Collections.Generic;
using UnityEngine;

// 這是一個最基本的 Singleton（單例），確保全遊戲只有一個這個管理器
public class StoryProgressManager : MonoBehaviour
{
    public static StoryProgressManager Instance { get; private set; }

    // 每個角色的白天進度
    private Dictionary<string, int> dayProgress = new Dictionary<string, int>();

    // 每個角色的晚上進度
    private Dictionary<string, int> nightProgress = new Dictionary<string, int>();

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
        if (dayProgress.TryGetValue(characterName, out int value))
            return value;
        return 0;
    }

    // 更新角色白天進度
    public void IncrementDayProgress(string characterName)
    {
        if (!dayProgress.ContainsKey(characterName))
            dayProgress[characterName] = 1;
        else
            dayProgress[characterName]++;
    }

    // 查詢角色晚上進度
    public int GetNightProgress(string characterName)
    {
        if (nightProgress.TryGetValue(characterName, out int value))
            return value;
        return 0;
    }

    // 更新角色晚上進度
    public void IncrementNightProgress(string characterName)
    {
        if (!nightProgress.ContainsKey(characterName))
            nightProgress[characterName] = 1;
        else
            nightProgress[characterName]++;
    }
}
