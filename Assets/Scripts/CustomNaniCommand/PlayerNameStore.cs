using Naninovel;
using UnityEngine;

/// <summary>
/// 玩家名字的長期保管處。
///
/// ★ 為什麼需要這個 ★
/// PlayerName 是 Naninovel 的自訂變數，而 @exitToTitle 會 ResetStateAsync()
/// 把自訂變數全部清空（那是為了好感度不要帶到下一輪）。從蝕之聖典直接跳進
/// 某個節點時，劇本並沒有重新問過名字，所有 {PlayerName} 就會變成空白。
///
/// 所以取名之後另外存一份到 PlayerPrefs，進節點前再放回去。
/// </summary>
public static class PlayerNameStore
{
    const string Key = "WC/PlayerName";

    /// <summary>沒存過名字時用的預設值，跟 @setPlayerName 那支同一套。</summary>
    public static string Fallback
    {
        get
        {
            var lang = PlayerPrefs.GetString("Language", "zh-TW").ToLower();
            return lang.StartsWith("en") ? "Traveler" : "旅人";
        }
    }

    public static string Saved => PlayerPrefs.GetString(Key, "");

    public static void Save(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        PlayerPrefs.SetString(Key, name);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerNameStore] 記住玩家名字：{name}");

        // 取名是作弊碼的入口：對得上就把收集要素一次打開（見 CheatUnlock）。
        CheatUnlock.TryActivate(name);
    }

    /// <summary>
    /// 變數是空的就補回去。已經有值的話不動——正常play through 的名字才是最新的。
    /// </summary>
    public static void RestoreIfMissing(ICustomVariableManager vars)
    {
        if (vars == null) return;

        var current = vars.GetVariableValue("PlayerName");
        if (!string.IsNullOrEmpty(current)) return;

        var name = Saved;
        if (string.IsNullOrEmpty(name)) name = Fallback;

        vars.SetVariableValue("PlayerName", name);
        Debug.Log($"[PlayerNameStore] PlayerName 是空的，補回 {name}");
    }
}

/// <summary>
/// @restorePlayerName
/// 把存起來的名字放回 PlayerName 變數（不管現在有沒有值）。
/// 給「我們是不是見過？」那段用。
/// </summary>
[CommandAlias("restorePlayerName")]
public class RestorePlayerNameCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var vars = Engine.GetService<ICustomVariableManager>();
        var name = PlayerNameStore.Saved;
        if (vars != null && !string.IsNullOrEmpty(name))
            vars.SetVariableValue("PlayerName", name);

        return UniTask.CompletedTask;
    }
}

/// <summary>
/// @rememberPlayerName
/// 把目前的 PlayerName 存起來，之後從蝕之聖典進節點時才有名字可以用。
/// 放在 chapter0 問完名字之後。
/// </summary>
[CommandAlias("rememberPlayerName")]
public class RememberPlayerNameCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var vars = Engine.GetService<ICustomVariableManager>();
        PlayerNameStore.Save(vars?.GetVariableValue("PlayerName"));
        return UniTask.CompletedTask;
    }
}
