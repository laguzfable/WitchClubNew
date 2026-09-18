using System.IO;
using Hexe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存讀檔、回憶模式兩支排版工具共用的小工具。
/// 座標一律以 1920×1080 的畫面、左上角為原點。
/// </summary>
public static class UILayoutKit
{
    /// <summary>兩個畫面共用的素材（底圖、分頁、箭頭、頁碼、返回）都在這裡。</summary>
    public const string SharedSpriteDir = "Assets/Sprites/UI/SaveLoad/";
    public const string GallerySpriteDir = "Assets/Sprites/UI/Gallery/";

    public static readonly Color Ink = new Color32(74, 52, 48, 255);
    public static readonly Color InkLight = new Color32(120, 95, 80, 255);

    // ─── 素材 ────────────────────────────────────────────────────

    /// <summary>資料夾裡的圖都設成 Sprite（Unity 預設是 Texture，Image 用不了）。</summary>
    public static void ImportSprites (string dir)
    {
        AssetDatabase.Refresh();
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir))
        {
            if (file.EndsWith(".meta")) continue;
            var path = file.Replace('\\', '/');
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    /// <summary>用檔名（不含副檔名）找圖，先找回憶模式專用的，再找共用的。</summary>
    public static Sprite LoadSprite (string name)
    {
        foreach (var dir in new[] { GallerySpriteDir, SharedSpriteDir })
        foreach (var ext in new[] { ".png", ".jpg" })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dir + name + ext);
            if (sprite) return sprite;
        }
        Debug.LogWarning($"[UILayoutKit] 找不到圖 {name}");
        return null;
    }

    // ─── 擺位置 ──────────────────────────────────────────────────

    /// <summary>1920×1080 左上角座標 → 以父物件中心為錨點的位置。父物件要撐滿畫面。</summary>
    public static void Place (RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w / 2 - 960, 540 - (y + h / 2));
    }

    /// <summary>以父物件左上角為原點擺位置（格子裡的東西用）。</summary>
    public static void PlaceTopLeft (RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    public static void Stretch (RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    /// <summary>置中、固定大小（選中外框這種會超出格子一點的東西用）。</summary>
    public static void Center (RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
    }

    // ─── 建物件 ──────────────────────────────────────────────────

    /// <summary>建一個空的 UI 物件。同名的舊物件會先刪掉，所以工具重跑不會越長越多。</summary>
    public static RectTransform NewRect (string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing) Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image NewImage (string name, Transform parent, Sprite sprite, bool raycast)
    {
        var rt = NewRect(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        return image;
    }

    public static Text NewText (string name, Transform parent, Font font, int size, TextAnchor align, string sample)
    {
        var rt = NewRect(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var text = rt.gameObject.AddComponent<Text>();
        if (font) text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = Ink;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = sample;
        return text;
    }

    public static Font FindFont (Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            if (text.font) return text.font;
        return null;
    }

    /// <summary>按鈕顏色：一般是原色、滑鼠移上去稍暗。Naninovel 預設的一般狀態是半透明黑，會把圖染黑。</summary>
    public static void SetPaperTint (Selectable selectable)
    {
        if (!selectable) return;
        selectable.transition = Selectable.Transition.ColorTint;
        var colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.88f, 0.84f, 1);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f, 1);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1, 1, 1, 0.5f);
        colors.colorMultiplier = 1;
        selectable.colors = colors;
    }

    // 不能寫 GetComponent() ?? AddComponent()：Editor 裡找不到元件時回傳的是「假 null」，?? 判斷不出來。
    public static T GetOrAdd<T> (GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    public static void RemoveComponent<T> (Transform t) where T : Component
    {
        var c = t.GetComponent<T>();
        if (c) Object.DestroyImmediate(c, true);
    }

    public static void SetRef (Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─── 共用元件 ────────────────────────────────────────────────

    /// <summary>分頁紙條：底圖 + 選中時蓋上去的紫色紙條 + 字。tab 上要有 Toggle，且 graphic 是選中那張。</summary>
    public static void StyleTab (Transform tab)
    {
        if (!tab) return;

        var layout = GetOrAdd<LayoutElement>(tab.gameObject);
        layout.preferredWidth = 340;
        layout.preferredHeight = 70;
        layout.flexibleWidth = layout.flexibleHeight = 0;

        var toggle = tab.GetComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;

        var image = tab.GetComponent<Image>();
        image.sprite = LoadSprite("saveload_tab");
        image.color = Color.white;

        if (toggle.graphic is Image on)
        {
            on.sprite = LoadSprite("saveload_tab_on");
            on.color = Color.white;
            Stretch(on.rectTransform);
        }

        var label = tab.GetComponentInChildren<Text>(true);
        if (label)
        {
            label.fontSize = 26;
            label.transform.SetAsLastSibling();
        }

        SetRef(GetOrAdd<ToggleLabelColor>(tab.gameObject), "label", label);
    }

    /// <summary>標題下面那排分頁的容器：橫排置中，只剩一顆時自動置中。</summary>
    public static void StyleTabBar (RectTransform bar)
    {
        Place(bar, 610, 151, 700, 70);
        var layout = GetOrAdd<HorizontalLayoutGroup>(bar.gameObject);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var image = bar.GetComponent<Image>();
        if (image) image.enabled = false;
    }

    /// <summary>
    /// 翻頁列：‹ 1 2 3 4 5 ›。pagination 底下要有 PreviousPageButton / NextPageButton
    /// （Naninovel 的 PaginationPanel 就是這樣），數字那排會插在兩顆箭頭中間。
    /// </summary>
    public static void StylePagination (RectTransform pagination, MonoBehaviour source, Font font, float y)
    {
        Place(pagination, 610, y, 700, 60);
        var layout = GetOrAdd<HorizontalLayoutGroup>(pagination.gameObject);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var previous = pagination.Find("PreviousPageButton");
        var next = pagination.Find("NextPageButton");
        StyleArrow(previous, true);
        StyleArrow(next, false);

        var oldNumber = pagination.Find("PageNumberPanel");
        if (oldNumber) oldNumber.gameObject.SetActive(false);

        var numbers = NewRect("PageNumbers", pagination);
        numbers.SetSiblingIndex(previous ? previous.GetSiblingIndex() + 1 : 0);
        var numbersLayout = numbers.gameObject.AddComponent<HorizontalLayoutGroup>();
        numbersLayout.spacing = 10;
        numbersLayout.childAlignment = TextAnchor.MiddleCenter;
        numbersLayout.childControlWidth = numbersLayout.childControlHeight = true;
        numbersLayout.childForceExpandWidth = numbersLayout.childForceExpandHeight = false;
        var numbersElement = numbers.gameObject.AddComponent<LayoutElement>();
        numbersElement.preferredWidth = 5 * 60 + 4 * 10;
        numbersElement.preferredHeight = 60;

        // 樣板：透明底（接點擊）＋ 選中菱形 ＋ 數字
        var hit = NewImage("PageButtonTemplate", numbers, null, true);
        hit.color = new Color(1, 1, 1, 0);
        var button = hit.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hit;
        var element = hit.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = element.preferredHeight = 60;

        var current = NewImage("Current", hit.transform, LoadSprite("saveload_page_on"), false);
        Stretch(current.rectTransform);
        var label = NewText("Label", hit.transform, font, 26, TextAnchor.MiddleCenter, "1");
        Stretch(label.rectTransform);

        var bar = numbers.gameObject.AddComponent<PageNumberBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("source").objectReferenceValue = source;
        so.FindProperty("template").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void StyleArrow (Transform arrow, bool flip)
    {
        if (!arrow) return;

        var element = GetOrAdd<LayoutElement>(arrow.gameObject);
        element.preferredWidth = 40;
        element.preferredHeight = 60;

        var image = arrow.GetComponent<Image>();
        image.sprite = LoadSprite("saveload_arrow");
        image.color = Color.white;
        image.preserveAspect = true;
        arrow.localScale = new Vector3(flip ? -1 : 1, 1, 1);

        var icon = arrow.Find("Icon");
        if (icon) icon.gameObject.SetActive(false);
    }

    /// <summary>右下角的返回鈕。button 上要有 Image，字在子物件。</summary>
    public static void StyleReturnButton (Transform button)
    {
        var image = button.GetComponent<Image>();
        image.sprite = LoadSprite("saveload_return");
        image.color = Color.white;
        SetPaperTint(button.GetComponent<Button>());
        var label = button.GetComponentInChildren<Text>(true);
        if (label) { label.color = Ink; label.fontSize = 26; }
    }
}
