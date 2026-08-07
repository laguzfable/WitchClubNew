using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 在 Naninovel 腳本中解鎖普通卡的 A/B 變體。
///
/// 用法範例：
///   @unlockCardVariant ids:RedA
///   @unlockCardVariant ids:RedA,RedB
///   @unlockCardVariant ids:BlueA,GreenB,YellowA
///
/// id 格式為「屬性名稱(Blue/Red/Green/Yellow) + 變體字母(A/B)」，多個 ID 用半型逗號隔開（不含空格）。
/// 只會「追加」解鎖，不會清除已有的資料。
/// </summary>
[CommandAlias("unlockCardVariant")]
public class UnlockCardVariantCommand : Command
{
    [ParameterAlias("ids")]
    public StringParameter IDs;

    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        if (!Assigned(IDs) || string.IsNullOrWhiteSpace(IDs.Value))
        {
            Debug.LogWarning("[unlockCardVariant] 未指定 ids 參數");
            return UniTask.CompletedTask;
        }

        var toUnlock = IDs.Value.Split(',');

        foreach (var rawID in toUnlock)
        {
            var id = rawID.Trim();
            if (string.IsNullOrEmpty(id)) continue;

            string element = GetElementName(id);
            string variant = GetVariant(id);
            if (element == null || variant == null)
            {
                Debug.LogWarning($"[unlockCardVariant] 無法判斷屬性/變體：'{id}'，跳過");
                continue;
            }

            string key = "UnlockedCardVariant_" + element;
            string current = PlayerPrefs.GetString(key, "");

            var list = current.Length > 0
                ? new System.Collections.Generic.List<string>(current.Split(','))
                : new System.Collections.Generic.List<string>();

            if (!list.Contains(variant))
            {
                list.Add(variant);
                PlayerPrefs.SetString(key, string.Join(",", list));
                Debug.Log($"[unlockCardVariant] 解鎖 {element}{variant}（key={key}）");
            }
            else
            {
                Debug.Log($"[unlockCardVariant] {element}{variant} 已解鎖，跳過");
            }
        }

        PlayerPrefs.Save();

        return UniTask.CompletedTask;
    }

    // id 例如 "RedA" → 屬性名稱 "Red"（對應 ECardElement 名稱）
    static string GetElementName(string id)
    {
        if (id.Length < 2) return null;
        string element = id.Substring(0, id.Length - 1);
        switch (element)
        {
            case "Blue":
            case "Red":
            case "Green":
            case "Yellow":
                return element;
            default:
                return null;
        }
    }

    // id 例如 "RedA" → 變體 "A"
    static string GetVariant(string id)
    {
        if (id.Length < 2) return null;
        string variant = id.Substring(id.Length - 1);
        return (variant == "A" || variant == "B") ? variant : null;
    }
}
