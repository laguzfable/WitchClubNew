using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

public class NodeButton : MonoBehaviour
{
    public Text labelText;

    [Tooltip("顯示節點圖的 Image。留空會自動抓本物件上的 Image（也就是 Button 的底圖）")]
    public Image iconImage;

    private string nodeId;
    private string scriptName;
    private string label;

    private BranchNode node;
    private BranchMapUI map;

    private Button btn;
    private CanvasGroup cg;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (!btn) btn = gameObject.AddComponent<Button>();

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        if (!iconImage) iconImage = GetComponent<Image>();
    }

public void Init(BranchNode data, BranchMapUI owner)
{
    node = data;
    map = owner;

    nodeId = data.nodeId;
    scriptName = data.scriptName;
    label = data.label;

    if (labelText)
        labelText.text = data.displayName;

    // 解鎖判定用 VisitKey，所以同一個 label 的多個變體節點會一起亮
    bool visited = VisitedNodeManager.Instance.IsVisited(nodeId, label);

    var lockedIcon = owner ? owner.lockedIcon : null;
    var lockedAlpha = owner ? owner.lockedAlpha : 0.4f;
    var preserveAspect = owner == null || owner.preserveAspect;

    // 走過了才給看真正的圖；還沒走過一律換成共用的未解鎖圖，免得先劇透結局
    if (iconImage)
    {
        var sprite = visited ? data.icon : (lockedIcon ? lockedIcon : data.icon);
        if (sprite) iconImage.sprite = sprite;   // 兩邊都沒指定就保留 prefab 原本的底圖
        iconImage.preserveAspect = preserveAspect;
    }

    if (visited)
    {
        cg.alpha = 1f;
        btn.interactable = true;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
    else
    {
        // 有未解鎖圖的話就靠那張圖表達鎖住的狀態，不必再調暗
        cg.alpha = lockedIcon ? lockedAlpha : 0.4f;
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

        // 清掉舊的返回點，避免 NaniScriptLoader_HEX 撿到之前留下的殘留值。
        // （scriptParameter 不用清，下面 GotoScript 會直接覆寫成這次的目標。）
        MapReturnPoint.Clear();

        // 1. 關閉 UI
        var uiManager = Engine.GetService<IUIManager>();
        if (uiManager != null)
        {
            uiManager.GetUI<TitleMenu>()?.Hide();
            uiManager.GetUI<BranchMapUI>()?.Hide();
        }

        // 2. 停止播放，避免舊指令干擾
        Engine.GetService<IScriptPlayer>()?.Stop();

        // 3. 進場前先把變數設好。Naninovel 的變數服務是跨場景常駐的，
        //    所以這裡設完之後，場景載入完仍然有效。
        var vars = Engine.GetService<ICustomVariableManager>();

        // 劇情地圖是全破後給玩家收結局用的機制，從這裡進入戰鬥時符文系統直接全開，
        // 不需要照劇情腳本原本的順序判斷（場景重載會讓 RuneActive 被重置成預設值 false）
        vars?.SetVariableValue("RuneActive", "True");

        // 好感度同理：它是「讓玩家走到這個節點」的門票，走過那條線就代表當初驗過了。
        // @exitToTitle 會把好感度清成 0，不先補回去的話，分歧點的兩個選項會掉進同一個結局。
        ApplyVariablePresets(vars);

        vars?.SetVariableValue("NextScript", scriptName);
        vars?.SetVariableValue("NextLabel", string.IsNullOrEmpty(label) ? "" : label);

        // 4. 交給 SceneLoader 這條正規管道：它會設好 ExplicitGotoPending 和
        //    scriptParameter 再切場景，然後由 NaniScriptLoader_HEX 統一負責播放。
        //
        //    以前這裡是自己 SceneManager.LoadScene + sceneLoaded 事件 + PreloadAndPlayAsync，
        //    等於跟 NaniScriptLoader_HEX.Start() 兩個人搶著決定要播哪一段，誰後跑誰贏。
        //    症狀就是「不同節點點進去卻播到同一段」，而且時好時壞。
        //    RestButton 和 MapEventManager 本來就都走 GotoScript，這裡跟它們一致。
        var loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("[NodeButton] 找不到 SceneLoader，無法跳轉");
            return;
        }

        loader.GotoScript(scriptName, string.IsNullOrEmpty(label) ? null : label);
    }

    /// <summary>
    /// 先依「玩家有沒有走過那條線」決定每個好感度，再套用這個節點自己的例外。
    /// </summary>
    private void ApplyVariablePresets(ICustomVariableManager vars)
    {
        if (vars == null) return;

        if (map != null && map.affinityPresets != null)
        {
            var summary = "";
            foreach (var preset in map.affinityPresets)
            {
                if (preset == null || string.IsNullOrEmpty(preset.variableName)) continue;

                // 走過那條線才算「跟她好過」。沒走過就維持 0——否則玩家明明只跑過別人的線，
                // 從地圖進第四章卻會被薇狄亞攔下來說「我一直都在這裡」，講不通。
                var earned = string.IsNullOrEmpty(preset.requireVisited)
                          || HasVisited(preset.requireVisited);

                var value = earned ? map.defaultAffinityValue : map.lockedAffinityValue;
                vars.SetVariableValue(preset.variableName, value.ToString());

                summary += $"{preset.variableName}={value}"
                         + (earned ? " " : $"(沒走過 {preset.requireVisited}) ");
            }

            Debug.Log($"[NodeButton] 回溯好感度 → {summary}");
        }

        if (node != null && node.variableOverrides != null)
        {
            foreach (var preset in node.variableOverrides)
            {
                if (preset == null || string.IsNullOrEmpty(preset.name)) continue;
                vars.SetVariableValue(preset.name, preset.value.ToString());
                Debug.Log($"[NodeButton] 節點覆寫 {preset.name}={preset.value}");
            }
        }
    }

    /// <summary>「nodeId」或「nodeId#label」寫法的解鎖查詢。</summary>
    private static bool HasVisited(string key)
    {
        if (VisitedNodeManager.Instance == null || string.IsNullOrEmpty(key)) return false;

        var sep = key.IndexOf('#');
        return sep < 0
            ? VisitedNodeManager.Instance.IsVisited(key)
            : VisitedNodeManager.Instance.IsVisited(key.Substring(0, sep), key.Substring(sep + 1));
    }

}