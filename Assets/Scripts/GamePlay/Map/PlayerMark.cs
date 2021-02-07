using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMark : MonoBehaviour {
    
    float theta = 0f;

    [SerializeField] float speed = 1f;

    [SerializeField] float offset = 0.5f;

    float orgY;

    // Use this for initialization
    void Start () {

        orgY = transform.localPosition.y;
    }

    void Update()
    {
        theta += Time.deltaTime * speed;
        transform.localPosition = new Vector3(transform.localPosition.x, orgY + offset * Mathf.Sin(theta), transform.localPosition.z);
    }
}
