using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Naninovel;
using Naninovel.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BranchMapUI : CustomUI
{
    public BranchDatabase database;
    [Tooltip("樹狀圖的可視範圍。執行時會被改造成 ScrollRect 的 Viewport")]
    public Transform nodeContainer;
    public GameObject nodeButtonPrefab;

    [Header("樹狀排版")]
    [Tooltip("節點尺寸。原本是由 GridLayoutGroup 的 Cell Size 撐開的，改成樹狀後要自己指定")]
    public Vector2 nodeSize = new Vector2(240f, 170f);
    [Tooltip("同一層相鄰節點的水平間距（含節點寬度）")]
    public float stepX = 270f;
    [Tooltip("上下層之間的垂直間距（含節點高度）")]
    public float stepY = 210f;
    [Tooltip("整棵樹的縮放。1 = 原尺寸；要更大就往上調")]
    public float treeScale = 1f;
    [Tooltip("樹的四周留白，避免節點貼齊邊緣")]
    public float padding = 60f;

    [Header("捲動")]
    [Tooltip("關掉的話會變回固定版面（整棵樹自動縮小塞進容器）")]
    public bool enableScroll = true;
    [Tooltip("滑鼠滾輪的靈敏度")]
    public float scrollSensitivity = 45f;

    [Header("節點圖")]
    [Tooltip("未解鎖節點共用的圖（???／鎖頭）。留空＝沿用舊行為，直接把節點原本的圖調暗")]
    public Sprite lockedIcon;
    [Tooltip("未解鎖節點的透明度。已經指定 lockedIcon 的話設 1 就好，設低一點會再暗一層")]
    [Range(0f, 1f)] public float lockedAlpha = 1f;
    [Tooltip("節點圖是否維持原始長寬比，避免被 nodeSize 拉伸變形")]
    public bool preserveAspect = true;

    [Header("回溯時的好感度")]
    [Tooltip("從地圖進入節點時，這些變數會先被設好。\n" +
             "理由：好感度是「讓玩家走到這個節點」的門票，節點解鎖就代表當初驗過了，不該再驗一次。\n" +
             "不這樣做的話，@exitToTitle 會把好感度清成 0，分歧點的兩個選項會掉進同一個結局。\n\n" +
             "但只有「曾經走過那條線」的角色才算數（Require Visited）——如果玩家之前跑的都是\n" +
             "別人的線，卻被薇狄亞攔下來說「我一直都在這裡」，那說不通。")]
    //
    // 只放「決定進不進得去某條線」的好感度。決定「線內拿到哪個結局」的那些
    // （西碧兒、涅莉）刻意不放——預設達標會把節點裡的分歧直接判死，
    // 玩家在地圖日約誰就變得毫無意義。那兩個要讓玩家自己在節點裡重新養。
    //
    public AffinityPreset[] affinityPresets =
    {
        new AffinityPreset { variableName = "affinity_Mel", requireVisited = "chapter4red" },
        new AffinityPreset { variableName = "affinity_Ved", requireVisited = "chapter4green" },
        new AffinityPreset { variableName = "affinity_Eup", requireVisited = "chapter5blue" },
    };
    [Tooltip("走過那條線時要設定的值。要蓋掉個別節點請用 BranchNode 的 Variable Overrides")]
    public int defaultAffinityValue = 100;
    [Tooltip("沒走過那條線時要設定的值")]
    public int lockedAffinityValue = 0;

    [Header("連接線")]
    public Color linkColor = new Color(1f, 1f, 1f, 0.28f);
    public float linkThickness = 2f;

    const string ContentName = "TreeContent";

    // 排版計算用的暫存
    class TreeItem
    {
        public BranchNode data;
        public List<TreeItem> children = new List<TreeItem>();
        public int depth;
        public float column;    // 水平格位（葉節點依序遞增，父節點置中於子節點之上）
        public Vector2 pos;     // 最終 anchoredPosition（相對於 content 的左上）
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
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
        else
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }


    private void GenerateNodes()
    {
        if (database == null)      { Debug.LogError("[BranchMapUI] database 為 NULL！"); return; }
        if (nodeButtonPrefab == null) { Debug.LogError("[BranchMapUI] nodeButtonPrefab 為 NULL！"); return; }
        if (nodeContainer == null) { Debug.LogError("[BranchMapUI] nodeContainer 為 NULL！"); return; }
        if (database.nodes == null || database.nodes.Length == 0)
        {
            Debug.LogWarning("[BranchMapUI] database.nodes 是空的 → 不會生成任何按鈕");
            return;
        }

        var viewport = nodeContainer as RectTransform;
        if (viewport == null) { Debug.LogError("[BranchMapUI] nodeContainer 不是 RectTransform！"); return; }

        // 樹狀排版要自己算座標，GridLayoutGroup 會蓋掉我們設的位置，先關掉
        var grid = viewport.GetComponent<GridLayoutGroup>();
        if (grid != null && grid.enabled) grid.enabled = false;

        // 每次重開都重建，先清乾淨（含上次的 TreeContent）
        for (int i = viewport.childCount - 1; i >= 0; i--)
            Destroy(viewport.GetChild(i).gameObject);

        // ── ① 建樹 ──────────────────────────────────────────
        List<TreeItem> all;
        var roots = BuildTree(database.nodes, out all);

        // ── ② 算格位 ────────────────────────────────────────
        float nextColumn = 0f;
        foreach (var root in roots)
            AssignColumns(root, 0, ref nextColumn);

        int maxDepth = 0;
        float maxColumn = 0f;
        foreach (var it in all)
        {
            if (it.depth > maxDepth) maxDepth = it.depth;
            if (it.column > maxColumn) maxColumn = it.column;
        }

        float treeW = (maxColumn + 1f) * stepX + padding * 2f;
        float treeH = (maxDepth + 1f) * stepY + padding * 2f;

        // 座標系：content 的錨點/樞紐在正上方中央，樹從頂端往下長
        float halfW = maxColumn * 0.5f;
        foreach (var it in all)
            it.pos = new Vector2((it.column - halfW) * stepX,
                                 -(it.depth + 0.5f) * stepY - padding);

        // ── ③ 建立可捲動的 content ──────────────────────────
        var content = new GameObject(ContentName, typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot     = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(treeW, treeH);
        content.localScale = new Vector3(treeScale, treeScale, 1f);

        // ── ④ 先畫連接線（放最前面才會在節點底下）────────────
        var linkRoot = new GameObject("TreeLinks", typeof(RectTransform)).GetComponent<RectTransform>();
        linkRoot.SetParent(content, false);
        TopCenterAnchors(linkRoot);
        linkRoot.anchoredPosition = Vector2.zero;
        linkRoot.sizeDelta = Vector2.zero;
        linkRoot.SetAsFirstSibling();

        foreach (var it in all)
            foreach (var child in it.children)
                DrawElbow(linkRoot, it.pos, child.pos);

        // ── ⑤ 生成節點 ──────────────────────────────────────
        foreach (var it in all)
        {
            var newObj = Instantiate(nodeButtonPrefab, content);

            var rt = newObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                TopCenterAnchors(rt);
                rt.sizeDelta = nodeSize;
                rt.anchoredPosition = it.pos;
            }

            var btnScript = newObj.GetComponent<NodeButton>();
            if (btnScript == null)
                Debug.LogError($"[BranchMapUI] NodeButton.cs 沒掛在 prefab 上（{it.data.displayName}）");
            else
                btnScript.Init(it.data, this);
        }

        // ── ⑥ 捲動或自動縮放 ────────────────────────────────
        if (enableScroll) SetupScroll(viewport, content);
        else              FitToViewport(viewport, content, treeW, treeH);

        Debug.Log($"[BranchMapUI] {all.Count} 個節點、{maxDepth + 1} 層、{maxColumn + 1} 欄 → " +
                  $"content {treeW:F0}x{treeH:F0} @ scale {treeScale:F2}");
    }

    /// <summary>把 viewport 就地改造成可用滾輪捲動的 ScrollRect。</summary>
    void SetupScroll(RectTransform viewport, RectTransform content)
    {
        // 沒有 Graphic 的話滑鼠事件會穿過去，滾輪就捲不動 —— 補一張全透明的擋板
        var raycastTarget = viewport.GetComponent<Graphic>();
        if (raycastTarget == null)
        {
            var img = viewport.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        // 超出可視範圍的部分要裁掉
        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        var scroll = viewport.GetComponent<ScrollRect>();
        if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = true;   // 樹也可能比畫面寬，允許左右拖曳
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = scrollSensitivity;
        scroll.inertia = false;     // 樹狀圖用慣性滑動反而不好對位
        scroll.horizontalScrollbar = null;
        scroll.verticalScrollbar = null;

        // 每次打開都從最上面開始看
        content.anchoredPosition = Vector2.zero;
    }

    /// <summary>不捲動時的舊行為：整棵樹縮小到塞得進容器。</summary>
    void FitToViewport(RectTransform viewport, RectTransform content, float treeW, float treeH)
    {
        float availW = viewport.rect.width;
        float availH = viewport.rect.height;
        if (availW <= 1f || availH <= 1f) return;

        float scale = Mathf.Min(treeScale, availW / treeW, availH / treeH);
        content.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>依 parentId 把節點接成樹；找不到父節點的一律當樹根。</summary>
    List<TreeItem> BuildTree(BranchNode[] nodes, out List<TreeItem> all)
    {
        var byKey = new Dictionary<string, TreeItem>();
        all = new List<TreeItem>();

        foreach (var n in nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;

            var item = new TreeItem { data = n };
            all.Add(item);

            if (!byKey.ContainsKey(n.Key))
                byKey.Add(n.Key, item);
            else
                Debug.LogWarning($"[BranchMapUI] 節點 Key 重複：{n.Key}（{n.displayName}）");
        }

        var roots = new List<TreeItem>();
        foreach (var item in all)
        {
            var pid = item.data.parentId;
            TreeItem parent;
            if (!string.IsNullOrEmpty(pid) && byKey.TryGetValue(pid, out parent) && parent != item)
                parent.children.Add(item);
            else
            {
                if (!string.IsNullOrEmpty(pid))
                    Debug.LogWarning($"[BranchMapUI] 找不到父節點 '{pid}'，{item.data.displayName} 當成樹根處理");
                roots.Add(item);
            }
        }

        if (roots.Count == 0 && all.Count > 0)
        {
            Debug.LogWarning("[BranchMapUI] parentId 形成循環，改用第一個節點當樹根");
            roots.Add(all[0]);
        }

        return roots;
    }

    /// <summary>後序走訪：葉節點依序佔一格，父節點置中於第一個與最後一個子節點之間。</summary>
    void AssignColumns(TreeItem item, int depth, ref float nextColumn)
    {
        item.depth = depth;

        if (item.children.Count == 0)
        {
            item.column = nextColumn;
            nextColumn += 1f;
            return;
        }

        foreach (var c in item.children)
            AssignColumns(c, depth + 1, ref nextColumn);

        item.column = (item.children[0].column + item.children[item.children.Count - 1].column) * 0.5f;
    }

    /// <summary>父子之間畫一條 ㄇ 字形連線：往下 → 橫移 → 再往下。</summary>
    void DrawElbow(RectTransform parentRect, Vector2 from, Vector2 to)
    {
        float midY = (from.y + to.y) * 0.5f;

        AddBar(parentRect, new Vector2(from.x, (from.y + midY) * 0.5f),
               new Vector2(linkThickness, Mathf.Abs(from.y - midY)));

        if (!Mathf.Approximately(from.x, to.x))
        {
            AddBar(parentRect, new Vector2((from.x + to.x) * 0.5f, midY),
                   new Vector2(Mathf.Abs(to.x - from.x) + linkThickness, linkThickness));
        }

        AddBar(parentRect, new Vector2(to.x, (midY + to.y) * 0.5f),
               new Vector2(linkThickness, Mathf.Abs(midY - to.y)));
    }

    void AddBar(RectTransform parentRect, Vector2 center, Vector2 size)
    {
        var go = new GameObject("Link", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parentRect, false);
        TopCenterAnchors(rt);
        rt.anchoredPosition = center;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = linkColor;
        img.raycastTarget = false;   // 不擋滾輪、也不擋按鈕
    }

    static void TopCenterAnchors(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
    }
}
