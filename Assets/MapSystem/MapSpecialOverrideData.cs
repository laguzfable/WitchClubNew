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
    public Vector2 offset;                        // 位置偏移
}
