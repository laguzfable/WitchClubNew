using UnityEngine;

// 這是小型的資料類別
[System.Serializable]
public class BranchNode
{
    public string nodeId;       // @visitNode 對應ID
    public string displayName;  // UI上顯示文字
    public string scriptName;   // Naninovel 腳本名稱

    public string label;        // ⭐ 新增：可選擇跳到腳本內的某個 label（不填＝從頭開始）
}


// 這是 ScriptableObject
[CreateAssetMenu(menuName = "BranchMap/BranchDatabase")]
public class BranchDatabase : ScriptableObject
{
    public BranchNode[] nodes;
}