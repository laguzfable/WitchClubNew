using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySkullAni : MonoBehaviour
{
    Animation ani;
    AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        ani = GetComponent<Animation>();
    }

    // Update is called once per frame
    public void PlayAni()
    {
        audioSource.Play();
        ani.Play();
    }
}
