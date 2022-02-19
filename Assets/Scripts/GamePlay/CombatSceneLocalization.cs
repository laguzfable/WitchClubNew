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
        string[,] grid = CSVReader.SplitCsvGrid(csvFile.text);

        /*
        var keys = new List<string>();
        for (var i = 1; i < grid.GetLength(1); i++)
        {
            if (string.IsNullOrWhiteSpace(grid[0, i]))
            {
                continue;
            }
            keys.Add(grid[0, i]);
            Debug.Log($"??? {grid[0, i]}");
        }*/

        for (var i = 1; i < grid.GetLength(1); i++)
        {
            var dict = new Dictionary<string, string>();            
            if (string.IsNullOrWhiteSpace(grid[0, i]))
            {
                continue;
            }
            var str = $"{grid[0, i]} : ";
            for (var n = 1; n < grid.GetLength(0); n++)
            {
                if (string.IsNullOrWhiteSpace(grid[n, i]))
                {
                    continue;
                }
                dict[grid[n, 0]] = grid[n, i];
                str += $"{grid[n, 0]} = {grid[n, i]},";
            }
            Debug.Log($"{str} | {i-1}");

            localeMap[grid[0, i]] = dict;
        }
    }

    public string GetLocalizedContent(string id, string defaultString = "")
    {
        if(!string.IsNullOrWhiteSpace(id))
        {
            var map = new Dictionary<string, string>();
            if (localeMap.TryGetValue(id, out map))
            {
                if (Engine.Initialized)
                {
                    return map[Engine.GetService<ILocalizationManager>().SelectedLocale];
                }
                else
                {
                    return map["zh-TW"];
                }
            }
        }
        
        return defaultString;
    }

}