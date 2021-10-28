using Naninovel;
using UnityEngine;

public class StopAudioListener : MonoBehaviour
{
    private void Awake()
    {
        if(Engine.Initialized)
        {
            GetComponent<AudioListener>().enabled = false;
            Debug.LogWarning("Nani is initialized, stop main camera's audio listener.");
        }
    }
}