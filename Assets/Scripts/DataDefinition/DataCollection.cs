using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DataCollection<T> where T : ScriptableObjectID
{
    Dictionary<string, T> dataTable = new Dictionary<string, T>();

    public void Init(string resourcePath)
    {
        var dataArr = Resources.LoadAll<T>(resourcePath);
        foreach (var data in dataArr)
        {
            dataTable.Add(data.name, data);
        }
    }

    public int GetLength()
    {
        return dataTable.Values.Count;
    }

    public T GetDataByID(string id)
    {
        foreach (var data in dataTable.Values)
        {
            if (data.ID.Equals(id))
            {
                return data;
            }
        }
        return null;
    }

    public T GetDataByName(string name)
    {
        if (dataTable.ContainsKey(name))
        {
            return dataTable[name];
        }
        return null;
    }

    public void DisplayItemNames()
    {
        foreach (var data in dataTable.Values)
        {
            Debug.Log("item name : " + data.name);
        }
    }
}
