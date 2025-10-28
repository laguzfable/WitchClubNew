using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Naninovel;

/// <summary>
/// 單張符文卡控制腳本（含遮罩）
/// 支援舊版 Unity / Naninovel
/// </summary>
public class RuneSystem : MonoBehaviour
{
    [Header("核心屬性")]
    public string abilityID; // 對應能力 ID
    public Button btn;       // 卡片按鈕

    [Header("解鎖遮罩設定")]
    [SerializeField] GameObject lockOverlay;   // 遮罩物件
    [SerializeField] Image lockIcon;           // 鎖頭圖示
    [SerializeField] CanvasGroup overlayGroup; // 控制透明度（可選）

    DataService dataService;
    object abilityData; // 不指定類型，避免舊結構編譯錯誤

    void Awake()
    {
        dataService = DataService.Instance;
        if (btn == null) btn = GetComponent<Button>();

        // 嘗試讀取能力資料（忽略類型）
        if (dataService != null)
            abilityData = dataService.GetAbilityById(abilityID);
    }

    /// <summary>
    /// 控制符文卡是否可互動，以及是否顯示遮罩
    /// </summary>
    public void SetInteractable(bool canUse)
    {
        // 控制按鈕是否可點
        if (btn != null)
            btn.interactable = canUse;

        // 顯示或隱藏遮罩物件
        if (lockOverlay != null)
            lockOverlay.SetActive(!canUse);

        // 若有 CanvasGroup，控制半透明效果
        if (overlayGroup != null)
            overlayGroup.alpha = canUse ? 0f : 0.6f;

        // 鎖頭圖示顯示
        if (lockIcon != null)
            lockIcon.enabled = !canUse;
    }
}
