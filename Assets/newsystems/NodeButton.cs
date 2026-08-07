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


private async void OnNodeClick()
    {
        Debug.Log($"[NodeButton] Click nodeId={nodeId}, script={scriptName}, label={label}");

        // 清掉舊的返回點/一次性參數，避免場景載入時 NaniScriptLoader_HEX
        // 撿到之前測試留下的 MapReturnPoint/scriptParameter，蓋掉這裡指定的目標章節。
        MapReturnPoint.Clear();
        if (DataService.Instance != null)
            DataService.Instance.scriptParameter = null;

        // 1. 關閉 UI (保持不變)
        var uiManager = Engine.GetService<IUIManager>();
        if (uiManager != null)
        {
            uiManager.GetUI<TitleMenu>()?.Hide();
            uiManager.GetUI<BranchMapUI>()?.Hide();
        }

        // 2. 準備切換
        var player = Engine.GetService<IScriptPlayer>();
        var stateManager = Engine.GetService<IStateManager>();
        
        // 停止播放，避免舊指令干擾
        player?.Stop(); 

        // 3. 設定變數 (保持相容性，以防其他系統需要)
        var vars = Engine.GetService<ICustomVariableManager>();
        vars?.SetVariableValue("NextScript", scriptName);
        vars?.SetVariableValue("NextLabel", string.IsNullOrEmpty(label) ? "" : label);

        // =========================================================
        // 重點修正：如何過場
        // =========================================================

        string targetScene = "NaniDialogTest";
        
        // 如果當前已經是小說場景，直接播放
        if (SceneManager.GetActiveScene().name == targetScene)
        {
            await LoadAndPlaySafe(player, scriptName, label);
        }
        else
        {
            // 如果需要切換場景，我們不要依賴場景本身的 Auto-Start 腳本
            // 而是掛載一個事件，等場景載入完後，由我們這邊發動播放
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(targetScene);
        }
    }

    // 場景載入完成的回調
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "NaniDialogTest")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 移除監聽，避免重複執行
            
            // 延遲一點點執行，確保 Naninovel 引擎初始化完畢
            LoadAfterSceneInit().Forget(); // 這裡需要引入 UniTask 或使用 Coroutine
        }
    }

    private async UniTaskVoid LoadAfterSceneInit()
    {
        // 等待一幀，讓 Engine 準備好
        await UniTask.Yield(); 

        var player = Engine.GetService<IScriptPlayer>();
        
        // 呼叫安全播放
        await LoadAndPlaySafe(player, scriptName, label);
    }

    // ★★★ 核心解決方案：安全播放方法 ★★★
    private async UniTask LoadAndPlaySafe(IScriptPlayer player, string scriptName, string label)
    {
        // 劇情地圖是全破後給玩家收結局用的機制，從這裡進入戰鬥時符文系統直接全開，
        // 不需要照劇情腳本原本的順序判斷（場景重載會讓 RuneActive 被重置成預設值 false）
        var vars = Engine.GetService<ICustomVariableManager>();
        vars?.SetVariableValue("RuneActive", "True");

        // 1. 先預載腳本
        await player.PreloadAndPlayAsync(scriptName, label: label);

        // 2. ★ 檢測是否卡在空行 ★
        // 如果播放清單是空的 (代表該 Label 下面沒東西)，手動停止它，防止當機
        if (player.Playlist == null || player.Playlist.Count == 0)
        {
            Debug.LogWarning($"[NodeButton] 檢測到 {scriptName}#{label} 是空標籤，強制停止以防當機！");
            player.Stop();
        }
    }
}