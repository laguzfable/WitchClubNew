using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum EMapType {Empty, Event, Combat, StartPoint };

public class MapPoint : MonoBehaviour {

    public MapPoint[] relatePoints;

    [SerializeField, EnumToggleButtons()]
    EMapType mapType;

    public bool canMove = false;

    MapPlayerController PC;

    // Use this for initialization
    void Start () {

        PC = GameObject.FindGameObjectWithTag("Player").GetComponent<MapPlayerController>();
        PC.AddMapPoint(this);
        canMove = true;// for test purpose.
    }

    public EMapType GetMapType()
    {
        return mapType;
    }

    void OnMouseUp()
    {
        Debug.Log("OnClick" + gameObject.name);

        PC.GotoPoint(this);

    }
}
