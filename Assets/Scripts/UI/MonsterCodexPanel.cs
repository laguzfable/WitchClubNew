using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 掛在 CGGalleryUI 的 BrowserPanel 上，在原本的 CG 格子旁邊多長出一個「怪物圖鑑」分頁。
    ///
    /// 整個 UI 都是程式跑出來的，沒有動到 CGGalleryUI.prefab（那份是 Naninovel 的，
    /// 之後升級套件比較不會打架）。實際掛上去的人是 <see cref="MonsterCodexInjector"/>。
    ///
    /// BrowserPanel 是 VerticalLayoutGroup，原本的小孩順序是：
    ///   TitleLabel / CGGrid / PaginationPanel / Space / ReturnButton
    /// 我們插進去之後變成：
    ///   TitleLabel / TabBar / CGGrid / PaginationPanel / MonsterGrid / MonsterPager / Space / ReturnButton
    /// 切分頁就是把 CG 那組跟怪物那組互相 SetActive。
    /// </summary>
    public class MonsterCodexPanel : MonoBehaviour
    {
        // ======== 想調版面的話動這幾個就好 ========
        // 座標單位是 CanvasScaler 的參考解析度 1920x1080（match = height），
        // 所以高度永遠是 1080，寬度會隨螢幕比例變（16:9 時是 1920）。

        /// <summary>面板左右各留多少邊。</summary>
        const float PanelMarginX = 70f;
        /// <summary>面板上下各留多少邊。</summary>
        const float PanelMarginY = 45f;

        /// <summary>
        /// 怪物圖鑑一頁排幾欄幾列。用 5x2 是因為怪物立繪多半是直的，
        /// 列數少一點格子才夠高、圖才放得大；26 隻剛好三頁（10/10/6）。
        /// </summary>
        const int MonsterColumns = 5;
        const int MonsterRows = 2;
        const int ItemsPerPage = MonsterColumns * MonsterRows;

        /// <summary>CG 那邊格子數是 Naninovel 的 CGGalleryGrid 決定的（itemsPerPage=9），所以固定 3x3。</summary>
        const int CGColumns = 3;
        const int CGRows = 3;
        /// <summary>CG 縮圖固定 16:9，格子拉太寬圖會變形，所以鎖長寬比。</summary>
        const float CGCellAspect = 16f / 9f;

        static readonly Vector2 CellSpacing = new Vector2(10, 10);
        static readonly Color NewBadgeColor = new Color(1f, 0.86f, 0.45f);

        static readonly Color TabSelectedColor = new Color(1f, 1f, 1f, 0.32f);
        static readonly Color TabNormalColor = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color TabSelectedTextColor = Color.white;
        static readonly Color TabNormalTextColor = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color SlotBackColor = new Color(1f, 1f, 1f, 0.07f);
        /// <summary>沒解鎖的怪物用這個顏色乘上去，變成剪影。</summary>
        static readonly Color LockedTint = new Color(0f, 0f, 0f, 0.8f);

        Font font;

        // CG 分頁原本就有的東西，切換分頁時要一起開關
        readonly List<GameObject> cgObjects = new List<GameObject>();

        // 怪物分頁自己長出來的東西
        GameObject monsterGrid;
        GameObject monsterPager;
        Text pageLabel;
        Button prevPageButton;
        Button nextPageButton;
        Text codexTabLabel;

        Image cgTabImage, codexTabImage;
        Text cgTabText, codexTabText;

        MonsterDetailOverlay overlay;

        readonly List<Slot> slots = new List<Slot>();

        // 全螢幕排版用
        RectTransform panelRect;
        RectTransform canvasRect;
        VerticalLayoutGroup panelLayout;
        GridLayoutGroup cgGridLayout;
        GridLayoutGroup monsterGridLayout;
        Vector2 lastCanvasSize = Vector2.zero;
        bool lastCodexActive;

        int currentPage = 1;
        bool codexActive;

        int PageCount => Mathf.Max(1, Mathf.CeilToInt(MonsterCodex.AllMobs.Count / (float)ItemsPerPage));

        class Slot
        {
            public GameObject root;
            public Image back;
            public Image portrait;
            public Text label;
            public Text newBadge;
            public MobData mob;
        }

        void Awake ()
        {
            font = FindFont();
            Build();
            SelectTab(false);
        }

        void OnEnable ()
        {
            // NEW 角標：每次打開都算一次新的瀏覽。這裡不能寫在 Awake——
            // 面板注入之後就一直活著，只是被開開關關，Awake 整場遊戲只跑一次，
            // 寫在那裡的話角標會一直停在第一次的狀態，關掉再開也不會消。
            NewItemTracker.BeginVisit(NewItemTracker.Monsters);

            // 每次重新打開回憶模式都回到 CG 分頁，並且把解鎖狀態重刷一次
            currentPage = 1;
            SelectTab(false);

            // 「CG 全收集」成就：CG 是 Naninovel 在管的，沒有解鎖當下的 hook 可以掛，
            // 所以在玩家打開回憶模式時補檢查一次。
            CGGalleryProgress.CheckAchievement(GetComponentInParent<Naninovel.UI.CGGalleryPanel>());
        }

        // NEW 角標用的：Naninovel 的 UI 是靠透明度隱藏的，不是 SetActive，
        // 所以關掉再打開不會觸發 OnEnable。要自己盯著可見狀態的變化，
        // 否則「一次瀏覽」整場遊戲只會開始一次，角標要重開遊戲才會消。
        Naninovel.UI.CGGalleryPanel gallery;
        bool wasVisible;

        void Update ()
        {
            if (overlay != null && overlay.IsShown && Input.GetKeyDown(KeyCode.Escape))
                overlay.Hide();

            WatchVisibility();
        }

        void WatchVisibility ()
        {
            if (gallery == null) gallery = GetComponentInParent<Naninovel.UI.CGGalleryPanel>();

            var visibleNow = gallery != null && gallery.Visible;
            if (visibleNow && !wasVisible)
            {
                NewItemTracker.BeginVisit(NewItemTracker.Monsters);
                Refresh();   // 重畫才會照新的一輪重算角標
            }
            wasVisible = visibleNow;
        }

        void LateUpdate ()
        {
            // 視窗大小會變（全螢幕切換、拉視窗），所以格子尺寸要跟著重算。
            // 只有真的變了才算，平常這裡不做事。
            if (canvasRect == null) return;

            var size = canvasRect.rect.size;
            if (size == lastCanvasSize && codexActive == lastCodexActive) return;

            lastCanvasSize = size;
            lastCodexActive = codexActive;
            ResizeGrids();
        }

        /// <summary>沿用 CGGalleryUI 自己的字體，這樣中文才顯示得出來。</summary>
        Font FindFont ()
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
                if (text.font != null) return text.font;

            Debug.LogWarning("[MonsterCodexPanel] 在 BrowserPanel 底下找不到任何有字體的 Text，改用內建 Arial（中文會變豆腐）。");
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // ===================== 建 UI =====================

        void Build ()
        {
            var titleLabel = transform.Find("TitleLabel");
            var cgGrid = transform.Find("CGGrid");
            var pagination = transform.Find("PaginationPanel");
            var space = transform.Find("Space");

            if (cgGrid != null) cgObjects.Add(cgGrid.gameObject);
            if (pagination != null) cgObjects.Add(pagination.gameObject);

            if (cgGrid == null)
                Debug.LogWarning("[MonsterCodexPanel] 找不到 CGGrid，切回 CG 分頁時可能不會顯示原本的格子。");

            // --- 分頁列，插在標題下面 ---
            var tabBar = BuildTabBar();
            tabBar.transform.SetSiblingIndex(titleLabel != null ? titleLabel.GetSiblingIndex() + 1 : 0);

            // --- 怪物格子 + 翻頁列，插在 Space 前面 ---
            monsterGrid = BuildMonsterGrid();
            monsterPager = BuildPager();

            var insertAt = space != null ? space.GetSiblingIndex() : transform.childCount;
            monsterGrid.transform.SetSiblingIndex(insertAt);
            monsterPager.transform.SetSiblingIndex(insertAt + 1);

            // --- 點怪物之後的放大檢視，掛在 CGGalleryUI 根節點才蓋得住整個畫面 ---
            var uiRoot = transform.parent != null ? transform.parent : transform;
            overlay = MonsterDetailOverlay.Create(uiRoot, font);

            // --- 把整個面板撐到接近全螢幕 ---
            panelRect = GetComponent<RectTransform>();
            panelLayout = GetComponent<VerticalLayoutGroup>();
            canvasRect = uiRoot as RectTransform;
            cgGridLayout = cgGrid != null ? cgGrid.GetComponent<GridLayoutGroup>() : null;
            monsterGridLayout = monsterGrid.GetComponent<GridLayoutGroup>();
            ApplyPanelSize();
        }

        // ===================== 全螢幕排版 =====================

        /// <summary>
        /// 原本的 BrowserPanel 是「置中、固定寬 600、高度隨內容」的小盒子。
        /// 這裡把它改成左右撐滿（各留 <see cref="PanelMarginX"/>），高度還是交給
        /// 原本的 ContentSizeFitter 算，再靠 <see cref="ResizeGrids"/> 把格子撐到剛好填滿螢幕。
        /// 這是執行時改的，CGGalleryUI.prefab 本身沒動。
        /// </summary>
        void ApplyPanelSize ()
        {
            if (panelRect == null) return;

            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            // 錨點左右撐開之後，sizeDelta.x 是「相對父物件寬度的差值」，
            // 給 -PanelMarginX*2 就等於左右各縮 PanelMarginX。
            panelRect.sizeDelta = new Vector2(-PanelMarginX * 2f, panelRect.sizeDelta.y);
            panelRect.anchoredPosition = new Vector2(0f, 0f);

            if (cgGridLayout != null)
            {
                cgGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                cgGridLayout.constraintCount = CGColumns;
                cgGridLayout.spacing = CellSpacing;
                cgGridLayout.childAlignment = TextAnchor.UpperCenter;
            }
        }

        /// <summary>
        /// 算出扣掉標題、分頁列、翻頁列、RETURN 之後還剩多少高度，把它平均分給格子。
        /// 刻意用「畫布高度」而不是「格子自己的高度」來算，不然格子撐高 → 面板變高 →
        /// 格子又重算，會無限迴圈。
        /// </summary>
        void ResizeGrids ()
        {
            if (panelRect == null || canvasRect == null || panelLayout == null) return;

            var activeGrid = codexActive ? monsterGridLayout : cgGridLayout;
            if (activeGrid == null) return;

            var availableHeight = canvasRect.rect.height - PanelMarginY * 2f;

            // 面板裡除了格子以外的東西各佔多高（標題 / 分頁列 / 翻頁列 / 空白 / RETURN）
            float chromeHeight = panelLayout.padding.top + panelLayout.padding.bottom;
            var activeChildren = 0;
            foreach (RectTransform child in panelRect)
            {
                if (!child.gameObject.activeSelf) continue;
                activeChildren++;
                if (child == activeGrid.transform) continue;
                chromeHeight += LayoutUtility.GetPreferredHeight(child);
            }
            chromeHeight += panelLayout.spacing * Mathf.Max(0, activeChildren - 1);

            var gridHeight = Mathf.Max(120f, availableHeight - chromeHeight);
            var gridWidth = Mathf.Max(120f, panelRect.rect.width - panelLayout.padding.left - panelLayout.padding.right);

            if (codexActive)
                monsterGridLayout.cellSize = FitCells(gridWidth, gridHeight, MonsterColumns, MonsterRows, 0f);
            else
                cgGridLayout.cellSize = FitCells(gridWidth, gridHeight, CGColumns, CGRows, CGCellAspect);
        }

        /// <summary>
        /// 把 cols x rows 個格子塞進 width x height 裡。
        /// aspect 傳 0 表示不鎖長寬比（格子直接填滿）；傳正值就會維持該比例、置中留白。
        /// </summary>
        static Vector2 FitCells (float width, float height, int cols, int rows, float aspect)
        {
            var cellW = (width - CellSpacing.x * (cols - 1)) / cols;
            var cellH = (height - CellSpacing.y * (rows - 1)) / rows;

            if (aspect > 0f)
            {
                if (cellW / cellH > aspect) cellW = cellH * aspect;
                else cellH = cellW / aspect;
            }

            return new Vector2(Mathf.Floor(cellW), Mathf.Floor(cellH));
        }

        GameObject BuildTabBar ()
        {
            var bar = NewUIObject("CodexTabBar", transform);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 8);

            var element = bar.AddComponent<LayoutElement>();
            element.minHeight = 62;
            element.preferredHeight = 62;

            BuildTabButton(bar.transform, "CGTab", "回憶 CG", () => SelectTab(false), out cgTabImage, out cgTabText);
            BuildTabButton(bar.transform, "CodexTab", "怪物圖鑑", () => SelectTab(true), out codexTabImage, out codexTabText);
            codexTabLabel = codexTabText;

            return bar;
        }

        void BuildTabButton (Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick,
                             out Image image, out Text text)
        {
            var go = NewUIObject(name, parent);

            image = go.AddComponent<Image>();
            image.color = TabNormalColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            text = CreateText(go.transform, "Label", label, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
        }

        GameObject BuildMonsterGrid ()
        {
            var grid = NewUIObject("MonsterGrid", transform);

            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(200, 200); // 只是初值，實際大小由 ResizeGrids 撐開
            layout.spacing = CellSpacing;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = MonsterColumns;
            layout.childAlignment = TextAnchor.UpperCenter;

            for (int i = 0; i < ItemsPerPage; i++)
                slots.Add(BuildSlot(grid.transform, i));

            return grid;
        }

        Slot BuildSlot (Transform parent, int index)
        {
            var slot = new Slot();

            slot.root = NewUIObject($"MonsterSlot{index}", parent);

            slot.back = slot.root.AddComponent<Image>();
            slot.back.color = SlotBackColor;

            var button = slot.root.AddComponent<Button>();
            button.targetGraphic = slot.back;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnSlotClicked(slot));

            var portraitGo = NewUIObject("Portrait", slot.root.transform);
            slot.portrait = portraitGo.AddComponent<Image>();
            slot.portrait.preserveAspect = true;
            slot.portrait.raycastTarget = false;
            var portraitRect = slot.portrait.rectTransform;
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = Vector2.one;
            portraitRect.offsetMin = new Vector2(12, 40);
            portraitRect.offsetMax = new Vector2(-12, -12);

            // 「解鎖了但還沒點開過」的角標。壓在格子右上角，不擋立繪。
            slot.newBadge = CreateText(slot.root.transform, "New", "NEW", 18, FontStyle.Bold, TextAnchor.UpperRight);
            slot.newBadge.color = NewBadgeColor;
            slot.newBadge.raycastTarget = false;
            var badgeRect = slot.newBadge.rectTransform;
            badgeRect.anchorMin = Vector2.zero;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.offsetMin = new Vector2(0f, 0f);
            badgeRect.offsetMax = new Vector2(-8f, -6f);
            slot.newBadge.gameObject.SetActive(false);

            slot.label = CreateText(slot.root.transform, "Name", string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            // 名字可能很長（例如「黑黑的（開始有點人形）」），讓它自己縮小塞進格子裡
            slot.label.horizontalOverflow = HorizontalWrapMode.Wrap;
            slot.label.resizeTextForBestFit = true;
            slot.label.resizeTextMinSize = 10;
            slot.label.resizeTextMaxSize = 22;
            var labelRect = slot.label.rectTransform;
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.offsetMin = new Vector2(6, 8);
            labelRect.offsetMax = new Vector2(-6, 36);

            return slot;
        }

        GameObject BuildPager ()
        {
            var pager = NewUIObject("MonsterPager", transform);

            var layout = pager.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var element = pager.AddComponent<LayoutElement>();
            element.minHeight = 54;
            element.preferredHeight = 54;

            prevPageButton = BuildPagerButton(pager.transform, "PrevPage", "◀", () => SelectPage(currentPage - 1));

            pageLabel = CreateText(pager.transform, "PageLabel", "1 / 1", 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            var labelElement = pageLabel.gameObject.AddComponent<LayoutElement>();
            labelElement.minWidth = 140;
            labelElement.preferredWidth = 140;

            nextPageButton = BuildPagerButton(pager.transform, "NextPage", "▶", () => SelectPage(currentPage + 1));

            return pager;
        }

        Button BuildPagerButton (Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUIObject(name, parent);

            var image = go.AddComponent<Image>();
            image.color = TabNormalColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var element = go.AddComponent<LayoutElement>();
            element.minWidth = 70;
            element.preferredWidth = 70;

            var text = CreateText(go.transform, "Label", label, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);

            return button;
        }

        // ===================== 行為 =====================

        void SelectTab (bool codex)
        {
            codexActive = codex;

            foreach (var go in cgObjects)
                if (go != null) go.SetActive(!codex);

            if (monsterGrid != null) monsterGrid.SetActive(codex);
            if (monsterPager != null) monsterPager.SetActive(codex);

            if (cgTabImage != null) cgTabImage.color = codex ? TabNormalColor : TabSelectedColor;
            if (codexTabImage != null) codexTabImage.color = codex ? TabSelectedColor : TabNormalColor;
            if (cgTabText != null) cgTabText.color = codex ? TabNormalTextColor : TabSelectedTextColor;
            if (codexTabText != null) codexTabText.color = codex ? TabSelectedTextColor : TabNormalTextColor;

            // 注意：這裡不去碰 MonsterCodex.AllMobs。CGGalleryUI 是遊戲一開機
            // Naninovel 初始化時就建好的，先讀清單等於把 26 張怪物立繪常駐在記憶體裡。
            // 所以清單留到玩家真的點進圖鑑分頁才載。
            if (codex)
            {
                currentPage = Mathf.Clamp(currentPage, 1, PageCount);
                Refresh();
            }
            else if (overlay != null) overlay.Hide();
        }

        void RefreshCodexTabLabel ()
        {
            if (codexTabLabel == null) return;
            codexTabLabel.text = $"怪物圖鑑  {MonsterCodex.SeenCount} / {MonsterCodex.AllMobs.Count}";
        }

        void SelectPage (int page)
        {
            var clamped = Mathf.Clamp(page, 1, PageCount);
            if (clamped == currentPage) return;
            currentPage = clamped;
            Refresh();
        }

        void Refresh ()
        {
            var mobs = MonsterCodex.AllMobs;
            var offset = (currentPage - 1) * ItemsPerPage;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var index = offset + i;

                // 最後一頁不夠填滿時，格子留著但清空。直接 SetActive(false) 的話
                // GridLayoutGroup 會少算一列，整個面板高度會跳動。
                if (index >= mobs.Count)
                {
                    slot.mob = null;
                    slot.back.color = Color.clear;
                    slot.portrait.enabled = false;
                    slot.label.text = string.Empty;
                    if (slot.newBadge != null) slot.newBadge.gameObject.SetActive(false);
                    continue;
                }

                slot.mob = mobs[index];

                var unlocked = MonsterCodex.IsSeen(slot.mob);

                slot.back.color = SlotBackColor;
                slot.portrait.sprite = slot.mob.sprite;
                slot.portrait.enabled = slot.mob.sprite != null;
                slot.portrait.color = unlocked ? Color.white : LockedTint;

                slot.label.text = unlocked ? DisplayNameOf(slot.mob) : "???";
                slot.label.color = unlocked ? Color.white : TabNormalTextColor;

                if (slot.newBadge != null)
                    slot.newBadge.gameObject.SetActive(
                        unlocked && NewItemTracker.IsNewThisVisit(NewItemTracker.Monsters, slot.mob.name));
            }

            if (pageLabel != null) pageLabel.text = $"{currentPage} / {PageCount}";
            if (prevPageButton != null) prevPageButton.interactable = currentPage > 1;
            if (nextPageButton != null) nextPageButton.interactable = currentPage < PageCount;

            RefreshCodexTabLabel();
        }

        static string DisplayNameOf (MobData mob)
        {
            return string.IsNullOrEmpty(mob.displayName) ? mob.name : mob.displayName;
        }

        void OnSlotClicked (Slot slot)
        {
            if (!codexActive || slot.mob == null) return;
            if (!MonsterCodex.IsSeen(slot.mob)) return;
            if (overlay == null) return;

            overlay.Show(slot.mob.sprite, DisplayNameOf(slot.mob));

        }

        // ===================== 小工具 =====================

        internal static GameObject NewUIObject (string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static void Stretch (RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Text CreateText (Transform parent, string name, string content, int size, FontStyle style, TextAnchor anchor)
            => CreateText(parent, name, content, size, style, anchor, font);

        internal static Text CreateText (Transform parent, string name, string content, int size,
                                         FontStyle style, TextAnchor anchor, Font font)
        {
            var go = NewUIObject(name, parent);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }
    }
}
