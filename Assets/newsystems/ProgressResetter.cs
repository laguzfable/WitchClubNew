using Naninovel;
using UnityEngine;

/// <summary>
/// 除錯用：按一顆鍵把「從頭開始測」需要清的東西一次清掉。
///
/// ★ 為什麼不是只有 PlayerPrefs ★
/// 這個專案的進度分散在三個互不相干的地方，只清 PlayerPrefs 會有東西殘留：
///   ‧ PlayerPrefs ── 事件進度、符文/卡片解鎖、女巫競技場樓層、結局紀錄…
///   ‧ 記憶體快取 ── 結局紀錄與怪物圖鑑各自有一份，不丟掉的話 DeleteAll 之後還是讀得到舊值
///   ‧ Naninovel global state ── 回憶CG（unlockable items）存在 GlobalSave.nson，
///     跟 PlayerPrefs 完全是兩套系統，DeleteAll 碰不到它
/// </summary>
public class ProgressResetter : MonoBehaviour
{
    // 可在 Inspector 修改按鍵
    [SerializeField] KeyCode resetKey = KeyCode.F10;

    // 同樣是清進度，但女巫競技場的最高樓層和進場資格留著。
    [SerializeField] KeyCode resetKeepArenaKey = KeyCode.F9;

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
            ResetLocalProgress();

        if (Input.GetKeyDown(resetKeepArenaKey))
            ResetLocalProgressKeepArena();
    }

    /// <summary>
    /// 清掉所有「本地」進度。F10 和 Editor 的 Tools 選單都走這裡，
    /// 兩邊共用同一份步驟，才不會其中一邊漏清東西。
    /// 注意：Steam 成就不在這裡面，那是雲端的，要另外呼叫 AchievementManager.DebugResetAll()。
    /// </summary>
    public static void ResetLocalProgress()
    {
        PlayerPrefs.DeleteAll(); // 或 StoryProgressManager.Instance.ResetProgress("角色代號");
        Hexe.UI.MonsterCodex.InvalidateCache(); // 圖鑑解鎖紀錄有快取，要一起丟掉
        EndingRecord.Clear(); // 結局紀錄也有快取，不清的話 DeleteAll 之後還是會判定成「拿過結局」
        ResetUnlockables(); // 回憶CG
        MapSpecialOverride.ClearAll(); // 地圖上還沒演到的特殊事件預約（純記憶體，DeleteAll 碰不到）
        Hexe.UI.TitleMenuUnlockInjector.Refresh(); // 標題選單的蝕之聖典／女巫競技場要立刻縮回去
        Debug.Log("已清除所有 PlayerPrefs 儲存的事件進度");
    }

    /// <summary>
    /// 除了女巫競技場以外全部清掉。要從頭測劇情、又不想把競技場的進度重跑時用。
    ///
    /// ★ 為什麼還要留結局紀錄 ★
    /// 競技場的入口是「跑過任一結局」才會出現（見 TitleMenuUnlockInjector）。
    /// 結局紀錄一起清掉的話，標題上那顆按鈕會縮回去，最高樓層留著也進不去。
    /// 所以這裡連結局紀錄一起保住——它是競技場的鑰匙，不是額外的恩惠。
    ///
    /// 其餘一律比照 ResetLocalProgress：圖鑑、回憶CG、符文、卡片型態、事件進度全部歸零。
    /// </summary>
    public static void ResetLocalProgressKeepArena()
    {
        // 先抄一份要留的，DeleteAll 之後再寫回去。
        // 一個一個 DeleteKey 的話，之後多一個 key 就會漏清，所以還是整份清掉再還原。
        //
        // 只留最高樓層：裝備中的護符、當前樓層那些是「這一局打到哪」，
        // 清掉等於中止未完成的挑戰，下次進競技場從第一層重開，這是對的。
        var bestFloor = PlayerPrefs.GetInt(BestFloorKey, 0);
        var endings = PlayerPrefs.GetString(EndingsKey, "");

        PlayerPrefs.DeleteAll();

        if (bestFloor > 0) PlayerPrefs.SetInt(BestFloorKey, bestFloor);
        if (!string.IsNullOrEmpty(endings)) PlayerPrefs.SetString(EndingsKey, endings);
        PlayerPrefs.Save();

        Hexe.UI.MonsterCodex.InvalidateCache();
        EndingRecord.InvalidateCache();   // 快取要重讀，不然讀到的是清掉前的那份
        ResetUnlockables();
        MapSpecialOverride.ClearAll();
        Hexe.UI.TitleMenuUnlockInjector.Refresh();

        Debug.Log($"[ProgressResetter] 除了女巫競技場以外都清掉了"
                  + $"（最高樓層 {bestFloor}、結局紀錄 {(string.IsNullOrEmpty(endings) ? 0 : endings.Split('|').Length)} 筆保留）");
    }

    const string BestFloorKey = "TowerMode.BestFloor";
    const string EndingsKey   = "WC/Endings/v1";

    /// <summary>
    /// 回憶CG（Naninovel 的 unlockable items）。
    /// 狀態在 global state，實體檔案是 Editor 的 Assets/NaninovelData/Saves/GlobalSave.nson
    /// （打包後在 persistentDataPath），所以要走引擎的 API 清、清完立刻存回去。
    /// LockAllItems 只會影響「已經登記在表裡」的項目，而 @unlock 過的都在表裡，所以夠用。
    /// </summary>
    static void ResetUnlockables()
    {
        // 引擎還沒起來時（例如在標題前的場景按下去）就跳過：
        // 那種情況下 global state 還沒載入，硬清反而會把空狀態寫回檔案。
        if (!Engine.Initialized)
        {
            Debug.LogWarning("[ProgressResetter] Naninovel 引擎還沒初始化，回憶CG 這次沒清到。");
            return;
        }

        Engine.GetService<IUnlockableManager>()?.LockAllItems();
        Engine.GetService<IStateManager>()?.SaveGlobalAsync().Forget();
        Debug.Log("[ProgressResetter] 回憶CG（unlockable items）已全部鎖回去");
    }
}
