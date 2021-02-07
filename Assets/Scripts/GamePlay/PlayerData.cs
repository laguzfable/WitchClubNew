using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData
{
    static PlayerData instance;
    private static object m_Lock = new object();

    public static PlayerData Instance
    {
        get
        {
            lock (m_Lock)
            {
                if (instance == null)
                {
                    instance = new PlayerData();
                    instance.Reset();
                }
                return instance;
            }
        }
    }

    public int money = 0;

    public string playerName;

    public Inventory inventory { private set; get; } = new Inventory();

    public string[] usingRuneIDs = new string[4] { "2", "1", "8", "4" };

    public void Reset()
    {
        money = 10000;
        inventory.Reset();
        //inventory.AddItem("0", 5);
        //inventory.AddItem("TestArm");
        //inventory.AddItem("TestLeg");
        //inventory.AddItem("TestLeg1");
        //inventory.AddItem("TestArm1");
        //Toolbox.Instance.GetOrAddComponent<NPCPerferenceDataCollection>().InitPerferenceData();
    }
}
