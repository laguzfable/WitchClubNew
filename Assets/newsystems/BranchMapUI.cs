using UnityEngine;
using UnityEngine.UI;
using Naninovel;
using Naninovel.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BranchMapUI : CustomUI
{
    public BranchDatabase database;
    public Transform nodeContainer;
    public GameObject nodeButtonPrefab;

    // 🔍 追蹤 Naninovel 在淡入時偷偷改 CanvasGroup
    void OnCanvasGroupChanged()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg)
        {
            Debug.Log($"[BranchMapUI] CanvasGroup Changed → " +
                      $"interactable={cg.interactable}, " +
                      $"blocksRaycasts={cg.blocksRaycasts}, " +
                      $"alpha={cg.alpha}");
        }
        else
        {
            Debug.LogWarning("[BranchMapUI] 沒有 CanvasGroup");
        }
    }

public override async UniTask ChangeVisibilityAsync(bool visible, float? duration = null, AsyncToken asyncToken = default)
{
    if (visible)
        GenerateNodes();

    // 讓 Naninovel 自己處理 fade
    await base.ChangeVisibilityAsync(visible, duration, asyncToken);

    // === 修正 CanvasGroup 狀態 ===
    var cg = GetComponent<CanvasGroup>();
    if (!cg) return;

    if (visible)
    {
        // 打開：確保能點
        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.alpha = 1f;
    }
    else
    {
        // 關閉：避免透明牆
        cg.interactable = false;
        cg.blocksRaycasts = false;
        // alpha 在這裡不用管，Naninovel 會 fade 到 0
    }
}



    private void GenerateNodes()
    {
        Debug.Log("=== [BranchMapUI] GenerateNodes() START ===");

        if (database == null)
        {
            Debug.LogError("❌ database 為 NULL！");
            return;
        }
        else Debug.Log($"✔ database 存在，共有 {database.nodes?.Length} 個節點");

        if (nodeButtonPrefab == null)
        {
            Debug.LogError("❌ nodeButtonPrefab 為 NULL！");
            return;
        }
        else Debug.Log("✔ nodeButtonPrefab 存在");

        Debug.Log($"清除舊按鈕，共 {nodeContainer.childCount} 個");
        for (int i = nodeContainer.childCount - 1; i >= 0; i--)
            Destroy(nodeContainer.GetChild(i).gameObject);

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

            if (string.IsNullOrEmpty(node.nodeId))
                Debug.LogWarning("⚠ nodeId 空的！");
            if (string.IsNullOrEmpty(node.displayName))
                Debug.LogWarning("⚠ displayName 空的！");
            if (string.IsNullOrEmpty(node.scriptName))
                Debug.LogWarning("⚠ scriptName 空的（跳不了劇本）！");

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
