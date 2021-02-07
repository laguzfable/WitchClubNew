using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
public class MainSceneShowVideo : MonoBehaviour
{
    [SerializeField]
    VideoPlayer videoPlayer;
    AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        videoPlayer.gameObject.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayVideo());
    }
    Coroutine coroutine;
    void Update() {
        if (Input.anyKeyDown && videoPlayer.gameObject.activeSelf) {
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            videoPlayer.Stop();
            videoPlayer.gameObject.SetActive(false);
           
            audioSource.Play();
            coroutine = StartCoroutine(PlayVideo());
        }
    }
        
        
    IEnumerator PlayVideo() {
        yield return new WaitForSeconds(30);
        videoPlayer.gameObject.SetActive(true);
        videoPlayer.Play();
        audioSource.Stop();
    }
}
