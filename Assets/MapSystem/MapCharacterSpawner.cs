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

        [Tooltip("白天事件播完之後要不要從第一個重來。\n★ 只影響白天 ★ 夜晚的五場儀式一律不循環，做完那個角色就不會出現在夜晚的地圖上。")]
        public bool loop = true;

        // ⭐ 限定出場的劇本。留空＝任何地圖日都出場（主要角色維持留空即可）。
        //    例：西碧兒填 chapter4green，她就只會在綠線的地圖日出現。
        //    判斷依據是 MapReturnPoint，也就是 @SaveReturnPoint 存下來的回程腳本名，
        //    等同於「現在是哪一段劇情把地圖叫出來的」。
        [Tooltip("限定出場的劇本名（例：chapter4green）。留空＝任何時候都出場。\n" +
                 "由 @overrideEvent 指定的特殊事件不受這個限制。")]
        public string[] onlyInScripts;
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
    // ⭐ 這個角色現在這一段劇情該不該出現在地圖上
    // ==========================================================
    private bool IsAvailableHere (CharacterEventList c)
    {
        if (c.onlyInScripts == null || c.onlyInScripts.Length == 0) return true;

        var currentScript = MapReturnPoint.ScriptName;
        if (string.IsNullOrEmpty(currentScript)) return false;

        foreach (var scriptName in c.onlyInScripts)
            if (!string.IsNullOrEmpty(scriptName) && scriptName == currentScript)
                return true;

        return false;
    }

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

        // =======================================================
        // ⭐ 劇情指定的日子：只生成掛著特殊事件的角色
        //    特殊事件是主線用 @overrideEvent 排好的（梅爾的線索日、涅莉的
        //    guidance），劇情接下去會假設它演過了。但 override 在「生成」當下
        //    就會被清掉，所以玩家只要點了別人，這一天就被消耗掉、特殊事件
        //    永遠不會播，主線就缺一塊。這種日子乾脆只讓該角色出現。
        // =======================================================
        var soloNames = new List<string>();
        foreach (var c in characterEventTable)
            if (c != null && !string.IsNullOrEmpty(c.characterName)
                && MapSpecialOverride.TryGet(c.characterName, out _))
                soloNames.Add(c.characterName);

        if (soloNames.Count > 0)
            logMsg += $"⭐ 今天是劇情指定日，只生成：{string.Join("、", soloNames)}\n";

        // ===============================
        // 逐角色生成 icon
        // ===============================
        foreach (var c in characterEventTable)
        {
            if (soloNames.Count > 0 && !soloNames.Contains(c.characterName))
            {
                logMsg += $"⛔ {c.characterName} 今天讓位給劇情事件\n";
                continue;
            }

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


            // =======================================================
            // ⭐ 限定出場章節：不在指定劇本的地圖日就不生成
            //    （放在特殊事件之後，讓 @overrideEvent 的明確指定永遠優先）
            // =======================================================
            if (!IsAvailableHere(c))
            {
                logMsg += $"⛔ {c.characterName} 不在 '{MapReturnPoint.ScriptName}' 出場\n";
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
                    // ⭐ 夜晚一律不循環（loop 只管白天）
                    // 夜晚是五場儀式，是一條有頭有尾的線，不是可以重複的日常。
                    // 繞回第一場的話，玩家做完全部之後又會被請去做一次「第一次引導」，
                    // 台詞和進度都對不起來。做完就讓她從夜晚的地圖上消失。
                    if (eventIdx >= c.nightEvents.Count)
                    {
                        logMsg += $"⭐ {c.characterName} 五場儀式都做完了，今晚不出現\n";
                        continue;
                    }

                    evt = c.nightEvents[eventIdx];

                    // ⭐ 好感門檻：不夠就改播閒聊，儀式進度原地不動（見 RitualGate）
                    if (!RitualGate.CanPerform(c.characterName, eventIdx))
                    {
                        var chat = RitualGate.ChatScript(c.characterName);
                        if (string.IsNullOrEmpty(chat))
                        {
                            logMsg += $"⚠️ {c.characterName} 好感不足但沒設定閒聊劇本，照舊播儀式\n";
                        }
                        else
                        {
                            // 複製原本那顆 icon 的外觀（位置、動畫），只換掉要播的劇本。
                            // 先找有沒有上次建過的，不然每次刷新地圖都會往 specialEvents 塞一筆。
                            var chatName = c.characterName + "_chat";
                            var chatEvt = c.specialEvents.Find(e => e.eventName == chatName);
                            if (chatEvt == null)
                            {
                                chatEvt = new CharacterEvent { eventName = chatName };
                                // 放進 specialEvents，點下去就不會 IncrementNightProgress。
                                c.specialEvents.Add(chatEvt);
                            }

                            chatEvt.naninovelScript = chat;
                            chatEvt.animatorController = evt.animatorController;
                            chatEvt.offset = evt.offset;

                            evt = chatEvt;
                            logMsg += $"💬 {c.characterName} 好感不足 → 改播閒聊 {chat}\n";
                        }
                    }
                }
            }


            if (evt != null)
            {
                count++;
                StartCoroutine(CreateCharacterIcon(c, evt));
            }
        }

        if (count == 0)
        {
            logMsg += "⭐ 所有角色事件都已播畢\n";
            Debug.Log(logMsg);
            ReturnToStory();
            return;
        }

        Debug.Log(logMsg);
    }

    /// <summary>
    /// 一個 icon 都沒生出來時的保險：直接回劇本。
    ///
    /// ★ 為什麼需要 ★
    /// 離開地圖的唯一方法是點某個角色（RestButton 那支程式沒掛在任何場景上），
    /// 所以空地圖等於卡死。兩種情況碰得到：五個人的夜晚儀式都做完了，
    /// 或者這一天的角色都被 onlyInScripts 擋掉。
    /// </summary>
    void ReturnToStory ()
    {
        if (!MapReturnPoint.HasValid())
        {
            Debug.LogWarning("[MapCharacterSpawner] 地圖上沒有任何角色，但也沒有返回點可以回。" +
                             "玩家會卡在空地圖上，請檢查進地圖前有沒有 @SaveReturnPoint。");
            return;
        }

        var loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("[MapCharacterSpawner] 找不到 SceneLoader，無法自動回到劇本。");
            return;
        }

        Debug.Log($"[MapCharacterSpawner] 今天沒人可以找，直接回劇本：{MapReturnPoint.ScriptName}#{MapReturnPoint.Label}");
        loader.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
        MapReturnPoint.Clear();
    }

    // ==========================================================
    // ⑤ 建立角色 icon（保持你原本邏輯）
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
        {
            StoryProgressManager.Instance.IncrementDayProgress(c.characterName);
        }
        else if (RitualGate.HasGate(c.characterName))
        {
            // 夜晚儀式改成「打贏才算過」——推進在 CombatSystem 勝利結算時做
            // （RitualGate.AdvanceOnRitualWin）。這裡先不動，輸了才能重打。
            Debug.Log($"🌙 {c.characterName} 的儀式進度等戰鬥結果，先不推進");
        }
        else
        {
            StoryProgressManager.Instance.IncrementNightProgress(c.characterName);
        }
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
