using Naninovel;
using UnityEngine;

/// <summary>
/// 把 <see cref="RunSnapshot"/> 掛到 Naninovel 的存讀流程上。開場自動掛載，
/// 不需要改任何場景或 prefab。
/// </summary>
public class RunSnapshotSync : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (FindObjectOfType<RunSnapshotSync>() != null) return;
        var go = new GameObject(nameof(RunSnapshotSync));
        DontDestroyOnLoad(go);
        go.AddComponent<RunSnapshotSync>();
    }

    IStateManager stateManager;

    void Awake () => WaitForEngineAndSubscribe().Forget();

    async UniTaskVoid WaitForEngineAndSubscribe ()
    {
        while (!Engine.Initialized)
            await UniTask.Yield();

        stateManager = Engine.GetService<IStateManager>();
        if (stateManager == null)
        {
            Debug.LogError("[RunSnapshot] 拿不到 IStateManager，這一輪的進度不會跟著存檔走。");
            return;
        }

        stateManager.AddOnGameSerializeTask(Serialize);
        stateManager.AddOnGameDeserializeTask(DeserializeAsync);
    }

    void OnDestroy ()
    {
        if (stateManager == null) return;
        stateManager.RemoveOnGameSerializeTask(Serialize);
        stateManager.RemoveOnGameDeserializeTask(DeserializeAsync);
    }

    static void Serialize (GameStateMap map) => map.SetState(RunSnapshot.Capture());

    static UniTask DeserializeAsync (GameStateMap map)
    {
        // 舊存檔沒有這一包，GetState 會回 null——那時候什麼都不做。
        RunSnapshot.Apply(map.GetState<RunSnapshotState>());
        return UniTask.CompletedTask;
    }
}
