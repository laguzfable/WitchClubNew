using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hexe.UI
{
    /// <summary>
    /// 怪物圖鑑的資料來源與解鎖紀錄。
    ///
    /// 資料直接讀 Resources/MobData 底下所有 MobData（排除 TestMobData 系列），
    /// 所以之後只要新增一個 MobData asset，圖鑑就會自動多一格，不用改程式。
    ///
    /// 解鎖條件：玩家在正式戰鬥中遇過該怪物。教學戰、測試戰、女巫競技場（隨機怪）
    /// 都不算，實際呼叫點在 EnemyUnit.Init()。
    /// 紀錄存在 PlayerPrefs，key = MonsterCodex_Seen，值是用 , 隔開的 MobData 檔名。
    /// </summary>
    public static class MonsterCodex
    {
        public const string SeenKey = "MonsterCodex_Seen";

        const string MobDataPath = "MobData";

        /// <summary>這些是拿來測試/教學用的假資料，不該出現在圖鑑裡。</summary>
        static readonly string[] excludedPrefixes = { "TestMobData" };

        static List<MobData> cachedMobs;
        static HashSet<string> cachedSeen;

        /// <summary>圖鑑要顯示的所有怪物，已排好順序。</summary>
        public static IReadOnlyList<MobData> AllMobs
        {
            get
            {
                if (cachedMobs != null) return cachedMobs;

                cachedMobs = Resources.LoadAll<MobData>(MobDataPath)
                    .Where(m => m != null && !excludedPrefixes.Any(p => m.name.StartsWith(p)))
                    .OrderBy(GetSortRank)
                    .ThenBy(m => m.name, System.StringComparer.Ordinal)
                    .ToList();

                Debug.Log($"[MonsterCodex] 載入 {cachedMobs.Count} 隻怪物：{string.Join(", ", cachedMobs.Select(m => m.name))}");
                return cachedMobs;
            }
        }

        /// <summary>主線怪物排前面，四色女巫依藍綠紅黃接在後面。</summary>
        static int GetSortRank (MobData mob)
        {
            var n = mob.name;
            if (n.StartsWith("monster")) return 0;
            if (n.StartsWith("micha")) return 1;
            if (n.StartsWith("blue")) return 2;
            if (n.StartsWith("green")) return 3;
            if (n.StartsWith("red")) return 4;
            if (n.StartsWith("yellow")) return 5;
            return 6;
        }

        static HashSet<string> Seen
        {
            get
            {
                if (cachedSeen != null) return cachedSeen;

                var raw = PlayerPrefs.GetString(SeenKey, string.Empty);
                cachedSeen = new HashSet<string>(
                    raw.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0));
                return cachedSeen;
            }
        }

        public static bool IsSeen (MobData mob) => mob != null && Seen.Contains(mob.name);

        public static bool IsSeen (string mobName) =>
            !string.IsNullOrEmpty(mobName) && Seen.Contains(mobName);

        /// <summary>已解鎖數量 / 總數，給標題上的「12 / 26」用。</summary>
        public static int SeenCount => AllMobs.Count(IsSeen);

        /// <summary>圖鑑是不是全開了。</summary>
        public static bool AllSeen => AllMobs.Count > 0 && SeenCount >= AllMobs.Count;

        /// <summary>
        /// 記下玩家遇過這隻怪。傳進來的是 MobData 的 asset 檔名（例如 monster02）。
        /// 重複呼叫沒有副作用，已經記過就直接跳出，不會一直寫 PlayerPrefs。
        /// </summary>
        public static void RecordSeen (string mobName)
        {
            if (string.IsNullOrEmpty(mobName)) return;
            if (excludedPrefixes.Any(p => mobName.StartsWith(p))) return;
            if (!Seen.Add(mobName)) return;

            PlayerPrefs.SetString(SeenKey, string.Join(",", Seen.ToArray()));
            PlayerPrefs.Save();
            Debug.Log($"[MonsterCodex] 解鎖怪物圖鑑：{mobName}（目前 {Seen.Count} 筆）");

            // 用 AllSeen 而不是「這次剛好補滿最後一隻」：Steam 沒就緒時 Unlock 會直接放棄，
            // 只在補滿的那一瞬間判一次的話，那次掉了就永遠補不回來。重複呼叫是安全的。
            if (AllSeen)
                AchievementManager.Instance?.Unlock(AchievementManager.ACH_MONSTER_CODEX_FULL);
        }

        /// <summary>清空解鎖紀錄，測試用。</summary>
        public static void ResetAll ()
        {
            cachedSeen = null;
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
            Debug.Log("[MonsterCodex] 已清空怪物圖鑑解鎖紀錄");
        }

        /// <summary>全部解鎖，測試用。</summary>
        public static void UnlockAll ()
        {
            foreach (var mob in AllMobs)
                RecordSeen(mob.name);
        }

        /// <summary>PlayerPrefs 被外部清掉（例如 ProgressResetter 的 DeleteAll）之後要叫這個。</summary>
        public static void InvalidateCache ()
        {
            cachedSeen = null;
        }
    }
}
