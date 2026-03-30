using UnityEngine;

public class AudioListenerWatcher : MonoBehaviour
{
    AudioListener listener;

    void Awake()
    {
        listener = GetComponent<AudioListener>();
    }

    void Update()
    {
        if (!listener.enabled)
        {
            listener.enabled = true;  // 強制開回來
            Debug.LogError("[AudioListenerWatcher] 有人把我關掉了！");
            // 印出完整的 call stack，找到是誰呼叫的
            Debug.LogError(System.Environment.StackTrace);
        }
    }
}