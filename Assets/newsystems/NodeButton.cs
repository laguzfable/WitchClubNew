using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

public class NodeButton : MonoBehaviour
{
    public Text labelText;

    private string nodeId;
    private string scriptName;

    private Button btn;
    private CanvasGroup cg;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (!btn) btn = gameObject.AddComponent<Button>();

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        var img = GetComponent<Image>();

        Debug.Log($"[NodeButton Awake] name = {gameObject.name}, layer = {gameObject.layer}, " +
                  $"imageRaycast = {img?.raycastTarget}, " +
                  $"buttonInteractable = {btn.interactable}, " +
                  $"cgInteractable = {cg.interactable}");
    }

    public void Init(BranchNode data)
    {
        this.nodeId = data.nodeId;
        this.scriptName = data.scriptName;

        if (labelText)
            labelText.text = data.displayName;

        bool visited = VisitedNodeManager.Instance.IsVisited(nodeId);

        // 看過亮、沒看過灰，但全部能按
        cg.alpha = visited ? 1f : 0.5f;

        btn.interactable = true;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnNodeClick);

        var img = GetComponent<Image>();
        if (img) img.raycastTarget = true;

        Debug.Log($"[RaycastCheck] NodeButton {scriptName} raycastTarget = {img?.raycastTarget}");
    }

    /// <summary>
    /// 這裡的過場邏輯，模仿 TitleNewGameButton：
    /// 1. 關掉場景中所有 Title / Canvas 類 root 物件
    /// 2. 從 UIManager 拿到 BranchMapUI，直接 Destroy（連帶所有子物件）
    /// 3. 隱藏 ITitleUI
    /// 4. Reset ScriptPlayer 服務
    /// 5. ResetStateAsync 後，在 callback 裡 PreloadAndPlayAsync(scriptName)
    /// </summary>
    private void OnNodeClick()
    {
        Debug.Log($"[NodeButton] Click nodeId={nodeId}, script={scriptName}");

        // 先拿到 Nani 服務
        var uiManager    = Engine.GetService<IUIManager>();
        var stateManager = Engine.GetService<IStateManager>();
        var scriptPlayer = Engine.GetService<IScriptPlayer>();

        // ================================
        // 1) 關掉場景中所有 Title / Canvas root 物件
        //    （完全模仿 TitleNewGameButton 的掃描方式）
        // ================================
        var activeScene = SceneManager.GetActiveScene();
        var roots       = activeScene.GetRootGameObjects();

        foreach (var obj in roots)
        {
            if (obj == null) continue;

            if (obj.name.Contains("Title") || obj.name.Contains("Canvas"))
            {
                Debug.Log("[NodeButton] Deactivate root object: " + obj.name);
                obj.SetActive(false);
            }
        }

        // ================================
        // 2) 處理 BranchMapUI：不只 Hide，而是整個 Destroy 掉
        // ================================
        if (uiManager != null)
        {
            // 先用型別抓 BranchMapUI（你已經用 CustomUI 繼承了）
            var branchMap = uiManager.GetUI<BranchMapUI>();
            if (branchMap != null)
            {
                Debug.Log("[NodeButton] Destroy BranchMapUI (含所有子物件).");

                // 這個 gameObject 底下就是你所有 nodeButton 的實體
                var branchGO = (branchMap as MonoBehaviour).gameObject;
                Object.Destroy(branchGO);
            }
            else
            {
                Debug.Log("[NodeButton] BranchMapUI not found in IUIManager, skip Destroy.");
            }

            // ================================
            // 3) 處理 ITitleUI：跟你之前寫的一樣，Hide 掉
            // ================================
            var titleUI = uiManager.GetUI<ITitleUI>();
            if (titleUI != null)
            {
                Debug.Log("[NodeButton] Hide ITitleUI.");
                titleUI.Hide();
            }
            else
            {
                Debug.Log("[NodeButton] ITitleUI not found, skip Hide.");
            }
        }
        else
        {
            Debug.LogWarning("[NodeButton] IUIManager service is NULL.");
        }

        // ================================
        // 4) Reset ScriptPlayer 服務
        //    （模仿 TitleNewGameButton 的 ResetService）
        // ================================
        if (scriptPlayer != null)
        {
            Debug.Log("[NodeButton] Reset ScriptPlayer service.");
            scriptPlayer.ResetService();
        }
        else
        {
            Debug.LogWarning("[NodeButton] IScriptPlayer service is NULL.");
        }

        // ================================
        // 5) ResetStateAsync → callback 裡啟動該按鈕的 scriptName
        //    簡化版：不帶 exclude，全部重置
        // ================================
        if (stateManager != null && scriptPlayer != null)
        {
            Debug.Log("[NodeButton] Call ResetStateAsync, then start script: " + scriptName);

            // Naninovel 1.17：ResetStateAsync(string[] exclude = null, Action onCompleted = null)
string[] exclude = new string[0];

stateManager.ResetStateAsync(exclude, async () =>
{
    Debug.Log("[NodeButton] Reset complete → Play: " + scriptName);
    await scriptPlayer.PreloadAndPlayAsync(scriptName);
});


        }
        else
        {
            // 後備方案：若 stateManager 為空，就直接跳腳本（至少不會卡死）
            Debug.LogWarning("[NodeButton] StateManager or ScriptPlayer missing, fallback to direct PreloadAndPlayAsync.");

            if (scriptPlayer != null)
                scriptPlayer.PreloadAndPlayAsync(scriptName);
        }
    }
}
