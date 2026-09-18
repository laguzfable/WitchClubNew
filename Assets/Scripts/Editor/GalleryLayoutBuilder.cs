using Hexe.UI;
using Naninovel;
using Naninovel.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UILayoutKit;

/// <summary>
/// 把回憶模式（CGGalleryUI.prefab + CGGallerySlot.prefab）排成跟存讀檔同一套：
/// 同一張底圖、標題、分頁紙條、‹ 1 2 3 4 5 › 翻頁列、右下返回鈕。
///
/// 怪物圖鑑以前是 MonsterCodexPanel 在執行時用程式畫出來的，現在物件都建進 prefab，
/// 行為交給 MonsterCodexView。
///
/// ★ 只跑一次 ★
/// 排好之後在 Editor 裡手調的東西，再跑一次會被蓋掉。座標以 1920×1080、左上角為原點。
/// </summary>
public static class GalleryLayoutBuilder
{
    const string UIPrefab = "Assets/Naninovel/Prefabs/DefaultUI/CGGalleryUI.prefab";
    const string SlotPrefab = "Assets/Naninovel/Prefabs/DefaultUI/CGGallerySlot.prefab";

    // 格子區跟存讀檔同寬、同位置。
    const float GridX = SaveLoadLayoutBuilder.GridX, GridW = SaveLoadLayoutBuilder.GridW;
    const float GridY = 232, GridH = 728;
    const float PagerY = 963;

    // CG：3×3，縮圖 16:9（376×212），四周留 12 給外框。
    const float CGW = 400, CGH = 236;
    const int CGColumns = 3;
    static readonly Vector2 CGGap = new Vector2(24, 10);

    // 怪物：跟 CG 同一個版型（3×3、同一套外框），立繪等比縮進 376×212 的圖框，名字壓在圖框下緣。
    const float MobW = CGW, MobH = CGH;
    const int MobColumns = CGColumns, MobRows = 3;
    static readonly Vector2 MobGap = CGGap;

    static readonly Color NewBadgeColor = new Color(0.85f, 0.6f, 0.2f);

    [MenuItem("Tools/Witch Club/回憶模式/照存讀檔的樣式排版（只跑一次）")]
    static void Build ()
    {
        if (!EditorUtility.DisplayDialog("回憶模式排版",
                "會重新擺 CGGalleryUI 和 CGGallerySlot 兩個 prefab，並把怪物圖鑑建進 prefab。\n\n" +
                "之前在 Editor 裡手調過的位置會被蓋掉，確定要跑嗎？", "排版", "取消"))
            return;

        ImportSprites(SharedSpriteDir);
        ImportSprites(GallerySpriteDir);
        BuildCGSlot();
        BuildGallery();
        AssetDatabase.SaveAssets();
        Debug.Log("[GalleryLayoutBuilder] 回憶模式排版完成");
    }

    // ─── CG 格子 ─────────────────────────────────────────────────

    static void BuildCGSlot ()
    {
        var root = PrefabUtility.LoadPrefabContents(SlotPrefab);
        try
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(CGW, CGH);

            var thumb = root.transform.Find("ThumbnailImage").GetComponent<RawImage>();
            thumb.raycastTarget = false;
            PlaceTopLeft(thumb.rectTransform, 12, 12, CGW - 24, CGH - 24);

            var frame = NewImage("Frame", root.transform, LoadSprite("gallery_cg_slot"), true);
            Stretch(frame.rectTransform);

            var selected = NewImage("SelectedFrame", root.transform, LoadSprite("gallery_cg_slot_on"), false);
            Center(selected.rectTransform, CGW + 20, CGH + 20);
            selected.gameObject.SetActive(false);
            SetRef(GetOrAdd<HoverFrame>(root), "frame", selected.gameObject);

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = frame;

            var so = new SerializedObject(root.GetComponent<CGGalleryGridSlot>());
            so.FindProperty("hoverOpacityFade").floatValue = 0;
            var locked = AssetDatabase.LoadAssetAtPath<Texture2D>(GallerySpriteDir + "gallery_cg_locked.png");
            if (locked) so.FindProperty("lockedTexture").objectReferenceValue = locked;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SlotPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ─── 整個畫面 ────────────────────────────────────────────────

    static void BuildGallery ()
    {
        var root = PrefabUtility.LoadPrefabContents(UIPrefab);
        try
        {
            var background = root.transform.Find("Background").GetComponent<Image>();
            background.sprite = LoadSprite("saveload_bg");
            background.color = Color.white;
            background.type = Image.Type.Simple;

            // BrowserPanel 原本是「置中、寬 600、直排自動排版」的小盒子，改成撐滿畫面照座標擺。
            var browser = (RectTransform)root.transform.Find("BrowserPanel");
            RemoveComponent<VerticalLayoutGroup>(browser);
            RemoveComponent<ContentSizeFitter>(browser);
            Stretch(browser);
            var browserImage = browser.GetComponent<Image>();
            if (browserImage) browserImage.enabled = false;

            var font = FindFont(root.transform);

            // 標題：原本的「回憶模式」字換成圖。
            var oldTitle = browser.Find("TitleLabel");
            if (oldTitle) oldTitle.gameObject.SetActive(false);
            var title = NewImage("Title", browser, LoadSprite("gallery_title"), false);
            Place(title.rectTransform, 620, 40, 680, 110);

            var space = browser.Find("Space");
            if (space) space.gameObject.SetActive(false);

            // 分頁
            var tabBar = NewRect("TabBar", browser);
            StyleTabBar(tabBar);
            var group = tabBar.gameObject.AddComponent<ToggleGroup>();
            group.allowSwitchOff = false;
            var cgTab = NewTab("CGTab", tabBar, group, font, "CG", true);
            var codexTab = NewTab("CodexTab", tabBar, group, font, "怪物圖鑑", false);

            // CG 格子
            var cgGrid = browser.GetComponentInChildren<CGGalleryGrid>(true);
            Place((RectTransform)cgGrid.transform, GridX, GridY, GridW, GridH);
            SetupGrid(cgGrid.GetComponent<GridLayoutGroup>(), new Vector2(CGW, CGH), CGGap, CGColumns);
            var gso = new SerializedObject(cgGrid);
            gso.FindProperty("itemsPerPage").intValue = 9;
            var cgPagination = gso.FindProperty("paginationPanel").objectReferenceValue as GameObject;
            gso.ApplyModifiedPropertiesWithoutUndo();
            if (cgPagination) StylePagination((RectTransform)cgPagination.transform, cgGrid, font, PagerY);

            // 怪物圖鑑
            var view = GetOrAdd<MonsterCodexView>(browser.gameObject);
            var mobGrid = NewRect("MonsterGrid", browser);
            Place(mobGrid, GridX, GridY, GridW, GridH);
            SetupGrid(mobGrid.gameObject.AddComponent<GridLayoutGroup>(), new Vector2(MobW, MobH), MobGap, MobColumns);
            var slots = new MonsterCodexSlot[MobColumns * MobRows];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = NewMonsterSlot(mobGrid, i, font);

            var mobPager = NewRect("MonsterPager", browser);
            var previous = NewPagerArrow("PreviousPageButton", mobPager);
            var next = NewPagerArrow("NextPageButton", mobPager);
            StylePagination(mobPager, view, font, PagerY);

            mobGrid.gameObject.SetActive(false);
            mobPager.gameObject.SetActive(false);

            // 返回
            var returnButton = (RectTransform)browser.Find("ReturnButton");
            Place(returnButton, 1522, 947, 320, 80);
            StyleReturnButton(returnButton);
            // 返回鈕底下有一顆測試用的「Button」，onClick 綁的 TestDirectLoad 目標是空的，按了沒反應。
            var debugButton = returnButton.Find("Button (1)");
            if (debugButton) debugButton.gameObject.SetActive(false);

            // 點怪物之後的放大檢視，掛在根節點才蓋得住整個畫面。
            var overlay = NewOverlay(root.transform, font);

            var so = new SerializedObject(view);
            so.FindProperty("cgTab").objectReferenceValue = cgTab;
            so.FindProperty("codexTab").objectReferenceValue = codexTab;
            so.FindProperty("codexTabLabel").objectReferenceValue = codexTab.GetComponentInChildren<Text>(true);
            SetArray(so.FindProperty("cgObjects"), cgGrid.gameObject, cgPagination);
            SetArray(so.FindProperty("codexObjects"), mobGrid.gameObject, mobPager.gameObject);
            SetArray(so.FindProperty("slots"), slots);
            so.FindProperty("previousPageButton").objectReferenceValue = previous;
            so.FindProperty("nextPageButton").objectReferenceValue = next;
            so.FindProperty("overlay").objectReferenceValue = overlay;
            so.ApplyModifiedPropertiesWithoutUndo();

            // CG 格子的 NEW 角標：以前是 MonsterCodexInjector 在執行時掛上去的。
            GetOrAdd<CGNewBadge>(browser.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, UIPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetupGrid (GridLayoutGroup layout, Vector2 cell, Vector2 gap, int columns)
    {
        layout.padding = new RectOffset();
        layout.cellSize = cell;
        layout.spacing = gap;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
    }

    static Toggle NewTab (string name, Transform bar, ToggleGroup group, Font font, string label, bool on)
    {
        var back = NewImage(name, bar, null, true);
        var toggle = back.gameObject.AddComponent<Toggle>();
        var onImage = NewImage("On", back.transform, null, false);
        var text = NewText("Label", back.transform, font, 26, TextAnchor.MiddleCenter, label);
        Stretch(text.rectTransform);
        toggle.targetGraphic = back;
        toggle.graphic = onImage;
        toggle.group = group;
        toggle.isOn = on;
        StyleTab(back.transform);
        return toggle;
    }

    static Button NewPagerArrow (string name, Transform pager)
    {
        var image = NewImage(name, pager, null, true);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        SetPaperTint(button);
        return button;
    }

    static MonsterCodexSlot NewMonsterSlot (Transform grid, int index, Font font)
    {
        var root = NewRect($"MonsterSlot{index}", grid);
        var slot = root.gameObject.AddComponent<MonsterCodexSlot>();

        // 內容全放在 Content 底下：最後一頁不夠填滿時只關這層，格子位置留著。
        var content = NewRect("Content", root);
        Stretch(content);

        var frame = NewImage("Frame", content, LoadSprite("gallery_cg_slot"), true);
        Stretch(frame.rectTransform);

        var portrait = NewImage("Portrait", content, null, false);
        portrait.preserveAspect = true;
        portrait.enabled = false;   // 沒有圖時 Image 會畫成白框；Bind 時才打開
        PlaceTopLeft(portrait.rectTransform, 12, 12, MobW - 24, MobH - 24);

        var name = NewText("Name", content, font, 22, TextAnchor.MiddleCenter, "???");
        PlaceTopLeft(name.rectTransform, 12, MobH - 12 - 36, MobW - 24, 32);
        // 字壓在立繪上，描一圈紙色的邊才看得清楚。
        var outline = name.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(250, 240, 222, 200);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        // 名字可能很長（例如「黑黑的（開始有點人形）」），讓它自己縮小塞進格子裡
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.verticalOverflow = VerticalWrapMode.Truncate;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 12;
        name.resizeTextMaxSize = 22;

        var badge = NewText("NewBadge", content, font, 18, TextAnchor.UpperRight, "NEW");
        badge.fontStyle = FontStyle.Bold;
        badge.color = NewBadgeColor;
        PlaceTopLeft(badge.rectTransform, MobW - 80, 14, 64, 24);
        badge.gameObject.SetActive(false);

        var selected = NewImage("SelectedFrame", content, LoadSprite("gallery_cg_slot_on"), false);
        Center(selected.rectTransform, MobW + 20, MobH + 20);
        selected.gameObject.SetActive(false);

        var button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = frame;
        SetRef(root.gameObject.AddComponent<HoverFrame>(), "frame", selected.gameObject);

        var so = new SerializedObject(slot);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("content").objectReferenceValue = content.gameObject;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("nameLabel").objectReferenceValue = name;
        so.FindProperty("newBadge").objectReferenceValue = badge.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    static MonsterDetailOverlay NewOverlay (Transform root, Font font)
    {
        // 最底層是黑色：背景圖還沒畫好（或是透明的）時才不會透出後面的格子。
        var back = NewImage("MonsterDetailOverlay", root, null, true);
        back.color = Color.black;
        Stretch(back.rectTransform);
        back.transform.SetAsLastSibling();
        var button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = back;
        button.transition = Selectable.Transition.None;

        var backdrop = NewImage("Background", back.transform, LoadSprite("gallery_monster_bg_01"), false);
        Stretch(backdrop.rectTransform);

        // 漂的是外層 Float，立繪在裡面撐滿。要調立繪大小位置就調 Float。
        var floater = NewRect("Float", back.transform);
        SetAnchors(floater, 0.2f, 0.15f, 0.8f, 0.8f);
        var portrait = NewImage("Portrait", floater, null, false);
        portrait.preserveAspect = true;
        Stretch(portrait.rectTransform);

        var quote = NewText("Quote", back.transform, font, 32, TextAnchor.MiddleCenter, "「……」");
        quote.color = Color.white;
        quote.horizontalOverflow = HorizontalWrapMode.Wrap;
        AddShadowOutline(quote);
        SetAnchors(quote.rectTransform, 0.12f, 0.82f, 0.88f, 0.95f);

        var name = NewText("Name", back.transform, font, 34, TextAnchor.MiddleCenter, "");
        name.fontStyle = FontStyle.Bold;
        name.color = Color.white;
        AddShadowOutline(name);
        SetAnchors(name.rectTransform, 0f, 0.07f, 1f, 0.14f);

        var hint = NewText("Hint", back.transform, font, 18, TextAnchor.MiddleCenter, "點擊任意處關閉");
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        AddShadowOutline(hint);
        SetAnchors(hint.rectTransform, 0f, 0.02f, 1f, 0.06f);

        var overlay = back.gameObject.AddComponent<MonsterDetailOverlay>();
        var so = new SerializedObject(overlay);
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("nameLabel").objectReferenceValue = name;
        so.FindProperty("quoteLabel").objectReferenceValue = quote;
        so.FindProperty("background").objectReferenceValue = backdrop;
        so.FindProperty("floatTarget").objectReferenceValue = floater;
        SetArray(so.FindProperty("backgrounds"),
            LoadSprite("gallery_monster_bg_01"), LoadSprite("gallery_monster_bg_02"), LoadSprite("gallery_monster_bg_03"));
        so.ApplyModifiedPropertiesWithoutUndo();

        back.gameObject.SetActive(false);
        return overlay;
    }

    /// <summary>字蓋在背景圖上，描一圈深色的邊，不管背景亮暗都看得清楚。</summary>
    static void AddShadowOutline (Text text)
    {
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.08f, 0.08f, 0.85f);
        outline.effectDistance = new Vector2(2, -2);
    }

    static void SetAnchors (RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetArray (SerializedProperty array, params Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
