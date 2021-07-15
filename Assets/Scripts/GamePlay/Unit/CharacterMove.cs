using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    float theta = 0f;

    [SerializeField] float speed = 1f;

    [SerializeField] float offset = 0.5f;

    float orgY;

    public void SetOrgY()
    {
        orgY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        theta += Time.deltaTime * speed;
        transform.position = new Vector3(transform.position.x, orgY + offset * Mathf.Sin(theta), transform.position.z);
    }
}
