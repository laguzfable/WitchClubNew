using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowTalkBubble : MonoBehaviour {
    [SerializeField]
    string[] talkTxt;
    int randomInt;
    [SerializeField]
    TextMesh tM;

	// Update is called once per frame
	public void RandowShowTxt () {
        randomInt = Random.Range(0, talkTxt.Length);
        tM.text = talkTxt[randomInt];
    }
}
