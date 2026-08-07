using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData : Singleton<PlayerData>
{
    // public string playerName;

    // ✅ 正確預設：基礎四張符文 (blue00/red00/yellow00/green00)
    public string[] usingRuneIDs = new string[4] { "blue00", "red00", "yellow00", "green00" };

    // ✅ 普通卡型態選擇，索引對應 ECardElement (Blue/Red/Yellow/Green)，空字串＝原版
    public string[] usingCardVariant = new string[4] { "", "", "", "" };

    public void Reset()
    {
        // ✅ 重置時也恢復基礎符文
        usingRuneIDs = new string[4] { "blue00", "red00", "yellow00", "green00" };
        usingCardVariant = new string[4] { "", "", "", "" };
    }

    static readonly string[] ElementNames = { "Blue", "Red", "Yellow", "Green" };

    protected override void Init()
    {
        // 把玩家上次選好、存在 PlayerPrefs 裡的符文/卡片型態讀回來，
        // 不然重開遊戲後這裡永遠只會是預設值（PlayerPrefs 只有寫，沒有人讀回來過）。
        for (int i = 0; i < ElementNames.Length; i++)
        {
            var equippedRune = PlayerPrefs.GetString($"Equipped_{ElementNames[i]}", "");
            if (!string.IsNullOrEmpty(equippedRune))
                usingRuneIDs[i] = equippedRune;

            usingCardVariant[i] = PlayerPrefs.GetString($"EquippedCardVariant_{ElementNames[i]}", "");
        }
    }
}
