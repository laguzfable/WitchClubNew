// Assets/Scripts/UI/NewItemTracker.cs
//
// 「有沒有看過」的共用紀錄。給筆記本、蝕之聖典節點、怪物圖鑑、回憶模式一起用。
//
// ★ 跟「解鎖」是兩件事 ★
// 解鎖＝玩家拿到了；看過＝玩家真的點開讀了。NEW 標的是「已解鎖但還沒點開」，
// 所以要另外記一份，不能拿解鎖狀態當條件。
//
// ★ 為什麼用 PlayerPrefs ★
// 跟怪物圖鑑的 MonsterCodex_Seen 同一套。這是「玩家這台機器看過什麼」，
// 不屬於某一次存檔，也不該被讀檔倒回去——你讀舊檔不會忘記自己讀過那一頁。
//
// ★ 什麼算「看過」★
// 進頁面就算，不是點開那一項才算。所以玩家打開筆記本看到一排 NEW，
// 下次再打開就只剩這段期間新拿到的——他不必為了消掉角標一條一條點過去。
//
// 實作上靠「一次瀏覽」的快照：BeginVisit 清空快照，之後每個項目第一次被畫出來時
// 就當場記成看過，但快照會讓它在這一次瀏覽期間持續顯示 NEW，不會畫到一半自己消失。
//
// 用法：
//   NewItemTracker.BeginVisit(Notes)          → 面板打開時呼叫一次
//   NewItemTracker.IsNewThisVisit(Notes, id)  → 畫每一項時問（問了就等於看過了）
//   NewItemTracker.HasAnyNew(Notes, ids)      → 入口按鈕上要不要掛紅點

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NewItemTracker
{
    /// <summary>各個收集頁面各記一份，key 用得開才不會互相干擾。</summary>
    public const string Notes = "Notes";
    public const string CodexNodes = "CodexNodes";
    public const string Monsters = "Monsters";
    public const string CGs = "CGs";

    const string KeyPrefix = "Seen_";

    // 每次查都去 PlayerPrefs 撈字串再 Split 的話，畫一頁 20 條就撈 20 次。
    static readonly Dictionary<string, HashSet<string>> cache = new Dictionary<string, HashSet<string>>();

    static HashSet<string> SeenIn (string surface)
    {
        if (cache.TryGetValue(surface, out var set)) return set;

        var raw = PlayerPrefs.GetString(KeyPrefix + surface, string.Empty);
        set = new HashSet<string>(raw.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0));
        cache[surface] = set;
        return set;
    }

    /// <summary>這一項已經解鎖、但玩家還沒看過。純查詢，不會改狀態。</summary>
    public static bool IsNew (string surface, string id)
    {
        return !string.IsNullOrEmpty(id) && !SeenIn(surface).Contains(id);
    }

    // 這一次瀏覽期間，哪些項目要顯示 NEW。面板重畫好幾次也是同一份答案。
    static readonly Dictionary<string, Dictionary<string, bool>> visit =
        new Dictionary<string, Dictionary<string, bool>>();

    /// <summary>面板打開時呼叫一次，開始新的一輪瀏覽。</summary>
    public static void BeginVisit (string surface)
    {
        visit[surface] = new Dictionary<string, bool>();
    }

    /// <summary>
    /// 這一項在這次瀏覽要不要標 NEW。第一次問的時候就當場記成看過——
    /// 「畫出來給玩家看到」就是看過，不用等他點。
    /// </summary>
    public static bool IsNewThisVisit (string surface, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        if (!visit.TryGetValue(surface, out var answers))
        {
            answers = new Dictionary<string, bool>();
            visit[surface] = answers;
        }

        if (answers.TryGetValue(id, out var cached)) return cached;

        var isNew = IsNew(surface, id);
        answers[id] = isNew;
        if (isNew) MarkSeen(surface, id);
        return isNew;
    }

    /// <summary>直接記成看過。重複呼叫沒有副作用。</summary>
    public static void MarkSeen (string surface, string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var set = SeenIn(surface);
        if (!set.Add(id)) return;

        PlayerPrefs.SetString(KeyPrefix + surface, string.Join(",", set));
        PlayerPrefs.Save();
    }

    /// <summary>這一整頁裡還有沒有沒看過的。給入口按鈕掛紅點用。</summary>
    public static bool HasAnyNew (string surface, IEnumerable<string> unlockedIds)
    {
        if (unlockedIds == null) return false;
        var set = SeenIn(surface);
        return unlockedIds.Any(id => !string.IsNullOrEmpty(id) && !set.Contains(id));
    }

    /// <summary>開新遊戲之類要清掉時用。</summary>
    public static void Clear (string surface)
    {
        cache.Remove(surface);
        visit.Remove(surface);
        PlayerPrefs.DeleteKey(KeyPrefix + surface);
        PlayerPrefs.Save();
    }
}
