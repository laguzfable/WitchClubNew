using UnityEngine;
using UnityEngine.SceneManagement;

public static class DDOLDumper
{
    public static void Dump(string tag)
    {
        var s = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (!s.IsValid())
        {
            Debug.Log($"[DDOL] ({tag}) scene not valid.");
            return;
        }

        var roots = s.GetRootGameObjects();
        Debug.Log($"[DDOL] ({tag}) roots={roots.Length}");
        foreach (var go in roots)
            Debug.Log($"[DDOL]  - {go.name}");
    }
}
