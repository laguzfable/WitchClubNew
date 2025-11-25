using System.Collections.Generic;
using UnityEngine;

public class VisitedNodeManager : MonoBehaviour
{
    public static VisitedNodeManager Instance { get; private set; }

    private const string PlayerPrefsKey = "WC/VisitedNodes/v1";

    // ★ 現在改成「完整 key」：
    //   chapter0
    //   chapter0#afterbattle
    //   chapter1#bossdead
    private HashSet<string> visited = new HashSet<string>();

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

    // ============================================================
    //  ⭐ 新版：支援 node + label
    // ============================================================
    private void Save()
    {
        var raw = string.Join("|", visited);
        PlayerPrefs.SetString(PlayerPrefsKey, raw);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        visited.Clear();
        var raw = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (string.IsNullOrEmpty(raw)) return;

        var parts = raw.Split('|');
        foreach (var p in parts)
        {
            if (!string.IsNullOrEmpty(p))
                visited.Add(p);
        }
    }

    // ============================================================
    //  ⭐ 供 VisitNodeCommand 呼叫
    // ============================================================
    public void MarkVisited(string nodeId, string label = "")
    {
        if (string.IsNullOrEmpty(nodeId)) return;

        string key = string.IsNullOrEmpty(label) ? nodeId : $"{nodeId}#{label}";

        if (visited.Add(key))
        {
            Save();
            Debug.Log($"[VisitedNodeManager] Visited: {key}");
        }
    }

    // ============================================================
    //  ⭐ 給地圖使用，判斷是否亮起
    // ============================================================
    public bool IsVisited(string nodeId, string label = "")
    {
        if (string.IsNullOrEmpty(nodeId)) return false;

        string key = string.IsNullOrEmpty(label) ? nodeId : $"{nodeId}#{label}";
        return visited.Contains(key);
    }
}
