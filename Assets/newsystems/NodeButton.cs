using UnityEngine;
using UnityEngine.UI;
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
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        Debug.Log($"[NodeButton Awake] layer = {gameObject.layer}, " +
          $"imageRaycast = {GetComponent<Image>().raycastTarget}, " +
          $"buttonInteractable = {GetComponent<Button>().interactable}, " +
          $"cgInteractable = {GetComponent<CanvasGroup>()?.interactable}");

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
    btn.onClick.AddListener(() => OnNodeClick());

    GetComponent<Image>().raycastTarget = true;
Debug.Log($"[RaycastCheck] NodeButton {scriptName} raycastTarget = " +
    GetComponent<Image>().raycastTarget);


}

    private async void OnNodeClick()
    {
        var uiManager = Engine.GetService<IUIManager>();

        var branchMap = uiManager.GetUI<BranchMapUI>();
        if (branchMap != null) branchMap.Hide();

        var titleUI = uiManager.GetUI<ITitleUI>();
        if (titleUI != null) titleUI.Hide();

        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();

        var player = Engine.GetService<IScriptPlayer>();

        // 🔥 無論如何，永遠從腳本開頭跳
        Debug.Log($"Jumping to script: {scriptName}");
        await player.PreloadAndPlayAsync(scriptName);
    }
}
