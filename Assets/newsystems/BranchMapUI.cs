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

    [Header("版面")]
    [Tooltip("勾起來＝書頁版面：依 BranchNode.slotName 把節點擺到 Node Slot Root 底下對應的插槽上。\n" +
             "取消＝舊的樹狀自動排版（會自己算座標、畫連接線、開捲動）。")]
    public bool useSlotLayout = false;
    [Tooltip("書頁版面用：插槽的父物件。底下每個子物件就是一個位置，名字對應 BranchNode.slotName")]
    public Transform nodeSlotRoot;
    [Tooltip("★排版用★ 勾起來會把所有節點當成已解鎖，好讓你在編輯時看到各自的圖。\n" +
             "出貨前記得取消，否則玩家一開始就看得到全部節點。")]
    public bool debugUnlockAll = false;
    [Tooltip("節點大小改由各自的 prefab 決定（羅馬數字和寶石大小不同，共用一個值一定有一邊被拉壞）。\n" +
             "取消＝全部統一套用底下「樹狀排版」的 Node Size。只影響書頁版面。")]
    public bool usePrefabSize = true;

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
    // 只放「決定進不進得去某條線」的好感度。西碧兒刻意不放——她決定的是
    // 綠線內拿到哪個結局（06／13／20），預設達標會把那個分歧直接判死，
    // 玩家在綠線那四個晚上約誰就變得毫無意義。她要讓玩家自己重新養。
    //
    // 涅莉在這裡是因為第四章的「跟著涅莉」需要 50 才進得去黃線。
    // 想在節點裡自己決定 09／07／08 的話，用 BranchNode 的 Variable Overrides
    // individually 蓋掉（SPIRAL 那格就是覆寫成 50）。
    //
    public AffinityPreset[] affinityPresets =
    {
        new AffinityPreset { variableName = "affinity_Mel", requireVisited = "chapter4red" },
        new AffinityPreset { variableName = "affinity_Ved", requireVisited = "chapter4green" },
        new AffinityPreset { variableName = "affinity_Eup", requireVisited = "chapter5blue" },
        new AffinityPreset { variableName = "affinity_Nel", requireVisited = "chapter5yellow" },
    };
    [Tooltip("走過那條線時要設定的值。要蓋掉個別節點請用 BranchNode 的 Variable Overrides")]
    public int defaultAffinityValue = 100;
    [Tooltip("沒走過那條線時要設定的值")]
    public int lockedAffinityValue = 0;

    [Header("劇情完成度")]
    [Tooltip("顯示完成度的 Text。留空就不顯示，其餘功能不受影響")]
    public Text completionText;
    [Tooltip("結局總數。目前 20 個（ACH_END_01 ～ ACH_END_20）")]
    public int totalEndings = 20;
    [Tooltip("{0}=已收集　{1}=總數")]
    public string completionFormat = "結局　{0} / {1}";

    [Header("右頁預覽")]
    [Tooltip("整組預覽的容器。沒有滑到任何節點時會關掉，留空＝不開關容器")]
    public GameObject previewRoot;
    [Tooltip("三角形裡顯示 BranchNode.preview 的 Image")]
    public Image previewImage;
    [Tooltip("顯示節點名稱的 Text（用 displayName）。留空＝不顯示")]
    public Text previewTitle;
    [Tooltip("三角形周圍的關鍵字，順序對應 BranchNode.keywords。放幾個就支援幾個")]
    public Text[] keywordTexts;
    [Tooltip("滑鼠移開後保留上一次的預覽，不要清空。\n" +
             "書頁上寶石很密，一直清空會閃個不停，覺得吵就勾這個")]
    public bool keepLastPreview = false;

    // 現在顯示中的是哪一格。用來擋掉「從 A 直接滑到 B」時
    // B 的 Enter 先送、A 的 Exit 後送，把 B 誤清掉的狀況。
    private BranchNode hoveredNode;

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
        {
            GenerateNodes();
            RefreshCompletion();

            // 「蝕之聖典開啟」成就。標題選單那顆按鈕是 prefab 裡的 @showUI BranchMapUI，
            // 沒有程式可以掛，所以改在面板自己被顯示出來時觸發。
            AchievementManager.Instance?.Unlock(AchievementManager.ACH_CODEX_OPEN);
        }

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


    /// <summary>
    /// 依 EndingRecord 的本地紀錄更新完成度。每次打開地圖時重算一次。
    /// 刻意只給分數不給百分比——旁邊就是 15 格亮暗分明的節點，
    /// 放百分比會讓玩家以為那是在講節點解鎖率。
    /// </summary>
    private void RefreshCompletion()
    {
        if (!completionText) return;

        completionText.text = string.Format(completionFormat, EndingRecord.Count, totalEndings);
    }

    /// <summary>滑鼠移到節點上時由 NodeButton 呼叫。</summary>
    public void ShowPreview(BranchNode node)
    {
        if (node == null) return;
        hoveredNode = node;

        if (previewRoot) previewRoot.SetActive(true);

        if (previewImage)
        {
            previewImage.sprite = node.preview;
            previewImage.preserveAspect = preserveAspect;
            // 沒指定圖就整個關掉，免得露出 Image 的白底
            previewImage.enabled = node.preview != null;
        }

        if (previewTitle) previewTitle.text = node.displayName;

        ApplyKeywords(node.keywords);
    }

    /// <summary>滑鼠離開節點時由 NodeButton 呼叫；node 傳 null 代表無條件清空。</summary>
    public void ClearPreview(BranchNode node)
    {
        // 離開的不是現在顯示中的那一格，代表滑鼠已經滑到別格上了，不要清
        if (node != null && hoveredNode != node) return;

        hoveredNode = null;
        if (keepLastPreview && node != null) return;

        if (previewRoot) previewRoot.SetActive(false);
        if (previewImage) previewImage.enabled = false;
        if (previewTitle) previewTitle.text = "";
        ApplyKeywords(null);
    }

    private void ApplyKeywords(string[] words)
    {
        if (keywordTexts == null) return;

        for (int i = 0; i < keywordTexts.Length; i++)
        {
            if (!keywordTexts[i]) continue;

            var has = words != null && i < words.Length && !string.IsNullOrEmpty(words[i]);
            keywordTexts[i].text = has ? words[i] : "";
        }
    }

    private void GenerateNodes()
    {
        // 每次打開地圖都從「什麼都沒滑到」開始，不要留著上次關閉時的殘影
        ClearPreview(null);

        if (database == null)      { Debug.LogError("[BranchMapUI] database 為 NULL！"); return; }
        if (nodeButtonPrefab == null) { Debug.LogError("[BranchMapUI] nodeButtonPrefab 為 NULL！"); return; }
        if (database.nodes == null || database.nodes.Length == 0)
        {
            Debug.LogWarning("[BranchMapUI] database.nodes 是空的 → 不會生成任何按鈕");
            return;
        }

        if (useSlotLayout)
        {
            GenerateOnSlots();
            return;
        }

        GenerateTree();
    }

    // ══════════════════════════════════════════════════════════════
    //  書頁版面：位置由美術決定，程式只負責把節點放到對應的插槽上。
    //  不建樹、不算座標、不畫連接線——書頁上的線是背景圖的一部分。
    // ══════════════════════════════════════════════════════════════
    private void GenerateOnSlots()
    {
        if (nodeSlotRoot == null)
        {
            Debug.LogError("[BranchMapUI] 書頁版面需要指定 Node Slot Root");
            return;
        }

        var placed = 0;
        var missing = new List<string>();

        foreach (var node in database.nodes)
        {
            if (node == null) continue;

            if (string.IsNullOrEmpty(node.slotName))
            {
                missing.Add($"{node.displayName}（沒填 slotName）");
                continue;
            }

            var slot = nodeSlotRoot.Find(node.slotName);
            if (slot == null)
            {
                missing.Add($"{node.displayName} → 找不到插槽「{node.slotName}」");
                continue;
            }

            // 每次重開都重建，先清掉插槽上一輪的內容
            for (int i = slot.childCount - 1; i >= 0; i--)
                Destroy(slot.GetChild(i).gameObject);

            var prefab = node.buttonPrefab != null ? node.buttonPrefab : nodeButtonPrefab;
            var go = Instantiate(prefab, slot);
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                // 插槽本身就是位置，節點貼齊插槽中心
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                // 不覆寫的話就保留 prefab 自己的 RectTransform 尺寸
                if (!usePrefabSize) rt.sizeDelta = nodeSize;
            }

            var btn = go.GetComponent<NodeButton>();
            if (btn == null)
                Debug.LogError($"[BranchMapUI] NodeButton.cs 沒掛在 prefab 上（{node.displayName}）");
            else
                btn.Init(node, this);

            placed++;
        }

        Debug.Log($"[BranchMapUI] 書頁版面：擺上 {placed} / {database.nodes.Length} 個節點");
        foreach (var m in missing)
            Debug.LogWarning($"[BranchMapUI] 沒擺上：{m}");
    }

#if UNITY_EDITOR
    /// <summary>
    /// 插槽是空的 RectTransform，Scene 視圖裡看不見。這裡把每個插槽的範圍和名字
    /// 畫出來，排版時才知道節點會落在哪、會不會互相重疊。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!useSlotLayout || nodeSlotRoot == null) return;

        foreach (Transform slot in nodeSlotRoot)
        {
            var rt = slot as RectTransform;
            if (rt == null) continue;

            var half = SizeForSlot(slot.name) * 0.5f;
            var c = new Vector3[4]
            {
                rt.TransformPoint(new Vector3(-half.x, -half.y, 0f)),
                rt.TransformPoint(new Vector3(-half.x,  half.y, 0f)),
                rt.TransformPoint(new Vector3( half.x,  half.y, 0f)),
                rt.TransformPoint(new Vector3( half.x, -half.y, 0f)),
            };

            Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.9f);
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(c[i], c[(i + 1) % 4]);

            UnityEditor.Handles.Label(rt.TransformPoint(new Vector3(-half.x, half.y, 0f)), slot.name);
        }
    }

    /// <summary>這個插槽上的節點實際會有多大——畫框才畫得準。</summary>
    private Vector2 SizeForSlot(string slotName)
    {
        if (!usePrefabSize) return nodeSize;
        if (database == null || database.nodes == null) return nodeSize;

        foreach (var n in database.nodes)
        {
            if (n == null || n.slotName != slotName) continue;

            var prefab = n.buttonPrefab != null ? n.buttonPrefab : nodeButtonPrefab;
            var prt = prefab != null ? prefab.transform as RectTransform : null;
            return prt != null ? prt.sizeDelta : nodeSize;
        }
        return nodeSize;   // 沒有節點對應到這個插槽
    }

    /// <summary>
    /// 把 Node Slot Root 拉成完全貼合書頁，同時保住每個插槽現在的視覺位置。
    ///
    /// 插槽的座標是相對 Node Slot Root 的中心算的。只要那個中心沒有跟書頁中心重合，
    /// Inspector 上看到的數字就不等於書頁上的位置，排起來全靠目測。這裡先記下所有
    /// 插槽的世界座標，改完 Root 再把世界座標填回去，所以畫面上不會有任何東西移動，
    /// 只是座標系被扶正了。
    /// </summary>
    [ContextMenu("校正 NodeSlots 對齊書頁")]
    private void NormalizeSlotRoot()
    {
        var root = nodeSlotRoot as RectTransform;
        if (root == null) { Debug.LogError("[BranchMapUI] Node Slot Root 沒指定或不是 RectTransform"); return; }

        var before = new Dictionary<Transform, Vector3>();
        foreach (Transform slot in root) before[slot] = slot.position;

        UnityEditor.Undo.RecordObject(root, "校正 NodeSlots");
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        foreach (var kv in before)
        {
            UnityEditor.Undo.RecordObject(kv.Key, "校正 NodeSlots");
            kv.Key.position = kv.Value;
        }

        Debug.Log($"[BranchMapUI] NodeSlots 已對齊書頁，{before.Count} 個插槽位置不變");
    }

    /// <summary>
    /// 把 PreviewSamples 底下的佔位圖和範例關鍵字填進 database，方便先把功能跑起來看。
    /// 只填空的欄位——已經放了正式圖的節點不會被蓋掉，之後補圖時可以放心重按。
    /// </summary>
    [ContextMenu("填入範例預覽圖與關鍵字")]
    private void FillSamplePreviews()
    {
        if (database == null || database.nodes == null) { Debug.LogError("[BranchMapUI] 沒有 database"); return; }

        // 佔位用的示意詞，正式的自己改
        var samples = new Dictionary<string, string[]>
        {
            { "HOUND",      new[] { "入學",   "初遇",   "犬" } },
            { "TOME",       new[] { "禁書",   "抄寫",   "規則" } },
            { "TETHER",     new[] { "繫縛",   "距離",   "試探" } },
            { "SEANCE",     new[] { "降靈",   "裂痕",   "預兆" } },
            { "PARTING",    new[] { "別離",   "三條路", "決定" } },
            { "TRUCE",      new[] { "梅爾",   "停戰",   "傷" } },
            { "ACCOMPLICE", new[] { "共犯",   "隱瞞",   "夜" } },
            { "UNMASK",     new[] { "揭露",   "米夏",   "真相" } },
            { "CHALICE",    new[] { "聖杯",   "邪神血", "王座" } },
            { "REVERIE",    new[] { "優菲",   "夢",     "塔" } },
            { "VIGIL",      new[] { "守夜",   "告別",   "星" } },
            { "CROWN",      new[] { "冠冕",   "替身",   "純藍" } },
            { "COMMUNION",  new[] { "薇狄亞", "共感",   "森林" } },
            { "WHISPER",    new[] { "坦白",   "沉默",   "火" } },
            { "SPIRAL",     new[] { "涅莉",   "輪迴",   "日蝕" } },
        };

        int filled = 0, kept = 0, missing = 0;
        foreach (var node in database.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.displayName)) continue;

            if (node.preview != null) { kept++; }
            else
            {
                var path = "Assets/newsystems/PreviewSamples/preview_" + node.displayName + ".png";
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) { Debug.LogWarning($"[BranchMapUI] 找不到 {path}"); missing++; continue; }
                node.preview = sprite;
                filled++;
            }

            string[] words;
            if ((node.keywords == null || node.keywords.Length == 0) && samples.TryGetValue(node.displayName, out words))
                node.keywords = words;
        }

        UnityEditor.EditorUtility.SetDirty(database);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[BranchMapUI] 填了 {filled} 張佔位圖，跳過 {kept} 個已有圖的，{missing} 個找不到檔案");
    }

    /// <summary>
    /// 一鍵把右頁預覽整組建好並接上欄位。座標是照書頁比例估的概略值，建完自己微調。
    /// 關鍵字和標題刻意放在遮罩外面，放進去會被三角形一起切掉。
    /// </summary>
    [ContextMenu("建立右頁預覽 UI")]
    private void CreatePreviewUI()
    {
        var parent = nodeSlotRoot != null ? nodeSlotRoot.parent as RectTransform : null;
        if (parent == null) { Debug.LogError("[BranchMapUI] 請先指定 Node Slot Root（預覽會建在它的父物件底下）"); return; }
        if (previewRoot != null) { Debug.LogWarning("[BranchMapUI] Preview Root 已經有東西了，先刪掉再按一次"); return; }

        // 沿用畫面上現有 Text 的字型，才顯示得出中文
        var sample = GetComponentInChildren<Text>(true);
        var font = (sample != null && sample.font != null)
                 ? sample.font
                 : Resources.GetBuiltinResource<Font>("Arial.ttf");

        var tri = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/newsystems/triangle_mask.png");
        if (tri == null) Debug.LogWarning("[BranchMapUI] 找不到 triangle_mask.png，遮罩圖要自己拖");

        var root = NewRect("Preview", parent);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

        var mask = NewRect("TriangleMask", root);
        Place(mask, 510f, 110f, 660f, 580f);
        var maskImg = mask.gameObject.AddComponent<Image>();
        maskImg.sprite = tri;
        maskImg.raycastTarget = false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        var inner = NewRect("PreviewImage", mask);
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = Vector2.zero; inner.offsetMax = Vector2.zero;
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.raycastTarget = false;
        innerImg.enabled = false;

        var title = NewText("PreviewTitle", root, font, 40, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 510f, -230f, 600f, 60f);

        var kws = new Text[3];
        kws[0] = NewText("Keyword1", root, font, 28, TextAnchor.MiddleCenter);
        Place(kws[0].rectTransform, 510f, 440f, 420f, 44f);
        kws[1] = NewText("Keyword2", root, font, 28, TextAnchor.MiddleRight);
        Place(kws[1].rectTransform, 175f, -60f, 240f, 44f);
        kws[2] = NewText("Keyword3", root, font, 28, TextAnchor.MiddleLeft);
        Place(kws[2].rectTransform, 845f, -60f, 240f, 44f);

        previewRoot = root.gameObject;
        previewImage = innerImg;
        previewTitle = title;
        keywordTexts = kws;

        UnityEditor.Undo.RegisterCreatedObjectUndo(root.gameObject, "建立右頁預覽 UI");
        AlignPreviewUI();   // 位置一律走量測值，這裡不重複寫一份
        Debug.Log("[BranchMapUI] 右頁預覽建好、接上欄位並排到書頁上的三角形了");
    }

    /// <summary>
    /// 把右頁預覽整組排回書頁上量出來的正確位置。
    /// 數值是直接量 ec.jpg 得到的：書上那個三角形頂邊 y=254、x 1051~1643，頂點 (1352, 751)，
    /// 換算成書頁座標就是中心 (387, 37)、586x492。三個關鍵字對稱擺在三角形的上、左、右，
    /// 標題壓在頂點正下方、圓形圖案上方。
    /// </summary>
    [ContextMenu("校正右頁預覽位置")]
    private void AlignPreviewUI()
    {
        if (previewRoot == null) { Debug.LogError("[BranchMapUI] Preview Root 沒指定，先跑「建立右頁預覽 UI」"); return; }

        var root = previewRoot.transform as RectTransform;
        if (root == null) { Debug.LogError("[BranchMapUI] Preview Root 不是 RectTransform"); return; }

        UnityEditor.Undo.RecordObject(root, "校正右頁預覽位置");
        // 容器一律貼合書頁。它只要歪一點，底下所有東西就跟著歪。
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var tri = root.Find("TriangleMask") as RectTransform;
        if (tri != null) { UnityEditor.Undo.RecordObject(tri, "校正"); Place(tri, 387f, 37f, 586f, 492f); }

        if (previewTitle != null)
        {
            UnityEditor.Undo.RecordObject(previewTitle.rectTransform, "校正");
            Place(previewTitle.rectTransform, 387f, -248f, 600f, 60f);
            previewTitle.alignment = TextAnchor.MiddleCenter;
        }

        // 上、左、右。左右兩個對稱於三角形中心 387，各自貼著斜邊外側。
        var slots = new[]
        {
            new { x = 387f, y = 468f, w = 460f, h = 48f, a = TextAnchor.MiddleCenter },
            new { x = 175f, y = -60f, w = 240f, h = 44f, a = TextAnchor.MiddleRight  },
            new { x = 599f, y = -60f, w = 240f, h = 44f, a = TextAnchor.MiddleLeft   },
        };

        if (keywordTexts != null)
            for (int i = 0; i < keywordTexts.Length && i < slots.Length; i++)
            {
                if (keywordTexts[i] == null) continue;
                UnityEditor.Undo.RecordObject(keywordTexts[i].rectTransform, "校正");
                Place(keywordTexts[i].rectTransform, slots[i].x, slots[i].y, slots[i].w, slots[i].h);
                keywordTexts[i].alignment = slots[i].a;
            }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[BranchMapUI] 右頁預覽已排回量測位置");
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static Text NewText(string name, Transform parent, Font font, int size, TextAnchor align)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = new Color(0.22f, 0.16f, 0.10f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;   // 別擋到節點的滑鼠事件
        t.text = "";
        return t;
    }

    /// <summary>
    /// 依 database 幫每個節點在 Node Slot Root 底下建一個空插槽（已存在的不動）。
    /// 建完之後在 Scene 視圖裡把它們拖到書頁上的寶石位置就好。
    /// </summary>
    [ContextMenu("建立缺少的插槽")]
    private void CreateMissingSlots()
    {
        if (database == null || database.nodes == null) { Debug.LogError("[BranchMapUI] 沒有 database"); return; }
        if (nodeSlotRoot == null) { Debug.LogError("[BranchMapUI] 請先指定 Node Slot Root"); return; }

        var created = 0;
        foreach (var node in database.nodes)
        {
            if (node == null) continue;

            // 沒填 slotName 的話，用 displayName 當預設名字並回寫
            if (string.IsNullOrEmpty(node.slotName))
            {
                node.slotName = string.IsNullOrEmpty(node.displayName) ? node.Key : node.displayName;
                UnityEditor.EditorUtility.SetDirty(database);
            }

            if (nodeSlotRoot.Find(node.slotName) != null) continue;

            var slot = new GameObject(node.slotName, typeof(RectTransform));
            var rt = slot.GetComponent<RectTransform>();
            rt.SetParent(nodeSlotRoot, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = nodeSize;
            created++;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        // 上面回寫的 slotName 只 SetDirty 還在記憶體裡，沒存檔就關 Unity 會整個掉。
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[BranchMapUI] 建立了 {created} 個插槽（全部疊在中心，請拖到書頁上的位置）");
    }
#endif

    // ══════════════════════════════════════════════════════════════
    //  舊的樹狀自動排版。書頁版面完成後可以整段刪掉。
    // ══════════════════════════════════════════════════════════════
    private void GenerateTree()
    {
        if (nodeContainer == null) { Debug.LogError("[BranchMapUI] nodeContainer 為 NULL！"); return; }

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
