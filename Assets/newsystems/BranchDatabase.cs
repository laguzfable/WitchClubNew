using UnityEngine;

// 這是小型的資料類別
[System.Serializable]
public class BranchNode
{
    public string nodeId;       // 對應 @visitNode id:xxx
    public string displayName;  // 按鈕上顯示的字
    public string scriptName;   // Naninovel 腳本檔名
    public string labelName;    // Label 名稱
}

// 這是 ScriptableObject
[CreateAssetMenu(menuName = "BranchMap/BranchDatabase")]
public class BranchDatabase : ScriptableObject
{
    public BranchNode[] nodes;
}