using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasAutoScaler : MonoBehaviour {

    // Use this for initialization
    void Start () {

        var scaler = GetComponent<CanvasScaler>();
        scaler.scaleFactor = /*Screen.dpi > 300 && Screen.width >= 2048*/Screen.width >= 2048 || Application.platform == RuntimePlatform.IPhonePlayer? 2f : 1f;
    }
}
