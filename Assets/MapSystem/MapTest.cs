using UnityEngine;

public class MapTest : MonoBehaviour
{
    void Start()
    {
        // 直接從 PlayerPrefs 讀，預設 1=白天
        bool isDay = PlayerPrefs.GetInt("MapIsDay", 1) == 1;

        Debug.Log("[MapTest] 啟動 MapTest，isDay=" + isDay);

        var bg = FindObjectOfType<MapBackgroundController>();
        if (bg != null)
            bg.SetDayMode(isDay);
        else
            Debug.LogWarning("找不到 MapBackgroundController！");
    }
}
