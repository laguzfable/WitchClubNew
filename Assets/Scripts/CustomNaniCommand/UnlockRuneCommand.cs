using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 在 Naninovel 腳本中解鎖指定符文。
///
/// 用法範例：
///   @unlockRune all:true                              ← 全部解鎖
///   @unlockRune ids:blue01,blue02,blue03
///   @unlockRune ids:red01,red02,red03,red04,red05
///   @unlockRune ids:blue01,blue02,red01,green01,yellow01
///
/// 多個 ID 用半型逗號隔開（不含空格）。
/// 只會「追加」解鎖，不會清除已有的資料。
/// </summary>
[CommandAlias("unlockRune")]
public class UnlockRuneCommand : Command
{
    [ParameterAlias("ids")]
    public StringParameter IDs;

    [ParameterAlias("all")]
    public BooleanParameter UnlockAll;

    static readonly string[] AllIDs = {
        "blue01","blue02","blue03","blue04","blue05",
        "red01","red02","red03","red04","red05",
        "yellow01","yellow02","yellow03","yellow04","yellow05",
        "green01","green02","green03","green04","green05",
        "mon02","mon04","mon08","mon09","mon10","mon12"
    };

    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        // all:true → 直接寫死完整清單，最快
        if (Assigned(UnlockAll) && UnlockAll.Value)
        {
            PlayerPrefs.SetString("UnlockedRunes_Blue",   "blue01,blue02,blue03,blue04,blue05");
            PlayerPrefs.SetString("UnlockedRunes_Red",    "red01,red02,red03,red04,red05");
            PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
            PlayerPrefs.SetString("UnlockedRunes_Green",  "green01,green02,green03,green04,green05");
            PlayerPrefs.SetString("UnlockedRunes_None",   "mon02,mon04,mon08,mon09,mon10,mon12");
            PlayerPrefs.Save();
            Debug.Log("[unlockRune] 全部符文已解鎖");
            return UniTask.CompletedTask;
        }

        if (!Assigned(IDs) || string.IsNullOrWhiteSpace(IDs.Value))
        {
            Debug.LogWarning("[unlockRune] 未指定 ids 或 all 參數");
            return UniTask.CompletedTask;
        }

        var toUnlock = IDs.Value.Split(',');

        foreach (var rawID in toUnlock)
        {
            var id = rawID.Trim();
            if (string.IsNullOrEmpty(id)) continue;

            // 判斷屬性（ECardElement enum 名稱）
            string elementName = GetElementName(id);
            if (elementName == null)
            {
                Debug.LogWarning($"[unlockRune] 無法判斷屬性：'{id}'，跳過");
                continue;
            }

            string key = "UnlockedRunes_" + GetElementName(id);
            string current = PlayerPrefs.GetString(key, "");

            // 避免重複加入
            var list = current.Length > 0
                ? new System.Collections.Generic.List<string>(current.Split(','))
                : new System.Collections.Generic.List<string>();

            if (!list.Contains(id))
            {
                list.Add(id);
                PlayerPrefs.SetString(key, string.Join(",", list));
                Debug.Log($"[unlockRune] 解鎖 {id}（key={key}）");
            }
            else
            {
                Debug.Log($"[unlockRune] {id} 已解鎖，跳過");
            }
        }

        PlayerPrefs.Save();
        return UniTask.CompletedTask;
    }

    // Returns ECardElement enum name (matches SelectRuneCard key format)
    static string GetElementName(string id)
    {
        if (id.StartsWith("blue"))   return "Blue";
        if (id.StartsWith("red"))    return "Red";
        if (id.StartsWith("yellow")) return "Yellow";
        if (id.StartsWith("green"))  return "Green";
        if (id.StartsWith("mon"))    return "None";   // mon runes = ECardElement.None
        return null;
    }

    static int GetElement(string id)
    {
        if (id.StartsWith("blue"))   return 0;
        if (id.StartsWith("red"))    return 1;
        if (id.StartsWith("yellow")) return 2;
        if (id.StartsWith("green"))  return 3;
        if (id.StartsWith("mon"))    return 4;
        return -1;
    }
}
