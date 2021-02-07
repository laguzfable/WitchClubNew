using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIEasyButton : MonoBehaviour {

    protected Button btn;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {

    }


    // Use this for initialization
    void Start () {

        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClickEvent);
        
    }

    protected virtual void OnClickEvent()
    {
        
    }
}
