using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MapPlayerController : MonoBehaviour {

    PlayerMark mark;

    public MapPoint curP;

    List<MapPoint> allPoint = new List<MapPoint>();

    public float moveSpeed;
    // Use this for initialization
    void Start () {

        mark = GetComponentInChildren<PlayerMark>();
	}

    public void AddMapPoint(MapPoint p)
    {
        allPoint.Add(p);
        if(p.GetMapType() == EMapType.StartPoint)
        {
            curP = p;
        }
    }
    
    public void GotoPoint(MapPoint p)
    {
        if(!p.canMove)
        {
            return;
        }
        //計算玩家所在的點和傳入的點 然後慢慢移動過去
        //遞迴走迷宮
        List<List<MapPoint>> allMovePath = new List<List<MapPoint>>();
        List<MapPoint> searchedPoint = new List<MapPoint>();

        searchedPoint.Clear();
        MapNode newNode = new MapNode();
        newNode.parent = null;
        newNode.data = p;

        FindPoint(p, newNode, allMovePath, searchedPoint);

        List<MapPoint> movePath = allMovePath[0];
        //Debug.Log("count = " + movePath.Count);
        if (allMovePath.Count > 1)
        {
            for (int i = 1; i < allMovePath.Count; i++)
            {
                if(allMovePath[i].Count < movePath.Count)
                {
                    movePath = allMovePath[i];
                }
            }
        }

        StartCoroutine(MoveToPoint(movePath));
    }

    IEnumerator MoveToPoint(List<MapPoint> movePath)
    {
        WaitForSeconds waitSec = new WaitForSeconds(moveSpeed);
        for (int i = 0; i < movePath.Count; i++)
        {
            transform.DOMove(movePath[i].transform.position, moveSpeed).SetEase(Ease.Linear);
            curP = movePath[i];
            yield return waitSec;
        }
    }

    void FindPoint(MapPoint p, MapNode node, List<List<MapPoint>> allMovePath, List<MapPoint> searchedPoint)
    {
        if (p == curP)
        {
            Debug.Log("Found!");
            MapNode target = node;
            var movePath = new List<MapPoint>();
            while(target.parent != null)
            {
                movePath.Add(target.data);
                target = target.parent;
            }
            allMovePath.Add(movePath);
            return;
        }

        MapNode newNode = new MapNode();
        newNode.parent = node;
        newNode.data = p;
        searchedPoint.Add(p);
        foreach (var map in p.relatePoints)
        {
            if(!searchedPoint.Contains(map))
            {
                FindPoint(map, newNode, allMovePath, searchedPoint);
            }
        }
    }

    class MapNode
    {
        public MapNode parent;
        public MapPoint data;
    }
}


