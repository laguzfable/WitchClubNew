using UnityEngine;
using UnityEngine.UI;

public class UIStatus : MonoBehaviour 
{

    // might = 0
    // ref = 1
    // shield = 2
    // poison = 3
    // stun = 4

    [SerializeField]
    BaseCombatUnit target;

    [SerializeField]
    GameObject[] statusImgArr;

    [SerializeField]
    Text[] textArr;

    /*
    void Start()
    {
        target.shield.OnValueChanged += OnShieldChanged;
        target.might.OnValueChanged += OnMightChanged;
        target.poison.OnValueChanged += OnPoisonChanged;
        target.reflection.OnValueChanged += OnReflectionChanged;
        target.stun.OnValueChanged += OnStunValueChanged;

        foreach(var go in statusImgArr)
        {
            go.SetActive(false);
        }

        //Debug.Log("UI Status Start target : " + target.name);
    }

    void OnShieldChanged(float value)
    {
        textArr[2].text = target.shield.Value.ToString();
        statusImgArr[2].SetActive(target.shield.Value > 0f);
    }

    void OnMightChanged(float value)
    {
        textArr[0].text = target.might.Value.ToString();
        statusImgArr[0].SetActive(target.might.Value > 0f);
    }

    void OnPoisonChanged(float value)
    {
        textArr[3].text = target.poison.Value.ToString();
        statusImgArr[3].SetActive(target.poison.Value > 0f);
    }

    void OnReflectionChanged(float value)
    {
        textArr[1].text = target.reflection.Value.ToString();
        statusImgArr[1].SetActive(target.reflection.Value > 0f);
    }

    void OnStunValueChanged(float value)
    {
        textArr[4].text = target.stun.Value.ToString();
        statusImgArr[4].SetActive(target.stun.Value > 0f);
    }
    */
}