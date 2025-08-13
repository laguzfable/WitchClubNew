using UnityEngine;
using UnityEngine.UI;

public class MapEventManager : MonoBehaviour
{
    public Button E_Day_Button, M_Day_Button, V_Day_Button, N_Day_Button;
    public Button E_Night_Button, M_Night_Button, V_Night_Button, N_Night_Button;

    void Start()
    {
        // 綁定按鈕（白天）
        E_Day_Button.onClick.AddListener(() => OnEventClick("E", true));
        M_Day_Button.onClick.AddListener(() => OnEventClick("M", true));
        V_Day_Button.onClick.AddListener(() => OnEventClick("V", true));
        N_Day_Button.onClick.AddListener(() => OnEventClick("N", true));
        // 綁定按鈕（晚上）
        E_Night_Button.onClick.AddListener(() => OnEventClick("E", false));
        M_Night_Button.onClick.AddListener(() => OnEventClick("M", false));
        V_Night_Button.onClick.AddListener(() => OnEventClick("V", false));
        N_Night_Button.onClick.AddListener(() => OnEventClick("N", false));

        RefreshButtons();
    }

    void RefreshButtons()
    {
        // 只顯示該時段按鈕
        bool isDay = PlayerPrefs.GetInt("IsDay", 1) == 1;
        E_Day_Button.gameObject.SetActive(isDay);
        M_Day_Button.gameObject.SetActive(isDay);
        V_Day_Button.gameObject.SetActive(isDay);
        N_Day_Button.gameObject.SetActive(isDay);
        E_Night_Button.gameObject.SetActive(!isDay);
        M_Night_Button.gameObject.SetActive(!isDay);
        V_Night_Button.gameObject.SetActive(!isDay);
        N_Night_Button.gameObject.SetActive(!isDay);
    }

    void OnEventClick(string ch, bool isDay)
    {
        string key = $"{ch}_Event_{(isDay ? "Day" : "Night")}";
        int prog = PlayerPrefs.GetInt(key, 1);

        Debug.Log($"{ch} {(isDay ? "白天" : "晚上")}事件 {prog}");

        // TODO：呼叫 Naninovel 劇本事件

        // 事件結束後進度+1
        PlayerPrefs.SetInt(key, prog + 1);
        PlayerPrefs.Save();

        RefreshButtons();
    }
}
