using UnityEngine;
using Naninovel;
using Naninovel.UI; // 必須引用

[RequireComponent(typeof(CanvasGroup))]
public class BranchMapUI : CustomUI
{
    public BranchDatabase database;
    public Transform nodeContainer;
    public GameObject nodeButtonPrefab;

    // 覆寫顯示方法：每次打開選單時，重新生成節點狀態
    public override async UniTask ChangeVisibilityAsync(bool visible, float? duration = null, AsyncToken asyncToken = default)
    {
        // 如果是要打開選單，先刷新內容
        if (visible)
        {
            GenerateNodes();
        }

        // 執行原本 Nani 的淡入淡出
        await base.ChangeVisibilityAsync(visible, duration, asyncToken);
    }

   private void GenerateNodes()
{
    Debug.Log("=== [BranchMapUI] GenerateNodes() START ===");

    // 1. 檢查 database
    if (database == null)
    {
        Debug.LogError("❌ database 為 NULL！");
        return;
    }
    else
    {
        Debug.Log($"✔ database 存在，共有 {database.nodes?.Length} 個節點");
    }

    // 2. 檢查 prefab
    if (nodeButtonPrefab == null)
    {
        Debug.LogError("❌ nodeButtonPrefab 為 NULL！");
        return;
    }
    else
    {
        Debug.Log("✔ nodeButtonPrefab 存在");
    }

    // 3. 清除舊的按鈕
    Debug.Log($"清除舊按鈕，共 {nodeContainer.childCount} 個");
    for (int i = nodeContainer.childCount - 1; i >= 0; i--)
    {
        Destroy(nodeContainer.GetChild(i).gameObject);
    }

    // 4. 開始產生節點
    if (database.nodes == null || database.nodes.Length == 0)
    {
        Debug.LogWarning("⚠ database.nodes 是空的 → 不會生成任何按鈕");
        return;
    }

    int index = 0;
    foreach (var node in database.nodes)
    {
        Debug.Log($"--- 產生節點[{index}] ---");
        Debug.Log($"nodeId: {node.nodeId}");
        Debug.Log($"displayName: {node.displayName}");
        Debug.Log($"scriptName: {node.scriptName}");
        Debug.Log($"labelName: {node.labelName}");

        // 基礎資料檢查
        if (string.IsNullOrEmpty(node.nodeId))
            Debug.LogWarning("⚠ nodeId 空的！");
        if (string.IsNullOrEmpty(node.displayName))
            Debug.LogWarning("⚠ displayName 空的！");
        if (string.IsNullOrEmpty(node.scriptName))
            Debug.LogWarning("⚠ scriptName 空的（跳不了劇本）！");
        if (string.IsNullOrEmpty(node.labelName))
            Debug.LogWarning("⚠ labelName 空的（跳不了 label）！");

        // 嘗試建立
        GameObject newObj = Instantiate(nodeButtonPrefab, nodeContainer);
        Debug.Log($"✔ 成功 Instantiate NodeButton → {newObj}");

        var btnScript = newObj.GetComponent<NodeButton>();
        if (btnScript == null)
        {
            Debug.LogError("❌ NodeButton.cs 沒掛在 prefab 上！？");
        }
        else
        {
            Debug.Log("✔ NodeButton.cs 存在，呼叫 Init()");
            btnScript.Init(node);
        }

        index++;
    }

    Debug.Log("=== [BranchMapUI] GenerateNodes() END ===");
}

}