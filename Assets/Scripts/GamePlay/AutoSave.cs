using Naninovel;
using UnityEngine;

/// <summary>
/// 自動存檔。存進 Naninovel 的快速存檔格（GameQuickSave001…），
/// 滿了自己往後推、最舊的丟掉，玩家從「載入 → 快速存檔」那一頁看得到。
///
/// ★ 存在哪三個地方 ★
///   ‧ 進戰鬥之前（@battle）
///   ‧ 進地圖之前（夜晚儀式／白天約會，@GotoUnityScene sceneName:MapTest）
///   ‧ 從戰鬥或地圖回到劇本之後（那一場的結果剛落袋，最不該弄丟）
///   ‧ 換到新的一章時（chapter*.nani 的第一個指令）
/// 都是「玩家心裡一個段落的開頭」，而且都緊接著換場景——當機最容易發生在那裡。
/// 一章存一次太少（第三章一章就有四個夜晚加四場戰鬥），每句話存一次又太吵。
///
/// ★ 為什麼要在指令執行「之前」存 ★
/// Naninovel 記的是「這個指令做過了沒」（ExecutedPlayedCommand）。
/// 在 @battle 的 ExecuteAsync 裡面存，那個旗標已經是 true，讀檔時會跳過這一行——
/// 玩家會發現讀了「戰鬥前」的檔，結果戰鬥被跳過了。
/// 掛在 ScriptPlayer 的 pre-execution task 上就是在旗標翻成 true 之前，
/// 讀檔會重新執行那一行，真的回到戰鬥前。
///
/// ★ 進度會跟著回捲嗎 ★
/// 會。符文、儀式進度那些是寫 PlayerPrefs 的，本來不跟存檔走，
/// 現在由 <see cref="RunSnapshot"/> 打包進存檔——沒有它的話，
/// 讀「戰鬥前」的檔會變成劇本回去了、符文卻還留著。
/// </summary>
public class AutoSave : MonoBehaviour
{
    /// <summary>進地圖的場景名。只有這個場景要存，其他 @GotoUnityScene（結算畫面之類）不用。</summary>
    const string MapScene = "MapTest";

    /// <summary>劇本場景。從戰鬥或地圖回來時載的就是它。</summary>
    const string StoryScene = "NaniDialogTest";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (FindObjectOfType<AutoSave>() != null) return;
        var go = new GameObject(nameof(AutoSave));
        DontDestroyOnLoad(go);
        go.AddComponent<AutoSave>();
    }

    IScriptPlayer player;

    /// <summary>上一次存的位置。讀檔會把同一個指令再跑一次，
    /// 不擋的話每讀一次檔就多存一格一模一樣的。</summary>
    PlaybackSpot lastSaved;

    /// <summary>上一個執行過指令的劇本名，用來認出「換章了」。</summary>
    string lastScript;

    /// <summary>正在存檔中。存檔本身是非同步的，中間劇本還會往下跑，
    /// 兩次疊在一起的話輪替檔名會撞在一起（Windows 會丟 Sharing violation）。</summary>
    bool saving;

    /// <summary>剛回到劇本場景，下一個指令之前要存一次。
    /// 不在場景載好的當下存——那時候劇本還在回到定位，存下去的位置不一定對。</summary>
    string pendingReason;

    void Awake ()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        WaitForEngineAndSubscribe().Forget();
    }

    void HandleSceneLoaded (UnityEngine.SceneManagement.Scene scene,
                            UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == StoryScene) pendingReason = "回到劇本";
    }

    async UniTaskVoid WaitForEngineAndSubscribe ()
    {
        while (!Engine.Initialized)
            await UniTask.Yield();

        player = Engine.GetService<IScriptPlayer>();
        if (player == null)
        {
            Debug.LogError("[AutoSave] 拿不到 IScriptPlayer，自動存檔不會發生。");
            return;
        }

        player.AddPreExecutionTask(BeforeCommandAsync);
    }

    void OnDestroy ()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        player?.RemovePreExecutionTask(BeforeCommandAsync);
    }

    async UniTask BeforeCommandAsync (Command command)
    {
        var script = player.PlayedScript != null ? player.PlayedScript.Name : null;
        var reason = ReasonFor(command, script);
        lastScript = script;

        if (reason == null) return;

        var spot = player.PlaybackSpot;
        if (spot.Equals(lastSaved)) return;      // 剛剛才存過這一格（多半是讀檔後重跑）

        var stateManager = Engine.GetService<IStateManager>();
        if (stateManager == null) return;
        if (saving) return;                      // 上一次還沒寫完，這次跳過

        Debug.Log($"[AutoSave] {reason}：{spot}");
        await SaveAsync(stateManager, spot);
    }

    /// <summary>
    /// 真的去存。存檔會叫醒 RunSnapshot 把 PlayerPrefs 那些進度一起打包進去。
    ///
    /// ★ 為什麼整段包在 try 裡 ★
    /// 這支是掛在 ScriptPlayer 的 pre-execution task 上，例外會一路往上炸穿
    /// PlayRoutineAsync——劇本會當場停住，玩家看到的是遊戲卡死。
    /// 自動存檔失敗頂多是少一格存檔，絕對不值得把整個劇本帶走。
    ///
    /// ★ 為什麼要重試一次 ★
    /// 快速存檔是用「把 001 改名成 002」的方式輪替的，而 Editor 底下存檔就寫在
    /// Assets/NaninovelData/Saves——Unity 的匯入器正好在看那個資料夾，
    /// 檔案偶爾會被鎖住（Sharing violation）。那是一瞬間的事，隔幾幀再試就過了。
    /// </summary>
    async UniTask SaveAsync (IStateManager stateManager, PlaybackSpot spot)
    {
        saving = true;
        try
        {
            try
            {
                await stateManager.QuickSaveAsync();
            }
            catch (System.Exception first)
            {
                Debug.LogWarning($"[AutoSave] 存檔失敗，隔一下再試一次：{first.Message}");
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
                await stateManager.QuickSaveAsync();
            }

            lastSaved = spot;
        }
        catch (System.Exception ex)
        {
            // 重試也失敗就放過這一格。下一個存檔點還會再存一次。
            Debug.LogWarning($"[AutoSave] 這一格跳過（劇本照常進行）：{ex.Message}");
        }
        finally { saving = false; }
    }

    /// <summary>這個指令前面該不該存？該的話回一句給 log 看的理由，不該就回 null。</summary>
    string ReasonFor (Command command, string script)
    {
        if (pendingReason != null)
        {
            var reason = pendingReason;
            pendingReason = null;
            return reason;
        }

        // 只認 @battle。教學戰（@tutorial／@runeTutorial）是另外的指令，刻意不存：
        // 那是寫死的一段演出，中間沒有玩家會心疼的進度，而且它的旗標
        // （TutorialController.isTutorial）不在存檔裡，讀回來會對不起來。
        if (command is GotoCombatScene) return "戰鬥前";

        if (command is GotoUnityScene gotoScene &&
            Command.Assigned(gotoScene.SceneName) &&
            gotoScene.SceneName.Value == MapScene) return "進地圖前";

        if (script != null && script != lastScript &&
            script.StartsWith("chapter", System.StringComparison.OrdinalIgnoreCase)) return "換章";

        return null;
    }
}
