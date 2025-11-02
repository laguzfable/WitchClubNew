using UnityEngine;

public class RuneUIManager : MonoBehaviour
{
    [Header("Database")]
    public RuneDatabase database;
    public int defaultUnlockedCount = 4;

    [Header("Prefab & Containers")]
    public GameObject cardPrefab;
    public Transform blueContainer;
    public Transform redContainer;
    public Transform greenContainer;
    public Transform yellowContainer;

    private void Start()
    {
        GenerateCards();
    }

    private Transform GetContainer(RuneType type)
    {
        switch (type)
        {
            case RuneType.Blue:   return blueContainer;
            case RuneType.Red:    return redContainer;
            case RuneType.Green:  return greenContainer;
            case RuneType.Yellow: return yellowContainer;
            default:              return blueContainer;
        }
    }

    private void GenerateCards()
    {
        if (!database)   { Debug.LogError("[RuneUI] ❌ RuneDatabase 未綁定!"); return; }
        if (!cardPrefab) { Debug.LogError("[RuneUI] ❌ cardPrefab 未綁定!");   return; }

        for (int i = 0; i < database.runes.Count; i++)
        {
            var data = database.GetRune(i);
            if (data == null)
            {
                Debug.LogError($"[RuneUI] ❌ 符文資料 index={i} 是 null!");
                continue;
            }

            Transform parent = GetContainer(data.type);
            var go = Instantiate(cardPrefab, parent);

            var card = go.GetComponent<SelectRuneCard>();
            if (!card)
            {
                Debug.LogError("[RuneUI] ❌ Prefab 缺 SelectRuneCard 組件!");
                continue;
            }

            bool unlocked = i < defaultUnlockedCount;
            card.Setup(i, data, unlocked);

            Debug.Log($"[RuneUI] ✅ Spawn index={i}, type={data.type}, unlocked={unlocked}");
        }

        Debug.Log("[RuneUI] ✅ 全部卡片建立完成!");
    }

    public void OnRuneClicked(int index)
    {
        Debug.Log($"[RuneUI] 📌 玩家點擊符文 index={index}");
    }
}
