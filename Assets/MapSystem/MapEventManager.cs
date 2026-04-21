using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapEventManager : MonoBehaviour
{
    public Button E_Day_Button, M_Day_Button, V_Day_Button, N_Day_Button;
    public Button E_Night_Button, M_Night_Button, V_Night_Button, N_Night_Button;

    [System.Serializable]
    public class EventScriptEntry
    {
        public string character;        // "E", "M", "V", "N"
        public bool isDay;
        public int progressIndex;       // 第幾次觸發（從 1 開始）
        public string naninovelScript;  // 對應的 .nani 腳本名稱
    }

    [Header("事件劇本對照表（在 Inspector 填入）")]
    public List<EventScriptEntry> eventScripts = new List<EventScriptEntry>();

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

        var entry = eventScripts.Find(e =>
            e.character == ch && e.isDay == isDay && e.progressIndex == prog);

        if (entry != null && !string.IsNullOrEmpty(entry.naninovelScript))
        {
            var loader = SceneLoader.Instance;
            if (loader != null)
            {
                loader.GotoScript(entry.naninovelScript);
            }
            else
            {
                Debug.LogError("[MapEventManager] 找不到 SceneLoader！請確認場景中有 SceneLoader 物件。");
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[MapEventManager] 找不到劇本設定：{ch} {(isDay ? "白天" : "晚上")} prog={prog}，請在 Inspector 的「事件劇本對照表」填入對應腳本。");
        }

        // 事件結束後進度+1
        PlayerPrefs.SetInt(key, prog + 1);
        PlayerPrefs.Save();

        RefreshButtons();
    }
}
