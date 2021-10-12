using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UniRx.Async;

public enum ECombatTextType { Damage, Heal, Buff, Debuff, Other , System };
public class UICombatText : MonoBehaviour {

    Text txt;


	// Use this for initialization
	void Awake () {

        txt = GetComponent<Text>();

    }

    public async UniTaskVoid Display(string content, ECombatTextType type, Vector3 pos)
    {
        //type to color
        txt.text = content;
        switch(type)
        {
            case ECombatTextType.Damage:
                txt.color = Color.red;
                break;
            case ECombatTextType.Heal:
                txt.color = Color.green;
                break;
            case ECombatTextType.Buff:
                txt.color = Color.yellow;
                break;
            case ECombatTextType.Debuff:
                txt.color = Color.cyan;
                break;
            case ECombatTextType.Other:
                txt.color = Color.gray;
                break;
            case ECombatTextType.System:
                txt.color = Color.white;
                break;
        }

        transform.position = pos;
        txt.CrossFadeAlpha(1f, 0.3f, false);
        gameObject.SetActive(true);

        //transform.Translate(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 0.25f), 0f);
        transform.GetComponent<RectTransform>().anchoredPosition += new Vector2(Random.Range(-50f, 50f), Random.Range(-25f, 25f));

        //transform.DOMoveY(pos.y + 20f, 1f).onComplete += OnTweenCompleteEvent;
        //txt.CrossFadeAlpha(0f, 1f, false);
        transform.DOScale(1.8f, 0.3f);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken:this.GetCancellationTokenOnDestroy());
        txt.CrossFadeAlpha(0f, 0.3f, false);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken:this.GetCancellationTokenOnDestroy());
        gameObject.SetActive(false);
    }
}
