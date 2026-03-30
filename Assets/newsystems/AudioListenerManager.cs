using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioListenerManager : MonoBehaviour
{
    private AudioListener mainListener;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        FixListeners();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name == "CombatScene") return;  // ← 加這行
    FixListeners();
}

    private void FixListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

        if (listeners.Length == 0)
        {
            Debug.LogWarning("[AudioListenerManager] 沒有 AudioListener，自動補一個。");
            mainListener = gameObject.AddComponent<AudioListener>();
            return;
        }

        // ✦ 優先保留 Naninovel AudioManager 的 Listener（正確解法）
        foreach (var l in listeners)
        {
            if (l.gameObject.name.Contains("AudioManager") ||
                l.gameObject.name.Contains("Naninovel"))
            {
                mainListener = l;
                break;
            }
        }

        // 找不到 Naninovel 的 listener → 退回保留第一個
        if (mainListener == null)
            mainListener = listeners[0];

        // 禁用多餘的 listener
        foreach (var l in listeners)
        {
            if (l != mainListener)
            {
                l.enabled = false;
            }
        }
    }
}
