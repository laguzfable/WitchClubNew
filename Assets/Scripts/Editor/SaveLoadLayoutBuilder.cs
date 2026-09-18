using Hexe.UI;
using Naninovel.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UILayoutKit;

/// <summary>
/// 把存讀檔畫面照設計圖排好（SaveLoadUI.prefab + SaveLoadSlot.prefab）。
///
/// ★ 為什麼用工具排，不直接改 prefab 檔 ★
/// 翻頁列是巢狀的 PaginationPanel.prefab（別的畫面也在用），要加新物件、換元件，
/// 手改 YAML 很容易壞。走 Unity 的 API 比較穩。
///
/// ★ 只跑一次 ★
/// 這支會照設計圖的座標重新擺位置。排好之後在 Editor 裡手調的東西，再跑一次會被蓋掉。
///
/// 座標以 1920×1080 的畫面、左上角為原點。回憶模式的排版見 GalleryLayoutBuilder，共用的小工具在 UILayoutKit。
/// </summary>
public static class SaveLoadLayoutBuilder
{
    const string UIPrefab = "Assets/Naninovel/Prefabs/DefaultUI/SaveLoadUI.prefab";
    const string SlotPrefab = "Assets/Naninovel/Prefabs/DefaultUI/SaveLoadSlot.prefab";

    // 格子尺寸。改這裡就好，格子內的東西都從這兩個數字推算。
    const float SlotW = 400, SlotH = 220;
    const int Columns = 3;
    const float Gap = 24;
    public const float GridW = SlotW * Columns + Gap * (Columns - 1);
    public const float GridX = (1920 - GridW) / 2;

    [MenuItem("Tools/Witch Club/存讀檔/照設計圖排版（只跑一次）")]
    static void Build ()
    {
        if (!EditorUtility.DisplayDialog("存讀檔排版",
                "會照設計圖重新擺 SaveLoadUI 和 SaveLoadSlot 兩個 prefab 的位置。\n\n" +
                "之前在 Editor 裡手調過的位置會被蓋掉，確定要跑嗎？", "排版", "取消"))
            return;

        ImportSprites(SharedSpriteDir);
        BuildSlot();
        BuildMenu();
        AssetDatabase.SaveAssets();
        Debug.Log("[SaveLoadLayoutBuilder] 存讀檔畫面排版完成");
    }

    // ─── 格子 ────────────────────────────────────────────────────

    static void BuildSlot ()
    {
        var root = PrefabUtility.LoadPrefabContents(SlotPrefab);
        try
        {
            var slot = root.GetComponent<HexeGameStateSlot>();
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(SlotW, SlotH);

            var font = FindFont(root.transform);
            var oldTitle = root.transform.Find("TitleText");
            if (oldTitle) Object.DestroyImmediate(oldTitle.gameObject);

            var thumb = root.transform.Find("ThumbnailImage").GetComponent<RawImage>();
            thumb.raycastTarget = false;
            PlaceTopLeft(thumb.rectTransform, 12, 12, SlotW - 24, 126);

            var emptyArt = NewImage("EmptyArt", root.transform, LoadSprite("saveload_empty_01"), false);
            PlaceTopLeft(emptyArt.rectTransform, 12, 12, SlotW - 24, 126);

            var frame = NewImage("Frame", root.transform, LoadSprite("saveload_slot"), true);
            Stretch(frame.rectTransform);

            var selected = NewImage("SelectedFrame", root.transform, LoadSprite("saveload_slot_on"), false);
            Center(selected.rectTransform, SlotW + 20, SlotH + 20);

            var number = NewText("NumberText", root.transform, font, 28, TextAnchor.MiddleLeft, "01");
            PlaceTopLeft(number.rectTransform, 16, 142, 48, 40);

            var divider = NewImage("Divider", root.transform, null, false);
            divider.color = InkLight;
            PlaceTopLeft(divider.rectTransform, 69, 150, 2, 24);

            var chapter = NewText("ChapterText", root.transform, font, 22, TextAnchor.MiddleLeft, "第一章");
            PlaceTopLeft(chapter.rectTransform, 82, 142, 130, 40);

            var date = NewText("DateText", root.transform, font, 20, TextAnchor.MiddleRight, "09/17  14:30");
            PlaceTopLeft(date.rectTransform, SlotW - 16 - 180, 142, 180, 40);

            var detail = NewText("DetailText", root.transform, font, 18, TextAnchor.MiddleLeft, "旅人・第2周目");
            PlaceTopLeft(detail.rectTransform, 82, 180, SlotW - 82 - 16, 30);

            // 刪除鈕要蓋在最上面。
            var delete = root.transform.Find("DeleteSlotButton");
            var drt = (RectTransform)delete;
            drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(1, 1);
            drt.anchoredPosition = new Vector2(-6, -6);
            drt.sizeDelta = new Vector2(36, 36);
            var deleteImage = delete.GetComponent<Image>();
            deleteImage.sprite = LoadSprite("saveload_delete");
            deleteImage.color = Color.white;
            var icon = delete.Find("Icon");
            if (icon) icon.gameObject.SetActive(false);
            delete.SetAsLastSibling();

            // 整格都能點：點擊判定交給外框。
            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = frame;

            var so = new SerializedObject(slot);
            so.FindProperty("hoverOpacityFade").floatValue = 0;
            so.FindProperty("onTitleTextChanged.m_PersistentCalls.m_Calls").arraySize = 0;
            so.FindProperty("numberText").objectReferenceValue = number;
            so.FindProperty("chapterText").objectReferenceValue = chapter;
            so.FindProperty("dateText").objectReferenceValue = date;
            so.FindProperty("detailText").objectReferenceValue = detail;
            so.FindProperty("emptyArt").objectReferenceValue = emptyArt;
            so.FindProperty("selectedFrame").objectReferenceValue = selected.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SlotPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ─── 整個畫面 ────────────────────────────────────────────────

    static void BuildMenu ()
    {
        var root = PrefabUtility.LoadPrefabContents(UIPrefab);
        try
        {
            var background = root.transform.Find("Background").GetComponent<Image>();
            background.sprite = LoadSprite("saveload_bg");
            background.color = Color.white;
            background.type = Image.Type.Simple;

            // Content 原本是直排自動排版，改成照座標擺。
            var content = root.transform.Find("Content");
            RemoveComponent<VerticalLayoutGroup>(content);
            RemoveComponent<ContentSizeFitter>(content);

            var font = FindFont(root.transform);

            // 標題
            var title = NewImage("Title", content, LoadSprite("saveload_title_load"), false);
            title.transform.SetAsFirstSibling();
            Place(title.rectTransform, 620, 40, 680, 110);
            var header = GetOrAdd<SaveLoadHeader>(root);
            var hso = new SerializedObject(header);
            hso.FindProperty("menu").objectReferenceValue = root.GetComponent<SaveLoadMenu>();
            hso.FindProperty("title").objectReferenceValue = title;
            hso.FindProperty("loadTitle").objectReferenceValue = LoadSprite("saveload_title_load");
            hso.FindProperty("saveTitle").objectReferenceValue = LoadSprite("saveload_title_save");
            hso.ApplyModifiedPropertiesWithoutUndo();

            // 分頁：存檔模式只剩一顆，靠橫排自動置中。
            var nav = (RectTransform)content.Find("NavigationPanel");
            StyleTabBar(nav);
            foreach (var name in new[] { "QuickLoadButton", "LoadButton", "SaveButton" })
                StyleTab(nav.Find(name));

            foreach (var name in new[] { "SavePanel", "LoadPanel", "QuickLoadPanel" })
                BuildPanel((RectTransform)content.Find(name), font);

            // 返回
            var returnPanel = (RectTransform)content.Find("ReturnPanel");
            Place(returnPanel, 1522, 947, 320, 80);
            var returnPanelImage = returnPanel.GetComponent<Image>();
            if (returnPanelImage) returnPanelImage.enabled = false;
            StyleReturnButton(returnPanel.Find("ReturnButton"));

            PrefabUtility.SaveAsPrefabAsset(root, UIPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BuildPanel (RectTransform panel, Font font)
    {
        if (!panel) return;

        RemoveComponent<VerticalLayoutGroup>(panel);
        RemoveComponent<ContentSizeFitter>(panel);
        Stretch(panel);
        var panelImage = panel.GetComponent<Image>();
        if (panelImage) panelImage.enabled = false;

        var grid = panel.GetComponentInChildren<GameStateSlotsGrid>(true);
        Place((RectTransform)grid.transform, GridX, 232, GridW, 700);
        var layout = grid.GetComponent<GridLayoutGroup>();
        layout.padding = new RectOffset();
        layout.cellSize = new Vector2(SlotW, SlotH);
        layout.spacing = new Vector2(Gap, 20);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Columns;
        var gso = new SerializedObject(grid);
        gso.FindProperty("itemsPerPage").intValue = 9;
        var pagination = gso.FindProperty("paginationPanel").objectReferenceValue as GameObject;
        gso.ApplyModifiedPropertiesWithoutUndo();

        if (pagination) StylePagination((RectTransform)pagination.transform, grid, font, 955);
    }
}
