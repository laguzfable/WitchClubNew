using Naninovel;
using UnityEngine;

/// <summary>
/// @setPlayerName
/// 根據 PlayerPrefs "Language" 自動設定 PlayerName 變數。
/// 在 demo.nani 裡取代 @set PlayerName="旅人"
/// </summary>
[CommandAlias("setPlayerName")]
public class SetPlayerNameCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var lang = PlayerPrefs.GetString("Language", "").ToLower();

        string name;
        if (lang.StartsWith("ja"))       name = "旅人";      // 日文：旅人（たびびと）
        else if (lang.StartsWith("zh"))  name = "旅人";      // 中文：旅人
        else                             name = "Traveler";  // 英文

        var vars = Engine.GetService<ICustomVariableManager>();
        vars.SetVariableValue("PlayerName", name);

        return UniTask.CompletedTask;
    }
}
