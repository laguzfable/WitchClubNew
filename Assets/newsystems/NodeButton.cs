using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

public class NodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    // 解鎖判定用 VisitKey，所以同一個 label 的多個變體節點會一起亮。
    // debugUnlockAll 是排版用的旁路；VisitedNodeManager 沒在場上時也視為未解鎖，不要 NRE。
    bool visited = data.alwaysUnlocked
                || (owner != null && owner.debugUnlockAll)
                || (VisitedNodeManager.Instance != null
                    && VisitedNodeManager.Instance.IsVisited(nodeId, label));

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


    // 未解鎖的節點在 Init 裡被關掉了 blocksRaycasts，收不到這兩個事件，
    // 所以還沒走過的劇情不會因為滑過去就被右頁劇透——這是刻意的。
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (map != null) map.ShowPreview(node);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (map != null) map.ClearPreview(node);
    }

    private void OnNodeClick()
    {
        Debug.Log($"[NodeButton] Click nodeId={nodeId}, script={scriptName}, label={label}");

        // 先讓星塵講一段，再問「這一輪你與誰最親近」，選完才真的進去。
        // 圖沒設定、或這一格沒勾 askAffinity 時，直接照舊進去。
        if (map != null && map.askAffinityBeforeEnter && node != null && node.askAffinity)
        {
            // 先把聖典收起來：星塵那段是走 Naninovel 的對話框和背景，
            // 書頁還開著的話會蓋在對話框上面。
            HideMapUI();
            AffinityChoicePanel.Show(BuildChoiceRequest(), Enter);
            return;
        }

        Enter(null);
    }

    /// <summary>
    /// 這一格真正能讓玩家選的角色。
    /// 被 variableOverrides 寫死的角色要拿掉——那個值最後一定會蓋回去，
    /// 留在面板上等於騙玩家（例：PARTING 強制 affinity_Ved=0，選了薇狄亞卻不會生效）。
    /// </summary>
    private System.Collections.Generic.List<AffinityChoiceOption> SelectableOptions()
    {
        var result = new System.Collections.Generic.List<AffinityChoiceOption>();
        if (map.affinityChoiceOptions == null) return result;

        foreach (var option in map.affinityChoiceOptions)
        {
            if (option == null || string.IsNullOrEmpty(option.variableName)) continue;

            var forced = false;
            if (node != null && node.variableOverrides != null)
                foreach (var preset in node.variableOverrides)
                    if (preset != null && preset.name == option.variableName) forced = true;

            if (forced)
            {
                Debug.Log($"[NodeButton] {option.variableName} 被這一格寫死了，不放進選人面板");
                continue;
            }

            result.Add(option);
        }

        return result;
    }

    /// <summary>收起標題選單與聖典書頁。</summary>
    private static void HideMapUI()
    {
        var uiManager = Engine.GetService<IUIManager>();
        if (uiManager == null) return;

        uiManager.GetUI<TitleMenu>()?.Hide();
        uiManager.GetUI<BranchMapUI>()?.Hide();
    }

    /// <summary>把 BranchMapUI 的共通設定和這一格自己的台詞組成一包。</summary>
    private AffinityChoicePanel.Request BuildChoiceRequest()
    {
        var lines = new System.Collections.Generic.List<string>();
        if (map.stardustLines != null) lines.AddRange(map.stardustLines);
        if (!string.IsNullOrEmpty(node.stardustLine)) lines.Add(node.stardustLine);

        return new AffinityChoicePanel.Request
        {
            Options = SelectableOptions(),
            Title = map.affinityChoiceTitle,
            SkipLabel = map.affinityChoiceSkipLabel,
            Lines = lines,
            SpeakerName = map.stardustSpeakerName,
            UseNaninovelDialogue = map.stardustUseNaninovelDialogue,
            Background = map.stardustBackground,
            CharacterId = map.stardustCharacterId
        };
    }

    /// <summary>真正進入節點。<paramref name="chosen"/> 是選人面板的結果，沒選就是 null。</summary>
    private void Enter(AffinityChoiceOption chosen)
    {

        // 清掉舊的返回點，避免 NaniScriptLoader_HEX 撿到之前留下的殘留值。
        // （scriptParameter 不用清，下面 GotoScript 會直接覆寫成這次的目標。）
        MapReturnPoint.Clear();

        // 特殊事件預約同理：從聖典跳節點等於換一條時間線，
        // 留著上一輪的預約會讓進去之後的地圖日接到別條線的劇情。
        MapSpecialOverride.ClearAll();

        // 1. 關閉 UI（選人面板那條路已經先關過了，重複呼叫沒有副作用）
        HideMapUI();

        // 2. 停止播放，避免舊指令干擾
        Engine.GetService<IScriptPlayer>()?.Stop();

        // 3. 進場前先把變數設好。Naninovel 的變數服務是跨場景常駐的，
        //    所以這裡設完之後，場景載入完仍然有效。
        var vars = Engine.GetService<ICustomVariableManager>();

        // 劇情地圖是全破後給玩家收結局用的機制，從這裡進入戰鬥時符文系統直接全開，
        // 不需要照劇情腳本原本的順序判斷（場景重載會讓 RuneActive 被重置成預設值 false）
        vars?.SetVariableValue("RuneActive", "True");

        // 好感度：@exitToTitle 會把它清成 0，不先補回去的話分歧點的選項會掉進同一個結局。
        //
        // 有兩套發法，看玩家這一格有沒有真的選人：
        //   ‧ 選了誰 → 只有她 100，其他人一律 0。規則單純到玩家一眼看得懂：
        //     「我選了誰，誰就是 100」。不能再混進走過／沒走過的判定——
        //     開得了聖典的人一定走過 chapter5yellow，涅莉會恆等於 100，
        //     那樣選她沒意義、不選她也照樣有她。
        //   ‧ 按了跳過（或這一格根本不問）→ 照舊用「走過那條線就發滿」的回溯判定。
        if (chosen != null)
            ApplyChosenAffinity(vars, chosen);
        else
            ApplyVariablePresets(vars);

        ApplyNodeOverrides(vars);

        // 名字也是被 @exitToTitle 清掉的東西之一。節點劇本不會重新問名字，
        // 不補的話台詞裡的 {PlayerName} 全都是空白。
        PlayerNameStore.RestoreIfMissing(vars);

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
    /// 選人面板的結果：選到的人發滿，其他所有候選人歸零。
    /// 歸零的名單同時取自 affinityPresets 和面板的選項，兩邊都掃過才不會漏
    /// （例如某個角色只出現在其中一邊）。
    /// </summary>
    private void ApplyChosenAffinity(ICustomVariableManager vars, AffinityChoiceOption chosen)
    {
        if (vars == null || map == null) return;

        var locked = map.lockedAffinityValue.ToString();
        var names = new System.Collections.Generic.HashSet<string>();

        if (map.affinityPresets != null)
            foreach (var preset in map.affinityPresets)
                if (preset != null && !string.IsNullOrEmpty(preset.variableName))
                    names.Add(preset.variableName);

        if (map.affinityChoiceOptions != null)
            foreach (var option in map.affinityChoiceOptions)
                if (option != null && !string.IsNullOrEmpty(option.variableName))
                    names.Add(option.variableName);

        foreach (var name in names)
            vars.SetVariableValue(name, locked);

        var value = map.defaultAffinityValue.ToString();
        vars.SetVariableValue(chosen.variableName, value);

        Debug.Log($"[NodeButton] 選人面板：{chosen.variableName}={value}，其餘 {names.Count - 1} 人歸 {locked}");
    }

    /// <summary>
    /// 依「玩家有沒有走過那條線」決定每個好感度。這是最底層的一道，
    /// 之後還會被選人面板和節點的 Variable Overrides 依序蓋過去。
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
    }

    /// <summary>
    /// 節點自己宣告的強制值（BranchNode.variableOverrides）。
    /// 這是最後一道，蓋過回溯判定和選人面板——它代表「這一格的劇情非這樣不可」，
    /// 例如第四章那格必須 affinity_Ved=0，否則三個選項不會出現、直接被推進綠線。
    /// </summary>
    private void ApplyNodeOverrides(ICustomVariableManager vars)
    {
        if (vars == null || node == null || node.variableOverrides == null) return;

        foreach (var preset in node.variableOverrides)
        {
            if (preset == null || string.IsNullOrEmpty(preset.name)) continue;
            vars.SetVariableValue(preset.name, preset.value.ToString());
            Debug.Log($"[NodeButton] 節點覆寫 {preset.name}={preset.value}");
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