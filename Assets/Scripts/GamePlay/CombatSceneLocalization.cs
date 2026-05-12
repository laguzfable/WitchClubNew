using Naninovel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSceneLocalization : MonoBehaviour
{

    [SerializeField] TextAsset csvFile;

    Dictionary<string, Dictionary<string, string>> localeMap = new Dictionary<string, Dictionary<string, string>>();

    // Use this for initialization
    void Awake()
    {
        // CSVReader.SplitCsvGrid 會跳過第一行（header），因此 grid 從第一筆資料開始。
        // 我們需要自行讀取 header 行來取得語言碼（zh-TW, en 等）。
        string[] rawLines = csvFile.text.Split('\n');
        string[] headers = rawLines[0].TrimEnd('\r').Split(',');
        // headers[0] = "ID", headers[1] = "zh-TW", headers[2] = "en", ...

        string[,] grid = CSVReader.SplitCsvGrid(csvFile.text);
        // grid[x, y]：CSVReader 已跳過 header，故 grid[*, 0] 是第一筆資料

        for (var i = 0; i < grid.GetLength(1); i++)
        {
            if (string.IsNullOrWhiteSpace(grid[0, i])) continue;

            var dict = new Dictionary<string, string>();
            for (var n = 1; n < headers.Length && n < grid.GetLength(0); n++)
            {
                string locale = headers[n].Trim('\r', ' ');
                if (!string.IsNullOrWhiteSpace(grid[n, i]))
                    dict[locale] = grid[n, i];
            }

            localeMap[grid[0, i]] = dict;
        }

        Debug.Log($"[CombatLocalization] 載入完成，共 {localeMap.Count} 筆，語言欄位：{string.Join(", ", headers, 1, headers.Length - 1)}");
    }

    public string GetLocalizedContent(string id, string defaultString = "")
    {
        if(!string.IsNullOrWhiteSpace(id))
        {
            var map = new Dictionary<string, string>();
            if (localeMap.TryGetValue(id, out map))
            {
                var locale = GetCurrentLocale();
                if (map.TryGetValue(locale, out var localized))
                    return localized;
                if (map.TryGetValue("zh-TW", out var fallback))
                    return fallback;
            }
        }

        return defaultString;
    }

    private string GetCurrentLocale()
    {
        // 優先從 PlayerPrefs 讀（玩家在語言選擇畫面設定的）
        var lang = PlayerPrefs.GetString("Language", "");
        if (!string.IsNullOrEmpty(lang))
        {
            if (lang.ToLower().StartsWith("en")) return "en";
            if (lang.ToLower().StartsWith("zh")) return "zh-TW";
            if (lang.ToLower().StartsWith("ja")) return "ja";
        }

        // Fallback：用 Naninovel 的 locale
        if (Engine.Initialized)
        {
            var locale = Engine.GetService<ILocalizationManager>().SelectedLocale;
            if (!string.IsNullOrEmpty(locale))
                return locale;
        }

        return "zh-TW";
    }

    public string GetCurLanguage()
    {
        if (Engine.Initialized)
            return Engine.GetService<ILocalizationManager>().SelectedLocale;

        // fallback to PlayerPrefs
        var lang = PlayerPrefs.GetString("Language", "");
        if (lang.ToLower().StartsWith("en"))   return "en";
        if (lang.ToLower().StartsWith("ja"))   return "ja";
        return "zh-TW";
    }

}