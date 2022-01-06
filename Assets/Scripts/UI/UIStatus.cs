using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIStatus : MonoBehaviour 
{

    [SerializeField] BaseCombatUnit target;

    [SerializeField] GameObject[] statusImgArr;

    [SerializeField] TextMeshProUGUI[] txtArr;

    public void Reset()
    {
        foreach (var status in statusImgArr)
        {
            status.SetActive(false);
        }
    }

    public void SetATK(int value) => DisplayValue(0, value.ToString());

    public void SetDEF(int value) => DisplayValue(1, value.ToString());

    public void SetHEAL(int value) => DisplayValue(2, value.ToString());

    public void SetSkill() => DisplayValue(3, string.Empty);

    void DisplayValue(int status, string valueStr)
    {
        statusImgArr[status].SetActive(true);
        txtArr[status].text = valueStr;
    }
}