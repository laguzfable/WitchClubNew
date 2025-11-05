using UnityEditor;
using UnityEngine;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    static void FindMissingScripts()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int missingCount = 0;

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script found on GameObject: '{go.name}' in scene '{go.scene.name}'", go);
                    missingCount++;
                }
            }
        }

        Debug.Log($"Scan complete. Found {missingCount} missing scripts.");
    }
}
