using System.Collections.Generic;
using UnityEngine;

public class VisitedNodeManager : MonoBehaviour
{
    public static VisitedNodeManager Instance { get; private set; }

    private const string PlayerPrefsKey = "WC/VisitedNodes/v1";
    private HashSet<string> visitedNodes = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Load()
    {
        visitedNodes.Clear();
        var raw = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (string.IsNullOrEmpty(raw)) return;

        var parts = raw.Split('|');
        foreach (var p in parts)
        {
            if (!string.IsNullOrEmpty(p)) visitedNodes.Add(p);
        }
    }

    private void Save()
    {
        var raw = string.Join("|", visitedNodes);
        PlayerPrefs.SetString(PlayerPrefsKey, raw);
        PlayerPrefs.Save();
    }

    public void MarkVisited(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (visitedNodes.Add(nodeId))
        {
            Save();
            Debug.Log($"[VisitedNodeManager] Visited: {nodeId}");
        }
    }

    public bool IsVisited(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return visitedNodes.Contains(nodeId);
    }
}