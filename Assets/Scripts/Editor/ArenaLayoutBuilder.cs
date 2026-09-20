using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UILayoutKit;

/// <summary>
/// 女巫競技場的畫面先套上現有的 UI：TowerHubScene（每層之間那一頁）、ChangeAmuletScene 的「裝備完成」鈕。
/// 換符文、換卡片跟主線休息室共用，這裡不動。
///
/// ★ 先用現有的，畫好就換 ★
/// Sprites/UI/Arena/ 底下的 arena_*.png 目前是附尺寸的示意圖。這支會判斷那張是不是還是示意圖：
/// 是的話先借用存讀檔的紙條（返回鈕、分頁），畫好之後重跑一次就會換成你的圖。
/// 版面模板在 docs/ui/競技場_Hub_版面.png。
///
/// ★ 動的是場景，不是 prefab ★
/// 遊戲執行中不動；場景開著而且有沒存的修改也不動（免得蓋掉）。
/// 場景沒開的話，會在背景打開、改完、存檔、關掉，不影響你目前開著的場景。
/// </summary>
public static class ArenaLayoutBuilder
{
    const string HubScene = "Assets/Scenes/Test/TowerHubScene.unity";
    const string AmuletScene = "Assets/Scenes/Test/ChangeAmuletScene.unity";
    const string ArenaSpriteDir = "Assets/Sprites/UI/Arena/";

    static readonly Color Paper = new Color32(246, 238, 222, 255);
    static readonly Color TextShadow = new Color(0.12f, 0.08f, 0.08f, 0.85f);

    [MenuItem("Tools/Witch Club/女巫競技場/套上現有的 UI")]
    static void Build ()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("女巫競技場", "遊戲執行中，先停止再跑。", "好");
            return;
        }
        if (!EditorUtility.DisplayDialog("女巫競技場",
                "會修改 TowerHubScene 的按鈕、樓層字、確認框，以及 ChangeAmuletScene 的「裝備完成」鈕。\n\n" +
                "換符文、換卡片（跟主線共用）不會動。確定要跑嗎？", "套用", "取消"))
            return;

        ImportSprites(SharedSpriteDir);
        ImportSprites(ArenaSpriteDir);

        if (!EditScene(HubScene, BuildHub)) return;
        if (!EditScene(AmuletScene, BuildAmulet)) return;
        Debug.Log("[ArenaLayoutBuilder] 女巫競技場套用完成");
    }

    // ─── Hub ─────────────────────────────────────────────────────

    static void BuildHub (Scene scene)
    {
        var canvas = FindRoot(scene, "Canvas");
        if (!canvas) { Debug.LogError("[ArenaLayoutBuilder] TowerHubScene 找不到 Canvas"); return; }

        // 一般按鈕：還沒畫就借存讀檔的返回鈕紙條。
        var button = ArenaSprite("arena_button") ?? LoadSprite("saveload_return");
        foreach (var name in new[] { "AmuletButton", "RuneButton", "CardButton", "back", "AbandonButton" })
            StyleButton(canvas.Find(name), button, Ink, 28);

        // 主按鈕「進入下一層」：還沒畫就借紫色的分頁紙條（選中那張），跟其他按鈕分得出來。
        var main = ArenaSprite("arena_button_main");
        StyleButton(canvas.Find("ContinueButton"), main ?? LoadSprite("saveload_tab_on"), main ? Ink : Paper, 32);

        // 樓層字：底下有沒有牌子看有沒有畫；沒有牌子時字直接壓在背景上，描深色邊才看得清楚。
        var floor = canvas.Find("FloorText");
        if (floor)
        {
            var text = floor.GetComponent<Text>();
            var plate = ArenaSprite("arena_floor_plate");
            var plateName = "FloorPlate";
            var old = canvas.Find(plateName);
            if (old) Object.DestroyImmediate(old.gameObject);
            if (plate)
            {
                var image = NewImage(plateName, canvas, plate, false);
                var rt = image.rectTransform;
                var f = (RectTransform)floor;
                rt.anchorMin = f.anchorMin; rt.anchorMax = f.anchorMax; rt.pivot = f.pivot;
                rt.anchoredPosition = f.anchoredPosition; rt.sizeDelta = f.sizeDelta;
                image.transform.SetSiblingIndex(floor.GetSiblingIndex());
                text.color = Ink;
                RemoveComponent<Outline>(floor);
            }
            else
            {
                text.color = Paper;
                var outline = GetOrAdd<Outline>(floor.gameObject);
                outline.effectColor = TextShadow;
                outline.effectDistance = new Vector2(2, -2);
            }
        }

        // 放棄挑戰的確認框：原本撐滿大半個畫面，縮成置中 900×420。
        var panel = canvas.Find("Panel") as RectTransform;
        if (panel)
        {
            Place(panel, 510, 330, 900, 420);
            var image = panel.GetComponent<Image>();
            var sprite = ArenaSprite("arena_confirm_panel");
            if (sprite)
            {
                SetSliceBorder(ArenaSpriteDir + "arena_confirm_panel.png", 48);
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2;
                image.color = Color.white;
                RemoveComponent<Outline>(panel);
            }
            else
            {
                // 還沒畫：用紙色底加細邊，跟設定畫面的下拉選單同一套。
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = new Color(Paper.r, Paper.g, Paper.b, 0.97f);
                var outline = GetOrAdd<Outline>(panel.gameObject);
                outline.effectColor = new Color(InkLight.r, InkLight.g, InkLight.b, 0.8f);
                outline.effectDistance = new Vector2(2, -2);
            }

            foreach (Transform child in panel)
            {
                var message = child.GetComponent<Text>();
                if (message && !child.GetComponent<Button>())
                {
                    var rt = (RectTransform)child;
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, 60);
                    rt.sizeDelta = new Vector2(800, 200);
                    message.color = Ink;
                    message.fontSize = 32;
                    message.alignment = TextAnchor.MiddleCenter;
                }
            }
            var yes = panel.Find("yes") as RectTransform;
            var no = panel.Find("no") as RectTransform;
            if (yes) { yes.anchoredPosition = new Vector2(-200, -120); yes.sizeDelta = new Vector2(300, 80); }
            if (no) { no.anchoredPosition = new Vector2(200, -120); no.sizeDelta = new Vector2(300, 80); }
            StyleButton(yes, button, Ink, 28);
            StyleButton(no, button, Ink, 28);
        }
    }

    // ─── 換護符 ──────────────────────────────────────────────────

    /// <summary>
    /// 這一頁的護符格子是執行時才排出來的，看不到實際畫面前先只改「裝備完成」鈕。
    /// </summary>
    static void BuildAmulet (Scene scene)
    {
        var canvas = FindRoot(scene, "Canvas");
        if (!canvas) { Debug.LogError("[ArenaLayoutBuilder] ChangeAmuletScene 找不到 Canvas"); return; }
        var done = canvas.Find("Button");
        StyleButton(done, ArenaSprite("arena_button") ?? LoadSprite("saveload_return"), Ink, 30);
    }

    // ─── 小工具 ──────────────────────────────────────────────────

    static void StyleButton (Transform button, Sprite sprite, Color textColor, int fontSize)
    {
        if (!button) return;
        var image = button.GetComponent<Image>();
        if (image)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        SetPaperTint(button.GetComponent<Button>());
        var label = button.GetComponentInChildren<Text>(true);
        if (label)
        {
            label.color = textColor;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// 競技場專用的圖：還是示意圖（左上角是示意圖那個粉紫色框）就當作還沒畫，回傳 null。
    /// 直接讀檔案判斷，不用把貼圖設成可讀。
    /// </summary>
    static Sprite ArenaSprite (string name)
    {
        var path = ArenaSpriteDir + name + ".png";
        if (!File.Exists(path)) return null;
        var probe = new Texture2D(2, 2);
        try
        {
            if (!probe.LoadImage(File.ReadAllBytes(path))) return null;
            var corner = (Color32)probe.GetPixel(0, probe.height - 1);
            var isPlaceholder = corner.r == 170 && corner.g == 60 && corner.b == 120;
            return isPlaceholder ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    static void SetSliceBorder (string path, int corner)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
        if (importer.spriteBorder != Vector4.zero) return;
        importer.spriteBorder = new Vector4(corner, corner, corner, corner);
        importer.SaveAndReimport();
    }

    static Transform FindRoot (Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root.transform;
        return null;
    }

    /// <summary>
    /// 打開場景改完存檔。場景本來就開著的話直接改（但有沒存的修改就停下來）；
    /// 沒開的話在背景疊開，改完關掉。
    /// </summary>
    static bool EditScene (string path, System.Action<Scene> edit)
    {
        var scene = SceneManager.GetSceneByPath(path);
        var wasOpen = scene.IsValid() && scene.isLoaded;
        if (wasOpen && scene.isDirty)
        {
            EditorUtility.DisplayDialog("女巫競技場",
                $"{Path.GetFileName(path)} 開著而且有還沒存的修改，先存檔再跑。", "好");
            return false;
        }

        if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            edit(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
        return true;
    }
}
