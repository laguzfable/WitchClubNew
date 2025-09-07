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
            Debug.Log("已清除所有 PlayerPrefs 儲存的事件進度");
        }
    }
}
