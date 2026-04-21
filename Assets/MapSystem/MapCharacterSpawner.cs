using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MapCharacterSpawner : MonoBehaviour
{
    public GameObject characterIconPrefab;
    public Transform iconParent;

    [System.Serializable]
    public class CharacterEvent
    {
        public string eventName;
        public string naninovelScript;
        public RuntimeAnimatorController animatorController;
        public Vector2 offset;
    }

    [System.Serializable]
    public class CharacterEventList
    {
        public string characterName;
        public Vector2 position;
        public List<CharacterEvent> dayEvents = new List<CharacterEvent>();
        public List<CharacterEvent> nightEvents = new List<CharacterEvent>();
    }

    public List<CharacterEventList> characterEventTable = new List<CharacterEventList>();

    public enum TimeOfDay { Day, Night }
    public TimeOfDay currentTimeOfDay = TimeOfDay.Day;
    public bool autoDetectTimeOfDay = true;

    private string logMsg = "";

void Start()
{
    // 多重保險：MapIsDay 優先，其次 Game_IsDay，最後預設白天
    int rawVal = -99;
    bool isDay = true;

    if (PlayerPrefs.HasKey("MapIsDay"))
    {
        rawVal = PlayerPrefs.GetInt("MapIsDay", 1);
        isDay = rawVal == 1;
        logMsg += $"⏰ 時段偵測來源：MapIsDay = {rawVal}\n";
    }
    else if (PlayerPrefs.HasKey("Game_IsDay"))
    {
        rawVal = PlayerPrefs.GetInt("Game_IsDay", 1);
        isDay = rawVal == 1;
        logMsg += $"⏰ 時段偵測來源：Game_IsDay = {rawVal}\n";
    }
    else
    {
        logMsg += $"⏰ 沒有找到任何時段資料，預設為 Day\n";
    }

    currentTimeOfDay = isDay ? TimeOfDay.Day : TimeOfDay.Night;

    Debug.Log($"🟢 Start() 確認時段：{currentTimeOfDay}（MapIsDay={rawVal}）");


        if (iconParent == null)
        {
            logMsg += "⚠ iconParent 未指定，角色 icon 將無法正確掛在 MapRoot 下。\n";
        }
        else
        {
            logMsg += $"📌 iconParent：{iconParent.name}\n";
        }

        int count = 0;

        foreach (var c in characterEventTable)
        {
            int eventIdx = 0;
            CharacterEvent evt = null;

            if (currentTimeOfDay == TimeOfDay.Day)
            {
                eventIdx = StoryProgressManager.Instance.GetDayProgress(c.characterName);
                if (c.dayEvents != null && eventIdx < c.dayEvents.Count)
                    evt = c.dayEvents[eventIdx];
            }
            else
            {
                eventIdx = StoryProgressManager.Instance.GetNightProgress(c.characterName);
                if (c.nightEvents != null && eventIdx < c.nightEvents.Count)
                    evt = c.nightEvents[eventIdx];
            }

            if (evt != null)
            {
                count++;
                StartCoroutine(CreateCharacterIcon(c, evt));
            }
        }

        if (count == 0)
            logMsg += "⚠ 沒有任何角色生成！可能未設定 characterEventTable。\n";

        Debug.Log(logMsg);
    }

    IEnumerator CreateCharacterIcon(CharacterEventList c, CharacterEvent evt)
    {
        var iconGO = Instantiate(characterIconPrefab, iconParent != null ? iconParent : transform);
        iconGO.name = $"Icon_{c.characterName}_{evt.eventName}";
        iconGO.transform.SetAsLastSibling();

        logMsg += $"\n🧩 生成：{iconGO.name} @ {c.position + evt.offset}\n";

        var rect = iconGO.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = c.position + evt.offset;

        var txt = iconGO.GetComponentInChildren<Text>(true);
        if (txt != null)
        {
            txt.text = $"{(currentTimeOfDay == TimeOfDay.Day ? "白天" : "晚上")}：{evt.eventName}";
        }

        var animator = iconGO.GetComponentInChildren<Animator>(true);
        if (animator != null && evt.animatorController != null)
        {
            animator.runtimeAnimatorController = evt.animatorController;
            var clips = evt.animatorController.animationClips;
            if (clips != null && clips.Length > 0)
                animator.Play(clips[0].name, -1, 0f);
        }

        Debug.Log($"🧩 [生成Icon] {c.characterName} | 時段：{currentTimeOfDay} | 劇本：{evt.naninovelScript}");

        yield return null;

        var btnTransform = iconGO.transform.Find("GirlButton") ?? iconGO.transform.Find("CharacterIcon/GirlButton");
        if (btnTransform != null)
        {
            var button = btnTransform.GetComponent<Button>();
            if (button != null)
            {
                var c911 = button.gameObject.AddComponent<call911>();
                c911.劇本名 = evt.naninovelScript;

                button.onClick.AddListener(() =>
                {
                    Debug.Log($"👉 點擊事件：{c.characterName} | 時段：{currentTimeOfDay} | 執行劇本：{evt.naninovelScript}");

                    if (currentTimeOfDay == TimeOfDay.Day)
                        StoryProgressManager.Instance.IncrementDayProgress(c.characterName);
                    else
                        StoryProgressManager.Instance.IncrementNightProgress(c.characterName);
                });

                logMsg += $"✅ 掛上 call911 成功：{evt.naninovelScript} @ {button.name}\n";
            }
            else
            {
                logMsg += $"❌ GirlButton 上找不到 Button 組件！\n";
                MarkRed(btnTransform.gameObject);
            }
        }
        else
        {
            logMsg += $"❌ 找不到 GirlButton！（Prefab 結構錯誤？）\n";
            MarkRed(iconGO);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("🧾【角色進度顯示】");
            foreach (var c in characterEventTable)
            {
                int day = StoryProgressManager.Instance.GetDayProgress(c.characterName);
                int night = StoryProgressManager.Instance.GetNightProgress(c.characterName);
                Debug.Log($"📚 {c.characterName} | 白天：{day} | 晚上：{night}");
            }
        }
    }

    void MarkRed(GameObject go)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.5f);
    }
}
