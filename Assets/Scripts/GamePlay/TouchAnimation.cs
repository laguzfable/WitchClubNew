using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchAnimation : MonoBehaviour {
    Animator m_Animator;
    bool touchBool;
    Collider coll;
    bool touchingBool = true;
    [SerializeField]
    GameObject talkBubble;
    // Use this for initialization
    void Start () {
        m_Animator = gameObject.GetComponent<Animator>();
        coll = GetComponent<Collider>();
    }
	
	// Update is called once per frame
	void Update () {
        // Does the ray intersect any objects excluding the player layer
        if (Input.GetMouseButtonDown(0))
        {
            if (touchingBool) {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (coll.Raycast(ray, out hit, 100.0f))
                {
                    if (coll.gameObject == this.gameObject)
                    {
                        m_Animator.SetBool("touchBool", true);
                        Invoke("SetTouchTrue",0.1f);
                        talkBubble.SetActive(false);
                        talkBubble.SetActive(true);
                        talkBubble.GetComponent<ShowTalkBubble>().RandowShowTxt();
                        touchingBool = false;

                    }
                }
            }
            
        }

       
    }
    void SetTouchTrue() {
        m_Animator.SetBool("touchBool", false);
        touchingBool = true;
    }
}
