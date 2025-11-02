using UnityEngine;

public class RuneUnlockManager : MonoBehaviour
{
    public int defaultUnlockedCount = 4;

    private void Start()
    {
        Debug.Log($"[RuneUnlock] 預設解鎖 {defaultUnlockedCount} 個符文");
    }

    public bool IsRuneUnlocked(int index)
    {
        bool unlocked = index < defaultUnlockedCount;

        Debug.Log($"[RuneUnlock] Index={index}, unlocked={unlocked}");
        return unlocked;
    }
}
