using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MapCharacterSpawner : MonoBehaviour
{
    public GameObject characterIconPrefab;
    public Transform iconParent;

    // ============================================
    // ① 單一事件資料
    // ============================================
    [System.Serializable]
    public class CharacterEvent
    {
        public string eventName;
        public string naninovelScript;
        public RuntimeAnimatorController animatorController;
        public Vector2 offset;
    }

    // ============================================
    // ② 角色事件清單（白天/夜晚/特殊事件）
    // ============================================
    [System.Serializable]
    public class CharacterEventList
    {
        public string characterName;
        public Vector2 position;

        public List<CharacterEvent> dayEvents = new List<CharacterEvent>();
        public List<CharacterEvent> nightEvents = new List<CharacterEvent>();

        // ⭐ 新增：特殊事件（由 Nani 呼叫）
        public List<CharacterEvent> specialEvents = new List<CharacterEvent>();

        public bool loop = true;
    }

    // ============================================
    // ③ Inspector 用
    // ============================================
    public List<CharacterEventList> characterEventTable = new List<CharacterEventList>();

    public enum TimeOfDay { Day, Night }
    public TimeOfDay currentTimeOfDay = TimeOfDay.Day;
    public bool autoDetectTimeOfDay = true;

    private string logMsg = "▶ Spawner 啟動中...\n";

    // ==========================================================
    // ⭐ 取得特殊事件（由 overrideEvent 指令使用）
    // ==========================================================
    public CharacterEvent GetSpecialEvent(string character, string evtName)
    {
        var list = characterEventTable.Find(c => c.characterName == character);
        if (list == null) return null;
        return list.specialEvents.Find(e => e.eventName == evtName);
    }

    // ==========================================================
    // ④ Start()
    // ==========================================================
    void Start()
    {
        int rawVal = -99;
        bool isDay = true;

        // 判定時段（維持你的原邏輯）
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
            logMsg += "⚠ iconParent 未指定，可能掛錯位置。\n";
        else
            logMsg += $"📌 iconParent：{iconParent.name}\n";

        int count = 0;


        // ===============================
        // 逐角色生成 icon
        // ===============================
        foreach (var c in characterEventTable)
        {
// =======================================================
// ⭐ 當角色有特殊事件 → 只生成特殊事件，不生成一般事件
// =======================================================
if (MapSpecialOverride.TryGet(c.characterName, out var special))
{
    Debug.Log($"💥 特殊事件啟動：{c.characterName} → {special.eventName}");

    // 從 inspector 清單抓真正的設定
    var spEvt = c.specialEvents.Find(e => e.eventName == special.eventName);

    if (spEvt == null)
    {
        Debug.LogWarning($"❌ 特殊事件 '{special.eventName}' 在 {c.characterName} 的 specialEvents 找不到！");
    }
    else
    {
        // 用 inspector 的資料生成 icon（包括動畫、offset）
        StartCoroutine(CreateCharacterIcon(c, spEvt));
    }

    // ✨ 保證一次性：呼叫後馬上清掉
    MapSpecialOverride.Clear(c.characterName);

    // ✨ 跳過該角色的所有 day/night 事件（不生成）
    continue;
}


            // ===============================
            // ⭐ 原本白天/夜晚事件流程（不變）
            // ===============================
            int eventIdx = 0;
            CharacterEvent evt = null;

            if (currentTimeOfDay == TimeOfDay.Day)
            {
                eventIdx = StoryProgressManager.Instance.GetDayProgress(c.characterName);
                if (c.dayEvents != null && c.dayEvents.Count > 0)
                {
                    if (eventIdx >= c.dayEvents.Count)
                    {
                        if (c.loop)
                        {
                            eventIdx = 0;
                            PlayerPrefs.SetInt($"{c.characterName}_DayProgress", 0);
                            PlayerPrefs.Save();
                            logMsg += $"🔁 {c.characterName} 白天事件重新開始\n";
                        }
                        else
                        {
                            logMsg += $"⭐ {c.characterName} 白天事件已播完\n";
                            continue;
                        }
                    }

                    evt = c.dayEvents[eventIdx];
                }
            }
            else
            {
                eventIdx = StoryProgressManager.Instance.GetNightProgress(c.characterName);
                if (c.nightEvents != null && c.nightEvents.Count > 0)
                {
                    if (eventIdx >= c.nightEvents.Count)
                    {
                        if (c.loop)
                        {
                            eventIdx = 0;
                            PlayerPrefs.SetInt($"{c.characterName}_NightProgress", 0);
                            PlayerPrefs.Save();
                            logMsg += $"🔁 {c.characterName} 夜晚事件重新開始\n";
                        }
                        else
                        {
                            logMsg += $"⭐ {c.characterName} 夜晚事件已播完\n";
                            continue;
                        }
                    }

                    evt = c.nightEvents[eventIdx];
                }
            }


            if (evt != null)
            {
                count++;
                StartCoroutine(CreateCharacterIcon(c, evt));
            }
        }

        if (count == 0)
            logMsg += "⭐ 所有角色事件都已播畢\n";

        Debug.Log(logMsg);
    }

    // ==========================================================
    // ⑤ 建立角色 icon（保持你原本邏輯）
    // ==========================================================
    IEnumerator CreateCharacterIcon(CharacterEventList c, CharacterEvent evt)
    {
        var iconGO = Instantiate(characterIconPrefab, iconParent != null ? iconParent : transform);
        iconGO.name = $"Icon_{c.characterName}_{evt.eventName}";
        iconGO.transform.SetAsLastSibling();

        logMsg += $"\n🧩 生成：{iconGO.name} @ {c.position + evt.offset}\n";

        var rect = iconGO.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = c.position + evt.offset;

        var txt = iconGO.GetComponentInChildren<Text>(true);
        if (txt != null)
            txt.text = $"{(currentTimeOfDay == TimeOfDay.Day ? "白天" : "晚上")}：{evt.eventName}";

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

        var btnTransform = iconGO.transform.Find("GirlButton") ??
                           iconGO.transform.Find("CharacterIcon/GirlButton");

        if (btnTransform != null)
        {
            var button = btnTransform.GetComponent<Button>();
            if (button != null)
            {
                var c911 = button.gameObject.AddComponent<call911>();
                c911.劇本名 = evt.naninovelScript;

button.onClick.AddListener(() =>
{
    Debug.Log($"👉 點擊：{c.characterName} | 劇本：{evt.naninovelScript}");

    bool isSpecial = c.specialEvents.Contains(evt);

    if (!isSpecial)
    {
        // 只有一般事件才推進進度
        if (currentTimeOfDay == TimeOfDay.Day)
            StoryProgressManager.Instance.IncrementDayProgress(c.characterName);
        else
            StoryProgressManager.Instance.IncrementNightProgress(c.characterName);
    }
    else
    {
        Debug.Log($"⭐ 特殊事件 → 不推進 {c.characterName} 的事件序號");
    }
});


                logMsg += $"✅ 掛上 call911 成功：{evt.naninovelScript}\n";
            }
            else
            {
                logMsg += "❌ GirlButton 找不到 Button\n";
                MarkRed(btnTransform.gameObject);
            }
        }
        else
        {
            logMsg += "❌ 找不到 GirlButton\n";
            MarkRed(iconGO);
        }
    }

    // ==========================================================
    // ⑥ F2 顯示進度
    // ==========================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("🧾【角色進度】");
            foreach (var c in characterEventTable)
            {
                int day = StoryProgressManager.Instance.GetDayProgress(c.characterName);
                int night = StoryProgressManager.Instance.GetNightProgress(c.characterName);
                Debug.Log($"📚 {c.characterName} | 白天：{day} | 晚上：{night}");
            }
        }
    }

    // ==========================================================
    // ⑧ 出錯時把物件染紅
    // ==========================================================
    void MarkRed(GameObject go)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.5f);
    }
}
