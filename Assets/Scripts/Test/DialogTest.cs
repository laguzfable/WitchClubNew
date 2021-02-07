using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTest : MonoBehaviour
{
    [SerializeField]
    TextAsset dialog;


    // Start is called before the first frame update
    void Start()
    {
        var str = dialog.text;
        string[,] grid = CSVReader.SplitCsvGrid(dialog.text);
        Debug.Log("size = " + (1 + grid.GetUpperBound(0)) + "," + (1 + grid.GetUpperBound(1)));

        CSVReader.DebugOutputGrid(grid);
    }
}
