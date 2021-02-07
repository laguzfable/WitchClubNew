using System;
using Kenaz;
using UnityEngine;

public class NPCPerferenceDataCollection : MonoBehaviour {
    
    NPCPerferenceData dataTable;
    private void Awake() {
        
        Toolbox.RegisterComponent<NPCPerferenceDataCollection>();

    }

    public void Save(string fileName)
    {
        FileHelper.WriteData(fileName, JsonUtility.ToJson(dataTable));
    }

    public void Load(string fileName)
    {
        dataTable = JsonUtility.FromJson<NPCPerferenceData>(FileHelper.LoadFile(fileName));
    }

    public void InitPerferenceData()
    {
        if (dataTable == null)
        {
            dataTable = new NPCPerferenceData();
        }
        dataTable.npcs = new PerferenceData[4];
        for(int i = 0; i < dataTable.npcs.Length; i++)
        {
            dataTable.npcs[i] = new PerferenceData();
        }
        dataTable.npcs[0].name = ENPCPerference.Enigmae.ToString();
        dataTable.npcs[0].value = 0;
        dataTable.npcs[1].name = ENPCPerference.Huffy.ToString();
        dataTable.npcs[1].value = 0;
        dataTable.npcs[2].name = ENPCPerference.Nelly.ToString();
        dataTable.npcs[2].value = 0;
        dataTable.npcs[3].name = ENPCPerference.Verdia.ToString();
        dataTable.npcs[3].value = 0;
    }

}

public enum ENPCPerference { Enigmae, Huffy, Nelly, Verdia };

[Serializable]
public class NPCPerferenceData
{
    public PerferenceData[] npcs;
}


[Serializable]
public class PerferenceData
{
    public string name;
    public int value;

}
