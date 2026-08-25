using System.Linq;
using Naninovel;
using UnityEngine;
using Hexe.UI;

/// <summary>
/// 「作弊名字」：在 chapter0 問名字時輸入指定的名字，就把要一格一格解的收集要素一次打開。
///
/// ★ 打開的東西 ★
///   ‧ 回憶模式全 CG（Naninovel unlockable items）
///   ‧ 怪物圖鑑全解鎖
///   ‧ 蝕之聖典（標題按鈕 + 地圖上所有節點）
///   ‧ 女巫競技場（標題按鈕）
///
/// ★ 不會打開的東西（刻意的）★
///   結局紀錄與符文／卡片解鎖不碰——那些是要玩家自己跑出來的，
///   而且 EndingRecord 直接影響成就與「輪迴の鑰匙」的判定，灌假資料會污染成就。
///
/// ★ 要換名字就改 <see cref="CheatNames"/> ★
/// 想同時留幾個舊名字也可以，陣列裡全部都算數。比對會去掉前後空白、忽略英文大小寫。
/// </summary>
public static class CheatUnlock
{
    // ↓↓↓ 要換作弊名字改這一行就好 ↓↓↓
    static readonly string[] CheatNames = { "洛蘭" };
    // ↑↑↑ 要換作弊名字改這一行就好 ↑↑↑

    const string Key = "WC/CheatUnlock/v1";

    /// <summary>作弊模式開著沒有。F10 清進度（PlayerPrefs.DeleteAll）會一起關掉。</summary>
    public static bool IsActive => PlayerPrefs.GetInt(Key, 0) == 1;

    /// <summary>名字對得上就開啟作弊模式。回傳有沒有觸發。</summary>
    public static bool TryActivate (string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return false;

        var name = playerName.Trim();
        if (!CheatNames.Any(n => string.Equals(n, name, System.StringComparison.OrdinalIgnoreCase)))
            return false;

        Activate();
        return true;
    }

    public static void Activate ()
    {
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
        Debug.Log("[CheatUnlock] 作弊名字命中，全 CG／怪物圖鑑／蝕之聖典／女巫競技場已解鎖。");

        Apply();
    }

    public static void Deactivate ()
    {
        PlayerPrefs.SetInt(Key, 0);
        PlayerPrefs.Save();
        MonsterCodex.InvalidateCache();
        TitleMenuUnlockInjector.Refresh();
        Debug.Log("[CheatUnlock] 作弊模式已關閉（已經解鎖的 CG／圖鑑不會收回去，要清請用 F10）。");
    }

    /// <summary>
    /// 把作弊模式的效果實際套下去。
    /// 蝕之聖典的節點與兩顆標題按鈕是「每次判定時查 IsActive」，不需要在這裡寫資料；
    /// 回憶CG 與怪物圖鑑則是各自有一份紀錄，得真的寫進去。
    /// </summary>
    public static void Apply ()
    {
        if (!IsActive) return;

        MonsterCodex.InvalidateCache();
        MonsterCodex.UnlockAll();

        UnlockAllCGAsync().Forget();

        TitleMenuUnlockInjector.Refresh();
    }

    /// <summary>
    /// 回憶模式的 CG。UnlockableManager 的表裡只有「被 Set 過」的項目，沒解鎖過的 CG
    /// 根本不在表裡，所以 UnlockAllItems() 不夠用——得跟 CGGalleryPanel 用同一組 loader
    /// 把資源掃出來，一張一張 Set。id 規則同 CGGalleryGridSlot：CG/<資源路徑>。
    /// </summary>
    static async UniTaskVoid UnlockAllCGAsync ()
    {
        if (!Engine.Initialized)
        {
            Debug.LogWarning("[CheatUnlock] Naninovel 還沒初始化，回憶CG 這次沒解到（進標題後會再試一次）。");
            return;
        }

        var unlockables = Engine.GetService<IUnlockableManager>();
        var providers = Engine.GetService<IResourceProviderManager>();
        var l10n = Engine.GetService<ILocalizationManager>();
        if (unlockables == null || providers == null) return;

        var sources = new[]
        {
            new ResourceLoaderConfiguration { PathPrefix = $"{UnlockablesConfiguration.DefaultPathPrefix}/{Naninovel.UI.CGGalleryPanel.CGPrefix}" },
            new ResourceLoaderConfiguration { PathPrefix = $"{BackgroundsConfiguration.DefaultPathPrefix}/{BackgroundsConfiguration.MainActorId}/{Naninovel.UI.CGGalleryPanel.CGPrefix}" }
        };

        var count = 0;
        foreach (var source in sources)
        {
            var loader = source.CreateLocalizableFor<Texture2D>(providers, l10n);
            var paths = await loader.LocateAsync(string.Empty);
            foreach (var path in paths)
            {
                unlockables.SetItemUnlocked(ToUnlockableId(path), true);
                count++;
            }
        }

        // unlockable 狀態存在 global state（GlobalSave.nson），不主動存的話關遊戲就沒了。
        var state = Engine.GetService<IStateManager>();
        if (state != null) await state.SaveGlobalAsync();

        Debug.Log($"[CheatUnlock] 回憶CG 已解鎖 {count} 張");
    }

    static string ToUnlockableId (string path)
    {
        const string prefix = Naninovel.UI.CGGalleryPanel.CGPrefix + "/";
        // loader 回傳的通常已經是去掉前綴的區域路徑，但保險起見兩種寫法都吃。
        var local = path.Contains(prefix) ? path.GetAfterFirst(prefix) : path;
        return prefix + local;
    }

    /// <summary>
    /// 進標題時補跑一次：名字是在遊戲中輸入的，那時 Naninovel 可能還沒把 CG 資源掃完，
    /// 而且玩家也可能是在上一輪開的作弊模式。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnBoot ()
    {
        if (IsActive) WaitForEngineThenApply().Forget();
    }

    /// <summary>引擎起來之前 CG 那段會直接放棄，所以等它初始化完再套。</summary>
    static async UniTaskVoid WaitForEngineThenApply ()
    {
        const float timeout = 30f;
        var elapsed = 0f;

        while (!Engine.Initialized && elapsed < timeout)
        {
            await UniTask.Yield();
            elapsed += Time.unscaledDeltaTime;
        }

        Apply();
    }
}
