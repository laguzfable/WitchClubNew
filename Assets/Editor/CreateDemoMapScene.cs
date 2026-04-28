using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CreateDemoMapScene
{
    [MenuItem("Tools/建立 DemoMap 場景")]
    static void Create()
    {
        // 1. 新場景
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.05f, 0.12f, 1f);
        cam.orthographic     = true;
        cam.tag              = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // 3. EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // 4. DemoMapAutoProgress 掛載點
        var demoGO = new GameObject("DemoMapController");
        var demoType = System.Type.GetType("DemoMapAutoProgress");
        if (demoType != null) demoGO.AddComponent(demoType);
        else Debug.LogWarning("[CreateDemoMapScene] 找不到 DemoMapAutoProgress，請手動加上腳本。");

        // 5. 儲存場景
        string path = "Assets/Scenes/DemoMap.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();

        // 6. 加入 Build Settings
        var scenes = EditorBuildSettings.scenes;
        bool alreadyIn = false;
        foreach (var s in scenes)
            if (s.path == path) { alreadyIn = true; break; }

        if (!alreadyIn)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(path, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        Debug.Log($"[CreateDemoMapScene] 完成！場景已儲存至 {path} 並加入 Build Settings。");
        EditorUtility.DisplayDialog("完成", $"DemoMap 場景已建立！\n路徑：{path}", "OK");
    }
}
