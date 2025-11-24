using UnityEngine;
using UnityEngine.UI;
using Naninovel;
using Naninovel.UI; 
// 注意：如果你發現 UniTask 報錯，可能需要引用 Cysharp.Threading.Tasks;
// 但通常 Naninovel namespace 裡已經包好了。

public class NodeButton : MonoBehaviour
{
    public Text labelText;

    private string nodeId;
    private string scriptName;
    private string labelName;
    
    private Button btn;
    private CanvasGroup cg;

    void Awake()
    {
        btn = GetComponent<Button>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(BranchNode data)
    {
        this.nodeId = data.nodeId;
        this.scriptName = data.scriptName;
        this.labelName = data.labelName;

        if (labelText) labelText.text = data.displayName;

        // 判斷是否看過
        bool visited = VisitedNodeManager.Instance.IsVisited(nodeId);

        // 沒看過變半透明且不能點 (依你需求調整)
        cg.alpha = visited ? 1f : 0.5f;
        btn.interactable = visited;

        // 綁定點擊事件
        btn.onClick.RemoveAllListeners();
        // 使用 lambda 呼叫 async 方法
        btn.onClick.AddListener(() => OnNodeClick());
    }

    private async void OnNodeClick()
    {
        // 1. 關閉所有 UI (包含 Title 和 BranchMap)
        var uiManager = Engine.GetService<IUIManager>();
        
        // 取得 BranchMapUI 並隱藏 (確保類別名稱跟下一個腳本一致)
        var branchMap = uiManager.GetUI<BranchMapUI>();
        if (branchMap != null) branchMap.Hide();

        // 隱藏標題畫面
        var titleUI = uiManager.GetUI<ITitleUI>();
        if (titleUI != null) titleUI.Hide();

        // 2. 重置遊戲狀態 (重要！避免殘留變數)
        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();

        // 3. 載入並播放
        var player = Engine.GetService<IScriptPlayer>();
        Debug.Log($"Jumping to {scriptName} #{labelName}");
        
        // 這裡不使用 .Forget() 因為這是 async void，
        // 且 LoadAndPlayAsync 在舊版可能會有不同的行為，PreloadAndPlayAsync 最穩
        await player.PreloadAndPlayAsync(scriptName, label: labelName);
    }
}