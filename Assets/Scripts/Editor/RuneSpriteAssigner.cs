// Assets/Editor/RuneSpriteAssigner.cs
// Usage: Tools > Witch Club > Assign Rune Sprites
// Put sprites in any folder, named exactly after the ability ID (e.g. blue01.png, red03.png).
// The tool scans the entire project for matching sprite names and assigns them in one click.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RuneSpriteAssigner : EditorWindow
{
    AbilityCollection target;
    string spriteFolder = "Assets/Sprites/RuneCards";
    Vector2 scroll;

    [MenuItem("Tools/Witch Club/Assign Rune Sprites")]
    static void Open() => GetWindow<RuneSpriteAssigner>("Rune Sprite Assigner");

    void OnGUI()
    {
        GUILayout.Label("Rune Sprite Batch Assigner", EditorStyles.boldLabel);
        GUILayout.Space(4);

        target = (AbilityCollection)EditorGUILayout.ObjectField(
            "AbilityCollection", target, typeof(AbilityCollection), false);

        GUILayout.Space(4);
        GUILayout.Label("Sprite folder (sprites named <abilityID>.png):");
        spriteFolder = EditorGUILayout.TextField(spriteFolder);

        if (GUILayout.Button("Browse..."))
        {
            var picked = EditorUtility.OpenFolderPanel("Select sprite folder",
                Application.dataPath, "");
            if (!string.IsNullOrEmpty(picked))
            {
                // Convert absolute path to project-relative
                if (picked.StartsWith(Application.dataPath))
                    spriteFolder = "Assets" + picked.Substring(Application.dataPath.Length);
            }
        }

        GUILayout.Space(8);

        if (target == null)
        {
            EditorGUILayout.HelpBox("Drag an AbilityCollection asset into the field above.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Assign Sprites", GUILayout.Height(32)))
            Assign();

        // Preview
        GUILayout.Space(8);
        GUILayout.Label($"Abilities ({target.abilityList.Count}):", EditorStyles.boldLabel);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var a in target.abilityList)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(a.id, GUILayout.Width(120));
            GUILayout.Label(a.image != null ? a.image.name : "(none)",
                a.image != null ? EditorStyles.label : EditorStyles.helpBox,
                GUILayout.Width(140));
            if (a.image != null)
                GUILayout.Label(EditorGUIUtility.ObjectContent(a.image, typeof(Sprite)).image,
                    GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    void Assign()
    {
        // Build a lookup: spriteName (no extension, lowercase) → sprite
        var allSprites = new Dictionary<string, Sprite>();
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null)
                allSprites[spr.name.ToLower()] = spr;
        }

        if (allSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("No sprites found",
                $"No sprites found in '{spriteFolder}'.\nMake sure the folder exists and contains sprites named after ability IDs.", "OK");
            return;
        }

        var so = new SerializedObject(target);
        var list = so.FindProperty("abilityList");
        int matched = 0, skipped = 0;

        for (int i = 0; i < list.arraySize; i++)
        {
            var elem = list.GetArrayElementAtIndex(i);
            var idProp = elem.FindPropertyRelative("id");
            var imgProp = elem.FindPropertyRelative("image");
            var id = idProp.stringValue?.ToLower();

            if (!string.IsNullOrEmpty(id) && allSprites.TryGetValue(id, out var spr))
            {
                imgProp.objectReferenceValue = spr;
                matched++;
            }
            else
                skipped++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Done",
            $"Matched: {matched}  /  Not found: {skipped}\n\n" +
            (skipped > 0 ? "Missing IDs logged to Console." : "All abilities assigned!"), "OK");

        // Log unmatched
        for (int i = 0; i < target.abilityList.Count; i++)
        {
            var a = target.abilityList[i];
            if (!allSprites.ContainsKey(a.id?.ToLower() ?? ""))
                Debug.LogWarning($"[RuneSpriteAssigner] No sprite found for id='{a.id}'");
        }

        Repaint();
    }
}
