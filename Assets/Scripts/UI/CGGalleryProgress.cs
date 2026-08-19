using System;
using System.Linq;
using Naninovel;
using Naninovel.UI;
using UnityEngine;

namespace Hexe.UI
{
    /// <summary>
    /// 回憶模式 CG 的收集進度，只為了「CG 全收集」成就而存在。
    ///
    /// Naninovel 那邊沒有現成的「解鎖了幾張」API：UnlockableManager 的 map 只裝過去被
    /// Set 過的項目，沒解鎖過的 CG 根本不在裡面，所以總數只能跟 CGGalleryPanel.CGCount 要
    /// （那是它掃資源掃出來的格子數）。已解鎖數則是從 map 裡撈 "CG/" 開頭且為 true 的項目。
    ///
    /// 一個格子可能對應多張圖（Naninovel 的 xxx_1 / xxx_2 變體），unlockable id 是「每張圖
    /// 一個」，所以這裡要先把結尾的 _數字 去掉還原成格子 id 再去重，否則兩邊的計數基準不一樣。
    /// 判定跟畫面一致：一個格子只要有任一張變體解鎖，那格在圖鑑上就是亮的，這裡也算解鎖。
    /// </summary>
    public static class CGGalleryProgress
    {
        const string IdPrefix = CGGalleryPanel.CGPrefix + "/";

        /// <summary>CG 格子總數。拿不到 CGGalleryPanel（還沒初始化）時回傳 0。</summary>
        public static int Total (CGGalleryPanel panel)
        {
            return panel != null ? panel.CGCount : 0;
        }

        /// <summary>已解鎖的格子數。</summary>
        public static int UnlockedCount ()
        {
            var unlockables = Engine.GetService<IUnlockableManager>();
            if (unlockables == null) return 0;

            return unlockables.GetAllItems()
                .Where(kv => kv.Value && kv.Key.StartsWith(IdPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => ToSlotId(kv.Key.Substring(IdPrefix.Length)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        /// <summary>
        /// 檢查有沒有全收集，有的話解成就。重複呼叫是安全的
        /// （AchievementManager 自己會先查已解鎖狀態），所以每次打開回憶模式叫一次就好。
        /// </summary>
        public static void CheckAchievement (CGGalleryPanel panel)
        {
            var total = Total(panel);
            if (total <= 0) return;

            var unlocked = UnlockedCount();
            Debug.Log($"[CGGalleryProgress] CG 收集進度 {unlocked} / {total}");

            if (unlocked >= total)
                AchievementManager.Instance?.Unlock(AchievementManager.ACH_CG_GALLERY_FULL);
        }

        /// <summary>把 "xxx_2" 這種變體路徑還原成格子 id "xxx"，規則跟 CGGalleryPanel 一致。</summary>
        static string ToSlotId (string path)
        {
            var underscore = path.LastIndexOf('_');
            if (underscore <= 0) return path;

            var suffix = path.Substring(underscore + 1);
            int _;
            if (!int.TryParse(suffix, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                return path;

            return path.Substring(0, underscore);
        }
    }
}
