using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData : Singleton<PlayerData>
{
    // public string playerName;

    // ✅ 正確預設：基礎四張符文 (blue00/red00/yellow00/green00)
    public string[] usingRuneIDs = new string[4] { "blue00", "red00", "yellow00", "green00" };

    public void Reset()
    {
        // ✅ 重置時也恢復基礎符文
        usingRuneIDs = new string[4] { "blue00", "red00", "yellow00", "green00" };
    }
}
