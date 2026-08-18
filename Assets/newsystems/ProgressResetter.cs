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

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            PlayerPrefs.DeleteAll(); // 或 StoryProgressManager.Instance.ResetProgress("角色代號");
            Hexe.UI.MonsterCodex.InvalidateCache(); // 圖鑑解鎖紀錄有快取，要一起丟掉
            EndingRecord.Clear(); // 結局紀錄也有快取，不清的話 DeleteAll 之後還是會判定成「拿過結局」
            ResetUnlockables(); // 回憶CG
            Hexe.UI.TitleMenuUnlockInjector.Refresh(); // 標題選單的蝕之聖典／女巫競技場要立刻縮回去
            Debug.Log("已清除所有 PlayerPrefs 儲存的事件進度");
        }
    }

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
