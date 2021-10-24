using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData : Singleton<PlayerData>
{
    // public string playerName;

    public string[] usingRuneIDs = new string[4] { "blue01", "red01", "green01", "yellow01" };

    public void Reset()
    {
        // usingRuneIDs[(int)ECardElement.Red] = "艾妮(血系)";
        // Instance.usingRuneIDs[(int)ECardElement.Green] = "樹女";
        // Instance.usingRuneIDs[(int)ECardElement.Blue] = "赫菲";
        usingRuneIDs = new string[4] { "blue01", "red01", "green01", "yellow01" };
    }
}
