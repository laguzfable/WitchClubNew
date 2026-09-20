using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UILayoutKit;

/// <summary>
/// 把設定畫面（SettingsUI2.prefab，EditorResources 登記的是這份）排成跟存讀檔同一套。
///
/// 不分頁，一頁全部列出（原本的設計就是這樣，分頁列是關著的）：
///   左欄「基本」＋「聲音」，右欄「文字」＋調文字速度時的預覽框。
/// 每一區上面的小標題是複製分頁按鈕上的字，所以會跟著語言換。
///
/// 除了標題和滑桿的拉桿（Sprites/UI/Settings/ 底下），其餘都拿存讀檔現有的素材拼：
/// 底圖、返回鈕；下拉選單的箭頭用翻頁箭頭轉 90 度。
/// 其餘（滑桿軌道、下拉框、選單底色）是從同一套配色取的純色。
///
/// ★ 只跑一次 ★
/// 排好之後在 Editor 裡手調的東西，再跑一次會被蓋掉。座標以 1920×1080、左上角為原點。
/// </summary>
public static class SettingsLayoutBuilder
{
    const string UIPrefab = "Assets/Naninovel/Prefabs/DefaultUI/SettingsUI2.prefab";

    // 兩欄，各 740 寬、中間隔 100，整體置中。一列一個選項：左邊名稱、右邊控制項。
    const float ColW = 740, ColGap = 100;
    const float LeftX = (1920 - ColW * 2 - ColGap) / 2, RightX = LeftX + ColW + ColGap;
    const float TopY = 190, SectionGap = 36;
    const float HeaderH = 44, RowH = 52, RowGap = 12;
    const float LabelW = 280, ControlW = 440, ControlH = 46;

    static readonly Color Paper = new Color32(246, 238, 222, 255);
    static readonly Color PaperDark = new Color32(232, 220, 198, 255);
    /// <summary>深咖啡：滑桿的進度條、下拉選單滑過／選中的底色。跟墨色同一系。</summary>
    static readonly Color Brown = new Color32(72, 44, 28, 255);
    static readonly Color Track = new Color32(120, 95, 80, 90);

    // 各頁的選項順序（由上到下）。名字是 prefab 裡那一列的物件名。
    static readonly string[] BasicRows = { "LocalePanel", "ScreenModePanel", "ResolutionPanel", "GraphicsPanel" };
    // 字型那一列（FontPanel）原本的 prefab 就是關著的，不列進來。
    static readonly string[] TextRows = { "MessageSpeedPanel", "AutoDelayPanel", "SkipModePanel", "SkipSeenTutorialsPanel", "FontSizePanel" };
    static readonly string[] SoundRows = { "MasterVolumePanel", "BgmVolumePanel", "SfxVolumePanel" };

    // 遊戲目前沒有語音，這兩列先藏起來（不刪，之後有語音再打開，把名字加回上面的清單）。
    static readonly string[] HiddenRows = { "VoiceLocalePanel", "VoiceVolumePanel" };

    [MenuItem("Tools/Witch Club/設定/照存讀檔的樣式排版（只跑一次）")]
    static void Build ()
    {
        if (!EditorUtility.DisplayDialog("設定畫面排版",
                "會重新擺 SettingsUI2 這個 prefab 的位置和樣式。\n\n" +
                "之前在 Editor 裡手調過的位置會被蓋掉，確定要跑嗎？", "排版", "取消"))
            return;

        ImportSprites(SharedSpriteDir);
        ImportSprites(SettingsSpriteDir);
        SetSliceBorder(SettingsSpriteDir + "settings_dropdown_list.png", ListCorner);

        var root = PrefabUtility.LoadPrefabContents(UIPrefab);
        try
        {
            BuildMenu(root);
            PrefabUtility.SaveAsPrefabAsset(root, UIPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[SettingsLayoutBuilder] 設定畫面排版完成");
    }

    static void BuildMenu (GameObject root)
    {
        var background = root.transform.Find("Background").GetComponent<Image>();
        background.sprite = LoadSprite("saveload_bg");
        background.color = Color.white;
        background.type = Image.Type.Simple;

        // Content 原本是直排自動排版（已經關掉了），改成撐滿畫面照座標擺。
        var content = (RectTransform)root.transform.Find("Content");
        RemoveComponent<VerticalLayoutGroup>(content);
        RemoveComponent<ContentSizeFitter>(content);
        Stretch(content);

        // 不分頁（原本的設計），分頁列維持關著；只借它按鈕上的字當小標題。
        var nav = content.Find("NavigationPanel");
        Text Heading (string button) => nav ? nav.Find(button)?.GetComponentInChildren<Text>(true) : null;

        // 標題：跟存讀檔、回憶模式一樣是一張圖（字畫在圖上）。
        var title = NewImage("Title", content, LoadSprite("settings_title"), false);
        title.transform.SetAsFirstSibling();
        Place(title.rectTransform, 620, 40, 680, 110);

        var basicH = BuildPanel(content.Find("BasicPanel"), BasicRows, Heading("BasicButton"), LeftX, TopY);
        BuildPanel(content.Find("SoundPanel"), SoundRows, Heading("SoundButton"), LeftX, TopY + basicH + SectionGap);
        var textH = BuildPanel(content.Find("TextPanel"), TextRows, Heading("TextButton"), RightX, TopY);

        // 畫質：Naninovel 原本列出 6 個 Unity 畫質等級，換成只有「一般／省效能」兩個（見 GraphicsPresetDropdown）。
        var graphics = content.Find("BasicPanel/GraphicsPanel");
        var graphicsDropdown = graphics ? graphics.GetComponentInChildren<Dropdown>(true) : null;
        if (graphicsDropdown)
        {
            RemoveComponent<Naninovel.UI.GameSettingsGraphicsDropdown>(graphicsDropdown.transform);
            GetOrAdd<Hexe.UI.GraphicsPresetDropdown>(graphicsDropdown.gameObject);
        }

        // 返回
        var returnPanel = (RectTransform)content.Find("ReturnPanel");
        Place(returnPanel, 1522, 947, 320, 80);
        var returnPanelImage = returnPanel.GetComponent<Image>();
        if (returnPanelImage) returnPanelImage.enabled = false;
        StyleReturnButton(returnPanel.Find("ReturnButton"));

        // 調文字速度時跳出來的預覽框：放在右欄「文字」底下，不要蓋到左欄。
        var preview = root.transform.Find("PreviewPrinter") as RectTransform;
        if (preview) Place(preview, RightX + (ColW - 600) / 2, TopY + textH + SectionGap, 600, 260);
    }

    // ─── 一頁 ────────────────────────────────────────────────────

    /// <returns>這一區排完的高度。</returns>
    static float BuildPanel (Transform panelTransform, string[] rows, Text heading, float x, float y)
    {
        if (!panelTransform) return 0;
        var panel = (RectTransform)panelTransform;

        var height = HeaderH + rows.Length * (RowH + RowGap);
        RemoveComponent<ContentSizeFitter>(panel);
        Place(panel, x, y, ColW, height);
        var panelImage = panel.GetComponent<Image>();
        if (panelImage) panelImage.enabled = false;

        // 原本的直排有的開有的關、位置也亂，統一交給它排。
        var layout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
        layout.enabled = true;
        layout.padding = new RectOffset();
        layout.spacing = RowGap;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        BuildHeader(panel, heading);

        foreach (var name in HiddenRows)
        {
            var hidden = panel.Find(name);
            if (hidden) hidden.gameObject.SetActive(false);
        }

        for (int i = 0; i < rows.Length; i++)
        {
            var row = panel.Find(rows[i]);
            if (!row) { Debug.LogWarning($"[SettingsLayoutBuilder] {panel.name} 底下找不到 {rows[i]}"); continue; }
            row.SetSiblingIndex(i + 1);
            BuildRow((RectTransform)row);
        }
        return height;
    }

    /// <summary>
    /// 小標題 + 底線。字是複製分頁按鈕上的那個 Text（連同 ManagedTextProvider），
    /// Instantiate 會把它的事件目標換成複製出來的 Text，所以換語言時小標題也會跟著換。
    /// </summary>
    static void BuildHeader (RectTransform panel, Text heading)
    {
        var header = NewRect("Header", panel);
        header.SetSiblingIndex(0);
        var element = header.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = element.minHeight = HeaderH;

        Text text;
        if (heading)
        {
            text = Object.Instantiate(heading, header, false);
            text.name = "Label";
        }
        else text = NewText("Label", header, null, 30, TextAnchor.MiddleLeft, panel.name);
        Stretch(text.rectTransform);
        text.color = Ink;
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;

        var line = NewImage("Underline", header, null, false);
        line.color = new Color(InkLight.r, InkLight.g, InkLight.b, 0.5f);
        var rt = line.rectTransform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 2);
    }

    /// <summary>一列：左邊名稱、右邊滑桿或下拉選單。</summary>
    static void BuildRow (RectTransform row)
    {
        var element = GetOrAdd<LayoutElement>(row.gameObject);
        element.preferredHeight = element.minHeight = RowH;
        var rowImage = row.GetComponent<Image>();
        if (rowImage) rowImage.enabled = false;

        foreach (RectTransform child in row)
        {
            var slider = child.GetComponent<Slider>();
            var dropdown = child.GetComponent<Dropdown>();
            if (slider || dropdown)
            {
                child.anchorMin = child.anchorMax = child.pivot = new Vector2(1, 0.5f);
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = new Vector2(ControlW, ControlH);
                if (slider) StyleSlider(slider);
                if (dropdown) StyleDropdown(dropdown);
                continue;
            }

            var label = child.GetComponent<Text>();
            if (!label) continue;
            child.anchorMin = child.anchorMax = child.pivot = new Vector2(0, 0.5f);
            child.anchoredPosition = Vector2.zero;
            child.sizeDelta = new Vector2(LabelW, RowH);
            label.color = Ink;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleLeft;
        }
    }

    // ─── 控制項 ──────────────────────────────────────────────────

    /// <summary>細軌道 + 咖啡色進度 + 拉桿（settings_slider_handle）。</summary>
    static void StyleSlider (Slider slider)
    {
        var t = slider.transform;

        // 拉桿高度＝滑桿高度，寬度照圖的比例算，圖畫成什麼形狀都不會被壓扁。
        // 進度條和拉桿的可動範圍左右各縮半個拉桿寬，拉到底時拉桿才不會超出軌道。
        var handleSprite = LoadSprite("settings_slider_handle");
        var handleW = ControlH * (handleSprite ? handleSprite.rect.width / handleSprite.rect.height : 1f);
        var half = handleW / 2;

        // 整條滑桿墊一層透明的點擊區：軌道只有細細一條，沒有這層的話要剛好點在線上或拉桿上才有反應，
        // 玩家會以為只能拖。Slider 本身就會把點下去的位置換算成數值，只是要先點得到它。
        var hit = GetOrAdd<Image>(slider.gameObject);
        hit.sprite = null;
        hit.color = Color.clear;
        hit.raycastTarget = true;
        var track = t.Find("Background");
        if (track)
        {
            var rt = (RectTransform)track;
            rt.anchorMin = new Vector2(0, 0.44f);
            rt.anchorMax = new Vector2(1, 0.56f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = track.GetComponent<Image>();
            image.sprite = null;
            image.color = Track;
        }

        var fillArea = t.Find("Fill Area") as RectTransform;
        if (fillArea)
        {
            fillArea.anchorMin = new Vector2(0, 0.44f);
            fillArea.anchorMax = new Vector2(1, 0.56f);
            fillArea.offsetMin = new Vector2(0, 0);
            fillArea.offsetMax = new Vector2(-half, 0);
            var fill = fillArea.Find("Fill");
            if (fill)
            {
                var image = fill.GetComponent<Image>();
                image.sprite = null;
                image.color = Brown;
                ((RectTransform)fill).sizeDelta = new Vector2(half, 0);
            }
        }

        var handleArea = t.Find("Handle Slide Area") as RectTransform;
        if (handleArea)
        {
            handleArea.offsetMin = new Vector2(half, 0);
            handleArea.offsetMax = new Vector2(-half, 0);
            var handle = handleArea.Find("Handle") as RectTransform;
            if (handle)
            {
                handle.sizeDelta = new Vector2(handleW, 0);
                var image = handle.GetComponent<Image>();
                image.sprite = handleSprite;
                image.color = Color.white;
                image.preserveAspect = true;
                slider.targetGraphic = image;
            }
        }

        SetPaperTint(slider);
    }

    /// <summary>紙色方框 + 朝下的箭頭；展開的選單是淺紙色，滑過去的那項染一點深咖啡，選中那項前面有 settings_check。</summary>
    static void StyleDropdown (Dropdown dropdown)
    {
        var t = dropdown.transform;

        // 方框：有圖用圖（settings_dropdown，440×46 的比例），沒圖才用紙色加細邊框。
        var box = dropdown.GetComponent<Image>();
        var boxSprite = LoadSprite("settings_dropdown");
        box.sprite = boxSprite;
        box.type = Image.Type.Simple;
        box.color = boxSprite ? Color.white : PaperDark;
        if (boxSprite) RemoveComponent<Outline>(box.transform);
        else AddBorder(box.gameObject);
        SetPaperTint(dropdown);

        var label = t.Find("Label");
        if (label)
        {
            var rt = (RectTransform)label;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20, 0);
            rt.offsetMax = new Vector2(-56, 0);
            var text = label.GetComponent<Text>();
            text.color = Ink;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
        }

        var arrow = t.Find("Arrow") as RectTransform;
        if (arrow)
        {
            arrow.anchorMin = arrow.anchorMax = new Vector2(1, 0.5f);
            arrow.anchoredPosition = new Vector2(-28, 0);
            arrow.sizeDelta = new Vector2(20, 30);
            arrow.localEulerAngles = new Vector3(0, 0, -90);   // 箭頭圖是朝右的
            var image = arrow.GetComponent<Image>();
            image.sprite = LoadSprite("saveload_arrow");
            image.color = Color.white;
            image.preserveAspect = true;
        }

        var template = t.Find("Template") as RectTransform;
        if (!template) return;
        template.sizeDelta = new Vector2(0, 240);
        // 展開的選單底：高度跟選項數量走，所以用九宮格拉（settings_dropdown_list，邊角在匯入設定裡）。
        var templateImage = template.GetComponent<Image>();
        var listSprite = LoadSprite("settings_dropdown_list");
        templateImage.sprite = listSprite;
        templateImage.color = listSprite ? Color.white : Paper;
        if (listSprite)
        {
            templateImage.type = Image.Type.Sliced;
            templateImage.pixelsPerUnitMultiplier = 2;   // 圖是兩倍大畫的，邊角要縮回一半
            RemoveComponent<Outline>(template);
        }
        else AddBorder(templateImage.gameObject);

        var item = template.Find("Viewport/Content/Item");
        if (item)
        {
            ((RectTransform)item).sizeDelta = new Vector2(0, 44);
            var content = (RectTransform)template.Find("Viewport/Content");
            content.sizeDelta = new Vector2(0, 44);

            var itemBack = item.Find("Item Background")?.GetComponent<Image>();
            if (itemBack) { itemBack.sprite = null; itemBack.color = Color.white; }

            var toggle = item.GetComponent<Toggle>();
            if (toggle)
            {
                // 底圖是白的，靠 ColorTint 乘出顏色：平常透明，滑過去一層淡淡的深咖啡。
                toggle.transition = Selectable.Transition.ColorTint;
                toggle.targetGraphic = itemBack;
                var colors = toggle.colors;
                colors.normalColor = new Color(1, 1, 1, 0);
                colors.highlightedColor = new Color(Brown.r, Brown.g, Brown.b, 0.18f);
                colors.pressedColor = new Color(Brown.r, Brown.g, Brown.b, 0.3f);
                colors.selectedColor = new Color(Brown.r, Brown.g, Brown.b, 0.12f);
                colors.colorMultiplier = 1;
                toggle.colors = colors;
            }

            var check = item.Find("Item Checkmark") as RectTransform;
            if (check)
            {
                check.anchoredPosition = new Vector2(20, 0);
                check.sizeDelta = new Vector2(18, 18);
                var image = check.GetComponent<Image>();
                image.sprite = LoadSprite("settings_check");
                image.color = Color.white;
                image.preserveAspect = true;
            }

            var itemLabel = item.Find("Item Label");
            if (itemLabel)
            {
                var rt = (RectTransform)itemLabel;
                rt.offsetMin = new Vector2(40, 0);
                rt.offsetMax = new Vector2(-10, 0);
                var text = itemLabel.GetComponent<Text>();
                text.color = Ink;
                text.fontSize = 22;
                text.alignment = TextAnchor.MiddleLeft;
            }
        }

        var scrollbar = template.Find("Scrollbar");
        if (scrollbar)
        {
            var image = scrollbar.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(Track.r, Track.g, Track.b, 0.25f);
            var handle = scrollbar.Find("Sliding Area/Handle")?.GetComponent<Image>();
            if (handle) { handle.sprite = null; handle.color = Track; }
        }
    }

    /// <summary>選單底的九宮格邊角寬度（兩倍大的圖上的像素）。示意圖上框的就是這個範圍。</summary>
    const int ListCorner = 32;

    /// <summary>設九宮格的邊角。只在還沒設過時設，你在 Sprite Editor 裡自己調過的不會被蓋掉。</summary>
    static void SetSliceBorder (string path, int corner)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
        if (importer.spriteBorder != Vector4.zero) return;
        importer.spriteBorder = new Vector4(corner, corner, corner, corner);
        importer.SaveAndReimport();
    }

    /// <summary>細細的墨色邊框，跟紙色底分得開。</summary>
    static void AddBorder (GameObject go)
    {
        var outline = GetOrAdd<Outline>(go);
        outline.effectColor = new Color(InkLight.r, InkLight.g, InkLight.b, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
