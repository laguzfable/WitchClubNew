using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISwitchButon : MonoBehaviour {

    [SerializeField]
    GameObject offPosition;

    [SerializeField]
    GameObject onPosition;

    [SerializeField]
    Image dot;

    [SerializeField]
    Image onImage;


    [SerializeField]
    Button btn;

    [SerializeField]
    Color onColor, offColor;

    [SerializeField]
    GameObject onTxt, offTxt;


    [SerializeField]
    bool _isOn;

    float tweenDuration = 0.35f;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        SetState(_isOn);

        btn.onClick.AddListener(OnSwitch);
    }

    public bool isOn
    {
        set
        {
            SetState(value);
        }
        get
        {
            return _isOn;
        }
    }


    void SetState(bool newState)
    {
        _isOn = newState;

        dot.gameObject.transform.position = _isOn ? onPosition.transform.position : offPosition.transform.position;
        onImage.CrossFadeAlpha(_isOn ? 1f : 0f, 0f, true);
        dot.CrossFadeColor(_isOn ? onColor : offColor, 0f, true, false, true);
        onTxt.SetActive(_isOn);
        offTxt.SetActive(!_isOn);
    }

    void OnSwitch()
    {
        _isOn = !_isOn;
#if iTween
        iTween.MoveTo(dot.gameObject, _isOn? onPosition.transform.position : offPosition.transform.position, tweenDuration);        
#endif
        onImage.CrossFadeAlpha(_isOn ? 1f : 0f, tweenDuration, false);
        dot.CrossFadeColor(_isOn ? onColor : offColor, tweenDuration, true, false, true);
        onTxt.SetActive(_isOn);
        offTxt.SetActive(!_isOn);
    }
}
