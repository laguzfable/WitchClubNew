using UnityEditor;
using UnityEngine;

public class MissingScriptInAssets
{
    [MenuItem("Tools/Find Missing Scripts in Prefabs")]
    static void FindMissingScriptsInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int missingCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"❌ Missing script in prefab: {path}", prefab);
                    missingCount++;
                    break;
                }
            }
        }

        Debug.Log($"🔍 Prefab scan complete. Found {missingCount} prefabs with missing scripts.");
    }
}
