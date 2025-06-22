using UnityEngine;
using UnityEngine.UI;

public class MapBackgroundController : MonoBehaviour
{
    [Header("底圖（日/夜草地）")]
    public Image backgroundImage;
    public Sprite daySprite;
    public Sprite nightSprite;

    [Header("建築物圖")]
    public Image buildingImage;
    public Sprite buildingSprite;

    /// <summary>
    /// 切換日/夜 + 保持建築物顯示
    /// </summary>
    public void SetDayMode(bool isDay)
    {
        if (backgroundImage == null)
        {
            Debug.LogError("[MapBG] 背景圖未設定！");
            return;
        }

        backgroundImage.sprite = isDay ? daySprite : nightSprite;

        if (buildingImage != null && buildingSprite != null)
            buildingImage.sprite = buildingSprite; // 每次都確保建築物圖層蓋上去

        Debug.Log($"[MapBG] 已切換為 {(isDay ? "白天" : "夜晚")} 背景 + 建築物圖疊上去");
    }
}
