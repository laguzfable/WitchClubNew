using UnityEngine;
using Naninovel;

/// <summary>
/// 夜晚儀式的好感門檻。
///
/// 每個角色有五場儀式，第 N 場需要好感 ≥ N×<see cref="Step"/>（20/40/60/80/100）。
/// 好感不夠時不是把角色從地圖上拿掉，而是改播「閒聊」劇本——玩家還是有事做、
/// 還是能拿到一點好感（閒聊本身 +5），只是儀式進度不會前進。
///
/// 進度不前進是靠 MapCharacterSpawner 既有的 specialEvents 機制：
/// 被當成特殊事件的話點下去不會 IncrementNightProgress。
/// </summary>
public static class RitualGate
{
    /// <summary>每一場儀式之間的好感間距。</summary>
    public const int Step = 20;

    /// <summary>
    /// 角色 → (好感變數名, 閒聊劇本)。
    /// 角色名是 MapCharacterSpawner 的 characterName，好感變數名是劇本裡 @set 的那些。
    ///
    /// ★ 西碧兒刻意不在這張表裡 ★
    /// 這裡管的是「夜晚儀式」——要打贏才算過關、贏了給符文（green01～05 每支都有 @battle）。
    /// 西碧兒那五場 syb_day01～05 一個 @battle 都沒有，是純劇情養成，所以兩條規則都不適用：
    ///   ‧ 門檻會擋死她：第一場要好感 ≥20，可是玩家進綠線時只有「告訴西碧兒」那 +5，
    ///     三個地圖夜會全部被換成閒聊，syb_day01 一次都見不到。
    ///   ‧ 進度會卡住：掛了門檻的角色在地圖上點下去不推進進度，要等戰鬥勝利才推進，
    ///     但 CharacterOf 只認得 blue/red/yellow/green，永遠等不到她的那一場。
    /// 沒列在這張表裡的角色一律照舊：點了就推進、不驗好感。
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, (string variable, string chatScript)> Table =
        new System.Collections.Generic.Dictionary<string, (string, string)>
        {
            { "Eupie", ("affinity_Eup", "chat_eupie")  },
            { "Mel",   ("affinity_Mel", "chat_mel")    },
            { "Nelly", ("affinity_Nel", "chat_nelly")  },
            { "Vedia", ("affinity_Ved", "chat_vedia")  },
        };

    /// <summary>這個角色有沒有掛門檻（沒列在表裡的角色一律照舊）。</summary>
    public static bool HasGate (string characterName)
    {
        return !string.IsNullOrEmpty(characterName) && Table.ContainsKey(characterName);
    }

    /// <summary>第 index+1 場儀式需要的好感。index 是 0-based 的 NightProgress。</summary>
    public static int RequiredAffinity (int nightEventIndex)
    {
        return (nightEventIndex + 1) * Step;
    }

    /// <summary>閒聊劇本名；沒有設定的話回 null。</summary>
    public static string ChatScript (string characterName)
    {
        return Table.TryGetValue(characterName, out var e) ? e.chatScript : null;
    }

    /// <summary>
    /// 這場儀式現在能不能做。沒掛門檻的角色一律 true。
    /// 引擎沒起來（讀不到變數）時也回 true——寧可讓劇情繼續，也不要把玩家鎖在地圖上。
    /// </summary>
    public static bool CanPerform (string characterName, int nightEventIndex)
    {
        if (!Table.TryGetValue(characterName, out var entry)) return true;

        var need = RequiredAffinity(nightEventIndex);
        var have = ReadAffinity(entry.variable);
        if (have < 0) return true;

        var ok = have >= need;
        Debug.Log($"[RitualGate] {characterName} 第 {nightEventIndex + 1} 場儀式：" +
                  $"好感 {have} / 需要 {need} → {(ok ? "可以做" : "改成閒聊")}");
        return ok;
    }

    /// <summary>
    /// 現在的好感。沒掛門檻或讀不到時回 -1。
    /// 地圖上顯示「好感 45/60」用的——同一份數字，只是提前給玩家看。
    /// </summary>
    public static int CurrentAffinity (string characterName)
    {
        if (!Table.TryGetValue(characterName, out var entry)) return -1;
        return ReadAffinity(entry.variable);
    }

    /// <summary>
    /// 儀式打贏之後推進夜晚進度。由 CombatSystem 在勝利結算時呼叫，
    /// monsterID 就是儀式的 target（blue01～green05）。
    ///
    /// ★ 為什麼推進要放在這裡 ★
    /// 原本是在地圖上點下 icon 的當下就 IncrementNightProgress，等於輸贏都算過關，
    /// 打輸的人就永遠錯過那場儀式（也拿不到符文，因為符文只有贏才給）。
    /// 移到勝利結算之後，輸了下個夜晚同一場儀式還在，可以重打。
    /// </summary>
    public static void AdvanceOnRitualWin (string monsterId)
    {
        var character = CharacterOf(monsterId);
        if (character == null) return;

        // ★ 直接寫 PlayerPrefs，不透過 StoryProgressManager ★
        // 那個管理器只活在地圖場景（它的 DontDestroyOnLoad 沒有生效），
        // 戰鬥場景裡拿不到 Instance，走它的話進度永遠不會推進。
        var key = character + NightKeySuffix;
        var next = PlayerPrefs.GetInt(key, 0) + 1;
        PlayerPrefs.SetInt(key, next);
        PlayerPrefs.Save();

        // 地圖那邊如果還活著，它的快取要作廢，不然回去會讀到舊值。
        if (StoryProgressManager.Instance != null)
            StoryProgressManager.Instance.InvalidateCache();

        Debug.Log($"[RitualGate] {character} 的儀式 {monsterId} 通過，夜晚進度 → {next}");
    }

    /// <summary>
    /// 這個角色目前卡在第幾場儀式（0-based，就是 NightProgress）。
    /// 閒聊劇本用它決定要播哪一段——不然玩家每次來都聽到同一句。
    /// </summary>
    public static int Stage (string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return 0;
        return PlayerPrefs.GetInt(characterName + NightKeySuffix, 0);
    }

    /// <summary>StoryProgressManager 存夜晚進度用的 key 尾巴，兩邊必須一致。</summary>
    const string NightKeySuffix = "_Event_Night";

    /// <summary>blue01 → Eupie，red03 → Mel……不是儀式的 id 回 null。</summary>
    public static string CharacterOf (string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId)) return null;

        var id = monsterId.Trim().ToLower();
        if (id.StartsWith("blue")) return "Eupie";
        if (id.StartsWith("red")) return "Mel";
        if (id.StartsWith("yellow")) return "Nelly";
        if (id.StartsWith("green")) return "Vedia";
        return null;
    }

    /// <summary>讀不到就回 -1（代表「別擋」）。</summary>
    static int ReadAffinity (string variableName)
    {
        if (!Engine.Initialized) return -1;

        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars == null) return -1;

        var raw = vars.GetVariableValue(variableName);
        if (string.IsNullOrEmpty(raw)) return 0; // 沒設過＝還沒加過好感＝0

        return int.TryParse(raw, out var value) ? value : -1;
    }
}
