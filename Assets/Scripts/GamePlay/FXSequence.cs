using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EFXSequenceType { Enemy, Player, UI, None }

[System.Serializable]
public class FXSequence : MonoBehaviour
{
    public GameObject fx;
    public float duration;
    public float lifeTime;
    public EFXSequenceType type;
    public Vector3 position;
    public bool playOnce = false;

    public FXSequence nextFX;

    static public IEnumerator PlayFX(FXSequence fxSeq)
    {
        var displayFX = fxSeq;
        while (displayFX != null)
        {
            var go = Instantiate(fxSeq.fx);
            go.transform.position = fxSeq.position;
            yield return new WaitForSeconds(fxSeq.duration);
            Destroy(go, fxSeq.lifeTime);
            displayFX = displayFX.nextFX;
        }
    }

}
