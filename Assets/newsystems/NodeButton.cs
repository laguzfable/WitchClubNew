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
    private string label;

    private Button btn;
    private CanvasGroup cg;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (!btn) btn = gameObject.AddComponent<Button>();

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

public void Init(BranchNode data)
{
    nodeId = data.nodeId;
    scriptName = data.scriptName;
    label = data.label;

    if (labelText)
        labelText.text = data.displayName;

    bool visited = VisitedNodeManager.Instance.IsVisited(nodeId, label);

    if (visited)
    {
        cg.alpha = 1f;
        btn.interactable = true;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
    else
    {
        cg.alpha = 0.4f;
        btn.interactable = false;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    btn.onClick.RemoveAllListeners();
    if (visited)
        btn.onClick.AddListener(OnNodeClick);
}


    private void OnNodeClick()
    {
        Debug.Log($"[NodeButton] Click nodeId={nodeId}, script={scriptName}, label={label}");

        // ============================================================
        // 1) 設定 NextScript / NextLabel
        //    Naninovel v1.17 “正統標籤跳轉方式”
        // ============================================================
        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars != null)
        {
            vars.SetVariableValue("NextScript", scriptName);
            vars.SetVariableValue("NextLabel", string.IsNullOrEmpty(label) ? "" : label);

            Debug.Log($"[NodeButton] Set NextScript={scriptName}, NextLabel={label}");
        }
        else
            Debug.LogWarning("[NodeButton] 找不到 ICustomVariableManager");


        // ============================================================
        // 2) 徹底關閉 TitleMenu 與 BranchMapUI
        // ============================================================
        var uiManager = Engine.GetService<IUIManager>();
        if (uiManager != null)
        {
            // --- 關掉 TitleMenu ---
            var titleMenu = uiManager.GetUI<TitleMenu>();
            if (titleMenu != null)
            {
                Debug.Log("[NodeButton] Hide TitleMenu");
                titleMenu.Hide();
            }

            // --- Destroy BranchMapUI prefab ---
            var mapUI = uiManager.GetUI<BranchMapUI>();
            if (mapUI != null)
            {
                Debug.Log("[NodeButton] Destroy BranchMapUI");
                Object.Destroy((mapUI as MonoBehaviour).gameObject);
            }
        }
        else
            Debug.LogWarning("[NodeButton] uiManager is null");


        // ============================================================
        // 3) Reset ScriptPlayer（必要，不然會出問題）
        // ============================================================
        var player = Engine.GetService<IScriptPlayer>();
        if (player != null)
        {
            Debug.Log("[NodeButton] Reset ScriptPlayer");
            player.ResetService();
        }


        // ============================================================
        // 4) 回到小說場景 → Naninovel 會自動讀取 NextScript#NextLabel
        // ============================================================
        try
        {
            Debug.Log("[NodeButton] Go back to NaniDialogTest");
            NaniBridgeUtility.GoBackToSavedStory("NaniDialogTest");
        }
        catch
        {
            Debug.LogWarning("[NodeButton] FallBack LoadScene");
            SceneManager.LoadScene("NaniDialogTest");
        }
    }
}
