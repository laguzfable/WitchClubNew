using UnityEngine;

/// <summary>
/// 「普通卡型態（A/B）」的解鎖狀態查詢。
///
/// 解鎖是由劇本的 @unlockCardVariant 寫進 PlayerPrefs 的
/// （見 <see cref="UnlockCardVariantCommand"/>，key 格式 UnlockedCardVariant_&lt;元素&gt;，值像 "A,B"）。
/// 在跑到那段劇情之前，玩家手上只有原版卡，「更換卡片」頁進去也只有四張原版可選，
/// 所以入口要先鎖起來。
///
/// 女巫競技場不用特別處理：開挑戰時 TowerModeManager.SnapshotAndUnlockAllCardVariants()
/// 會把四色都設成 "A,B"，所以競技場裡這裡一定回 true，選單頁的換卡片按鈕照常出現。
/// </summary>
public static class CardVariantUnlock
{
    /// <summary>key 要跟 UnlockCardVariantCommand / SelectCardVariant 用的一致，改的話三邊一起改。</summary>
    public static string KeyFor(ECardElement element) => "UnlockedCardVariant_" + element;

    /// <summary>有沒有解鎖過「任何一個」卡片型態。決定換卡片入口要不要出現。</summary>
    public static bool AnyUnlocked
    {
        get
        {
            foreach (ECardElement e in System.Enum.GetValues(typeof(ECardElement)))
            {
                if (e == ECardElement.None) continue;
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString(KeyFor(e), ""))) return true;
            }
            return false;
        }
    }
}
