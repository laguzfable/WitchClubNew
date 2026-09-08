using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 主線用 @overrideEvent 排好、但還沒演到的地圖特殊事件。
///
/// ★ 為什麼要存進 PlayerPrefs ★
/// 這裡原本只有一個 static dictionary，只活在記憶體裡，於是兩個方向都會出錯：
///   ‧ 玩家在「事件排好了、還沒走到那個地圖日」的中間關掉遊戲 → 預約消失，
///     引導被跳過，主線缺一塊；
///   ‧ 反過來，回標題重開或從蝕之聖典跳節點時它又不會自己消失，
///     上一輪的預約會綁架下一輪的第一個地圖日（涅莉的第六次引導跑到序章後面）。
/// 所以現在改成「寫進 PlayerPrefs、並且在每個『這一輪結束了』的入口明確清掉」，
/// 存活範圍才跟玩家的認知一致。清除入口見 <see cref="ClearAll"/>。
///
/// ★ 動畫存的是路徑不是物件 ★
/// RuntimeAnimatorController 是資產參照，序列化不進 PlayerPrefs，
/// 所以記 @overrideEvent 給的 Resources 路徑，讀回來時再 Load 一次。
/// </summary>
public static class MapSpecialOverride
{
    const string Key = "WC/MapSpecialOverride/v1";

    private static Dictionary<string, MapSpecialOverrideData> dict
        = new Dictionary<string, MapSpecialOverrideData>();

    static MapSpecialOverride ()
    {
        Load();
    }

    public static void SetOverride(MapSpecialOverrideData data)
    {
        if (data == null || string.IsNullOrEmpty(data.characterName)) return;

        dict[data.characterName] = data;
        Save();
        Debug.Log($"[Override] 設定：{data.characterName} → {data.eventName}");
    }

    public static bool TryGet(string characterName, out MapSpecialOverrideData data)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            data = null;
            return false;
        }

        return dict.TryGetValue(characterName, out data);
    }

    public static void Clear(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return;

        if (dict.Remove(characterName))
            Save();
    }

    /// <summary>
    /// 丟掉所有還沒被消耗的預約。「這一輪結束了」的每個入口都要呼叫：
    /// @exitToTitle（回標題）、蝕之聖典跳節點、F10 清進度。
    /// </summary>
    public static void ClearAll()
    {
        if (dict.Count == 0)
        {
            // 記憶體是空的不代表檔案是空的（例如這個 session 還沒載過），所以照樣清一次。
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            return;
        }

        Debug.Log($"[Override] 清掉還沒消耗的預約：{string.Join("、", dict.Keys.ToArray())}");
        dict.Clear();
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 丟掉記憶體裡那份，重新從 PlayerPrefs 讀。
    /// 讀存檔時 PlayerPrefs 會被整包換掉（見 RunSnapshot），
    /// 不重讀的話這裡還是上一輪的預約。
    /// </summary>
    public static void Reload () => Load();

    // ============================================================
    //  存讀：一行一筆，欄位用 \t 隔開（劇本名和角色名都不會有 tab）
    //  characterName \t eventName \t naninovelScript \t animatorPath \t offsetX \t offsetY
    // ============================================================

    static void Save()
    {
        if (dict.Count == 0)
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            return;
        }

        var lines = dict.Values.Select(d => string.Join("\t", new[]
        {
            d.characterName ?? "",
            d.eventName ?? "",
            d.naninovelScript ?? "",
            d.animatorPath ?? "",
            d.offset.x.ToString(System.Globalization.CultureInfo.InvariantCulture),
            d.offset.y.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));

        PlayerPrefs.SetString(Key, string.Join("\n", lines.ToArray()));
        PlayerPrefs.Save();
    }

    static void Load()
    {
        dict.Clear();

        var raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return;

        foreach (var line in raw.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;

            var f = line.Split('\t');
            if (f.Length < 6 || string.IsNullOrEmpty(f[0]))
            {
                Debug.LogWarning($"[Override] 讀不懂的預約紀錄，略過：{line}");
                continue;
            }

            var data = new MapSpecialOverrideData
            {
                characterName = f[0],
                eventName = f[1],
                naninovelScript = f[2],
                animatorPath = f[3],
                offset = new Vector2(ParseFloat(f[4]), ParseFloat(f[5]))
            };

            // 動畫是資產，存的是 Resources 路徑，這裡再撈回來。
            // 撈不到不算致命：icon 少一段動畫，但事件照樣播得出來。
            if (!string.IsNullOrEmpty(data.animatorPath))
            {
                data.animator = Resources.Load<RuntimeAnimatorController>(data.animatorPath);
                if (data.animator == null)
                    Debug.LogWarning($"[Override] 讀回預約時載不到動畫：{data.animatorPath}");
            }

            dict[data.characterName] = data;
        }

        if (dict.Count > 0)
            Debug.Log($"[Override] 讀回上次留下的預約：{string.Join("、", dict.Keys.ToArray())}");
    }

    static float ParseFloat(string s)
    {
        float v;
        return float.TryParse(s, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0f;
    }
}
