using UnityEngine;

public class ProgressResetter : MonoBehaviour
{
    // 可在 Inspector 修改按鍵
    [SerializeField] KeyCode resetKey = KeyCode.F10;

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            PlayerPrefs.DeleteAll(); // 或 StoryProgressManager.Instance.ResetProgress("角色代號");
            Hexe.UI.MonsterCodex.InvalidateCache(); // 圖鑑解鎖紀錄有快取，要一起丟掉
            EndingRecord.Clear(); // 結局紀錄也有快取，不清的話 DeleteAll 之後還是會判定成「拿過結局」
            Debug.Log("已清除所有 PlayerPrefs 儲存的事件進度");
        }
    }
}
