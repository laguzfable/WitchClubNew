using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{

    string[,] dataArr;

    int language = 1;// default 1 = zh-tw


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetData(string str)
    {
        dataArr = CSVReader.SplitCsvGrid(str);
    }

    string GetDataByID(int id)
    {
        return dataArr[language, id];
    }

    public void ParseCommand(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }
        if (!str[0].Equals('#'))
        {
            ParseDialog(str);
            return;
        }
        var param = str.Split(" "[0]);
        var cmd = param[0];

        switch (cmd)
        {
            case "#bg":
                break;
            case "#bgm":
                break;
            case "#se":
                break;
            case "#mood":
                break;
            case "#into":
                break;
            case "#cg":
                break;
            case "#goto":
                break;
            case "#add":
                break;
            case "#sub":
                break;
            case "#combat":
                break;
            case "#sel":
                break;
        }
    }

    void ParseDialog(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        var param = str.Split(":"[0]);

        Debug.Log($"Name : {param[0]}");
        Debug.Log($"Dialogue content : {param[1]}");
    }
}
