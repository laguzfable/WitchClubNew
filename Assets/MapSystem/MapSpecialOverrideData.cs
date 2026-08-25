using UnityEngine;

/// <summary>
/// 單一特殊事件資料。
/// </summary>
[System.Serializable]
public class MapSpecialOverrideData
{
    public string characterName;                  // 角色名稱
    public string eventName;                      // 事件名稱
    public string naninovelScript;                // 要跳的劇本
    public RuntimeAnimatorController animator;    // 動畫（可空）

    // 動畫的 Resources 路徑。animator 本身是資產參照，存不進 PlayerPrefs，
    // 所以另外記路徑，讀回預約時再 Resources.Load 一次（見 MapSpecialOverride）。
    public string animatorPath;
    public Vector2 offset;                        // 位置偏移
}
