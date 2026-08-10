using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.UI;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 高塔模式：從主選單開始，連續 50 層隨機戰鬥，血量隨樓層遞增並隨機浮動、
    /// 敵人圖片隨機、所有符文從第一層就解鎖（跑完後復原玩家原本的解鎖進度）。
    /// </summary>
    public static class TowerModeManager
    {
        public const int MaxFloor = 50;

        const string CombatSceneName = "CombatScene";
        const string TitleSceneName = "Title";
        const string ChangeRuneSceneName = "ChangeRuneScene";
        const string ChangeCardSceneName = "ChangeCardTypeScene";
        const string HubSceneName = "TowerHubScene";
        const string ChangeAmuletSceneName = "ChangeAmuletScene";
        const string BestFloorKey = "TowerMode.BestFloor";
        const string IsActiveKey = "TowerMode.IsActive";
        const string CurrentFloorKey = "TowerMode.CurrentFloor";
        const string SnapshotKeyPrefix = "TowerMode.Snapshot.";
        const string EquippedAmuletsKey = "TowerMode.EquippedAmulets";
        const string AdapterAmuletId = "Adapter";
        const string DimensionAmuletId = "Dimension";
        const string FullEnergyAmuletId = "FullEnergy";

        static readonly string[] Elements = { "Blue", "Red", "Yellow", "Green" };
        static readonly string[] BattleBackgrounds =
            { "bg00", "bg01", "bg02", "bg03", "bg04", "bg05", "bg06", "bg07", "bg08", "bg11", "bg12" };

        static List<string> monsterPool;
        static List<Sprite> spritePool;

        static bool isActiveField;
        public static bool IsActive
        {
            get => isActiveField;
            private set
            {
                isActiveField = value;
                PlayerPrefs.SetInt(IsActiveKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        static int currentFloorField;
        public static int CurrentFloor
        {
            get => currentFloorField;
            private set
            {
                currentFloorField = value;
                PlayerPrefs.SetInt(CurrentFloorKey, value);
                PlayerPrefs.Save();
            }
        }

        public static int BestFloor => PlayerPrefs.GetInt(BestFloorKey, 0);

        /// <summary>關掉遊戲重開後，是否還有一場沒結束的高塔挑戰可以繼續。</summary>
        public static bool HasSavedRun => PlayerPrefs.GetInt(IsActiveKey, 0) == 1;

        /// <summary>目前裝備中的護身符清單，只有高塔模式才會用到。一般狀況下最多裝 1 個，
        /// 但裝了「護符轉接頭」的話，額外多 2 個欄位（總共 3 個）。</summary>
        public static IReadOnlyList<string> EquippedAmulets => GetEquippedAmuletList();

        static List<string> GetEquippedAmuletList()
        {
            var raw = PlayerPrefs.GetString(EquippedAmuletsKey, "");
            return string.IsNullOrEmpty(raw)
                ? new List<string>()
                : raw.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        static void SaveEquippedAmuletList(List<string> list)
        {
            PlayerPrefs.SetString(EquippedAmuletsKey, string.Join(",", list));
            PlayerPrefs.Save();
        }

        /// <summary>裝備中的護身符（含轉接頭本身）能不能再多裝一個。</summary>
        static int CapacityFor(List<string> list) => list.Contains(AdapterAmuletId) ? 3 : 1;

        public static bool IsAmuletEquipped(string amuletId) => GetEquippedAmuletList().Contains(amuletId);

        /// <summary>由護身符按鈕呼叫：已裝備就卸下，沒裝備就嘗試裝上（欄位滿了會裝不進去，直接忽略）。</summary>
        public static void ToggleAmulet(string amuletId)
        {
            var list = GetEquippedAmuletList();

            if (list.Contains(amuletId))
            {
                list.Remove(amuletId);
                // 拿掉的如果是轉接頭，容量會從 3 縮回 1，多出來裝不下的要修剪掉
                int capacity = CapacityFor(list);
                while (list.Count > capacity)
                    list.RemoveAt(list.Count - 1);
                SaveEquippedAmuletList(list);
                return;
            }

            var prospective = new List<string>(list) { amuletId };
            if (list.Count >= CapacityFor(prospective)) return; // 欄位已經滿了，裝不下

            list.Add(amuletId);
            SaveEquippedAmuletList(list);
        }

        /// <summary>該色卡片是否因為護身符而數值 x2（只有高塔模式挑戰中才生效）。</summary>
        public static bool IsAmuletBoosted(ECardElement element)
        {
            if (!IsActive) return false;
            return IsAmuletEquipped(element.ToString());
        }

        /// <summary>裝備「次元護符」時，手牌卡片最高可以升到幾級（沒裝的話維持原本 5 級上限）。</summary>
        public static int CardLevelCap => (IsActive && IsAmuletEquipped(DimensionAmuletId)) ? 7 : 5;

        /// <summary>裝備「起始能量全滿護符」時，戰鬥一開始符文能量是不是該直接全滿。</summary>
        public static bool ShouldStartWithFullRuneEnergy => IsActive && IsAmuletEquipped(FullEnergyAmuletId);

        public static void StartRun()
        {
            EnsurePoolsLoaded();
            SnapshotAndUnlockAllRunes();
            SnapshotAndUnlockAllCardVariants();

            IsActive = true;
            CurrentFloor = 1;

            // 一開始先進選單頁，玩家可以自由選要換符文/換卡片，按「進入下一層」才會真的開始
            SceneManager.LoadScene(HubSceneName);
        }

        /// <summary>關掉遊戲/重開 App 後，偵測到有未結束的挑戰時呼叫，接續上次樓層跟解鎖快照。</summary>
        public static void ResumeRun()
        {
            if (!HasSavedRun)
            {
                StartRun();
                return;
            }

            EnsurePoolsLoaded();
            IsActive = true;
            CurrentFloor = PlayerPrefs.GetInt(CurrentFloorKey, 1);

            // 「回標題」途中 Naninovel 引擎狀態可能被重置過（RuneActive 這個自訂變數會被打回預設 false），
            // 續關時沒重設回 true 的話，CombatSystem.Init() 讀到 false 會直接把符文系統整組關掉。
            Engine.GetService<ICustomVariableManager>()?.SetVariableValue("RuneActive", "True");

            SceneManager.LoadScene(HubSceneName);
        }

        /// <summary>由選單頁的「換符文」按鈕呼叫。</summary>
        public static void OpenRuneScreen() => SceneManager.LoadScene(ChangeRuneSceneName);

        /// <summary>由選單頁的「換卡片」按鈕呼叫。</summary>
        public static void OpenCardScreen() => SceneManager.LoadScene(ChangeCardSceneName);

        /// <summary>由選單頁的「護身符」按鈕呼叫。</summary>
        public static void OpenAmuletScreen() => SceneManager.LoadScene(ChangeAmuletSceneName);

        /// <summary>由 ChangeRuneScene / ChangeCardTypeScene 的返回按鈕呼叫，回到選單頁（不是直接進下一層）。</summary>
        public static void BackToHub()
        {
            if (!IsActive) return;
            SceneManager.LoadScene(HubSceneName);
        }

        /// <summary>由選單頁的「進入下一層」按鈕呼叫，真正開始戰鬥。</summary>
        public static void ContinueToNextFloor()
        {
            if (!IsActive) return;
            LoadFloor();
        }

        /// <summary>由 CombatSystem.GameOver 呼叫，取代原本回 Nani 劇本的流程。</summary>
        public static void HandleBattleResult(bool isLose)
        {
            if (!IsActive) return;

            if (isLose)
            {
                if (CurrentFloor > BestFloor)
                {
                    PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                    PlayerPrefs.Save();
                }

                CurrentFloor = 1; // 死掉重新從第一層開始（跟主動「退出」不一樣，退出不會重置）
                BackToHub();
                return;
            }

            if (CurrentFloor > BestFloor)
            {
                PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                PlayerPrefs.Save();
            }

            var clearedFloor = CurrentFloor;

            if (clearedFloor >= MaxFloor)
            {
                FinishRun();
                return;
            }

            CurrentFloor++;
            LoadFloor();
        }

        /// <summary>由戰鬥畫面的「退出」按鈕呼叫，放棄本場戰鬥、記錄目前樓層，回到高塔選單頁（本次挑戰不會結束）。</summary>
        public static void QuitRun()
        {
            if (!IsActive) return;

            if (CurrentFloor > BestFloor)
            {
                PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                PlayerPrefs.Save();
            }

            BackToHub();
        }

        /// <summary>由選單頁的「回標題」按鈕呼叫。跟直接關掉遊戲一樣：本次挑戰不會結束，
        /// 樓層/符文快照維持原樣，下次從標題進高塔模式會直接續關（見 HasSavedRun）。</summary>
        public static async void QuitToTitle()
        {
            if (!IsActive) return;

            if (CurrentFloor > BestFloor)
            {
                PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                PlayerPrefs.Save();
            }

            // 只關掉「記憶體中的作用中狀態」，PlayerPrefs 的續關記錄（IsActiveKey）保留。
            // 不然回標題後去玩主線時，IsActive 還是 true，高塔的護身符加成/換符文返回/
            // 地圖返回全都會被高塔模式劫走。續關時 ResumeRun 會把它設回 true。
            isActiveField = false;

            await GoToTitleScene();
        }

        /// <summary>由高塔選單頁「放棄本次挑戰」的確認彈窗按下「確定」時呼叫（確認彈窗是場景自己
        /// 做的 Overlay UI，不是 Naninovel 的 IConfirmationUI——那個是 Camera-space canvas，
        /// 跟 Hub 場景的 Overlay canvas 疊層會衝突）。結束這次挑戰：記錄最高樓層、還原符文/卡片快照，
        /// 但不像「回標題」一樣離開高塔模式——直接重新開始新的一輪（回到高塔選單頁、樓層歸 1）。</summary>
        public static void AbandonRun()
        {
            if (!IsActive) return;

            if (CurrentFloor > BestFloor)
            {
                PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                PlayerPrefs.Save();
            }

            RestoreRuneSnapshot();
            RestoreCardVariantSnapshot();
            StartRun();
        }

        /// <summary>在畫面左上角顯示目前樓層，不依賴任何場景既有的 UI 物件。</summary>
        public static void ShowFloorHud()
        {
            var canvasGO = new GameObject("TowerFloorHud");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            canvasGO.AddComponent<CanvasScaler>();

            var textGO = new GameObject("FloorText", typeof(Text));
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.GetComponent<Text>();
            text.text = $"第 {CurrentFloor} 層";
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;

            Font font = null;
            foreach (var t in Object.FindObjectsOfType<Text>())
            {
                if (t.font != null) { font = t.font; break; }
            }
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.font = font;

            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            rt.sizeDelta = new Vector2(300f, 60f);
        }

        public static int RollMonsterHP(int baseHP)
        {
            var floorScale = 1f + (CurrentFloor - 1) * 0.06f;
            var jitter = Random.Range(0.85f, 1.25f);
            return Mathf.Max(1, Mathf.RoundToInt(baseHP * floorScale * jitter));
        }

        /// <summary>怪物攻擊力每 10 層 +20%：1~10 層 1.0 倍、11~20 層 1.2 倍、21~30 層 1.4 倍，以此類推。</summary>
        public static float GetMonsterAtkScale()
        {
            var tier = (CurrentFloor - 1) / 10;
            return 1f + tier * 0.2f;
        }

        public static Sprite RollMonsterSprite()
        {
            EnsurePoolsLoaded();
            if (spritePool == null || spritePool.Count == 0) return null;
            return spritePool[Random.Range(0, spritePool.Count)];
        }

        static void LoadFloor()
        {
            EnsurePoolsLoaded();

            TutorialController.isTutorial = false;
            TutorialController.isTutorial2 = false;

            var monsterId = monsterPool[Random.Range(0, monsterPool.Count)];
            var bg = BattleBackgrounds[Random.Range(0, BattleBackgrounds.Length)];

            PlayerPrefs.SetString("enemyName", monsterId);
            PlayerPrefs.Save();

            if (DataService.Instance != null)
            {
                DataService.Instance.scriptParameter = new ScriptParameter
                {
                    background = bg,
                    combatTarget = monsterId,
                };
            }

            SceneManager.LoadScene(CombatSceneName);
        }

        static async void FinishRun()
        {
            IsActive = false;
            RestoreRuneSnapshot();
            RestoreCardVariantSnapshot();

            await GoToTitleScene();
        }

        /// <summary>比照 ExitToTitleCommand 的做法：Title 選單是 Naninovel 常駐 UI，
        /// 只有遊戲第一次啟動時會自動 Show()，中途重載場景要自己停播/重置狀態/手動 Show()。</summary>
        static async UniTask GoToTitleScene()
        {
            Engine.GetService<IScriptPlayer>()?.Stop();
            var stateManager = Engine.GetService<IStateManager>();
            if (stateManager != null)
                await stateManager.ResetStateAsync();

            await SceneManager.LoadSceneAsync(TitleSceneName);

            // 同 ExitToTitleCommand：Title 場景沒有還原 UI 顯示/相機的邏輯，而這兩個狀態
            // 跨場景常駐、連 ResetStateAsync 都清不掉，玩過地圖後回 Title 會看不到選單。
            var uiManager = Engine.GetService<IUIManager>();
            uiManager?.SetUIVisibleWithToggle(true, false);

            var naniCamera = Engine.GetService<ICameraManager>()?.Camera;
            if (naniCamera != null) naniCamera.enabled = true;

            uiManager?.GetUI<ITitleUI>()?.Show();
        }

        static void SnapshotAndUnlockAllRunes()
        {
            foreach (var e in Elements)
            {
                SnapshotKeyIfMissing($"UnlockedRunes_{e}");
                // 裝備中的符文也要快照，不然高塔模式換符文會直接寫到玩家主線的存檔
                SnapshotKeyIfMissing($"Equipped_{e}");
            }

            PlayerPrefs.SetString("UnlockedRunes_Blue", "blue01,blue02,blue03,blue04,blue05");
            PlayerPrefs.SetString("UnlockedRunes_Red", "red01,red02,red03,red04,red05");
            PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
            PlayerPrefs.SetString("UnlockedRunes_Green", "green01,green02,green03,green04,green05");

            // 高塔模式一定要保證每個顏色都裝著「真的」符文，不能停在 xx00 那個空殼佔位符
            // （沒效果、0 費，戰鬥畫面符文欄位會壞掉）。目前沒裝過或還在用 00 預設的話，強制換成 01 等級符文。
            foreach (var e in Elements)
            {
                var key = $"Equipped_{e}";
                var current = PlayerPrefs.GetString(key, "");
                if (string.IsNullOrEmpty(current) || current.EndsWith("00"))
                    PlayerPrefs.SetString(key, $"{e.ToLower()}01");
            }

            PlayerPrefs.Save();

            var vars = Engine.GetService<ICustomVariableManager>();
            vars?.SetVariableValue("RuneActive", "True");
        }

        static void RestoreRuneSnapshot()
        {
            foreach (var e in Elements)
            {
                RestoreSnapshottedKey($"UnlockedRunes_{e}");
                RestoreSnapshottedKey($"Equipped_{e}");
            }
            PlayerPrefs.Save();
        }

        static void SnapshotAndUnlockAllCardVariants()
        {
            foreach (var e in Elements)
                SnapshotKeyIfMissing($"UnlockedCardVariant_{e}");

            foreach (var e in Elements)
                PlayerPrefs.SetString($"UnlockedCardVariant_{e}", "A,B");
            PlayerPrefs.Save();
        }

        static void RestoreCardVariantSnapshot()
        {
            foreach (var e in Elements)
                RestoreSnapshottedKey($"UnlockedCardVariant_{e}");
            PlayerPrefs.Save();
        }

        // 快照存進 PlayerPrefs（不是純記憶體），這樣就算遊戲中途被關掉、
        // 高塔進度沒有正常結束，玩家原本的解鎖狀態也不會遺失，之後還原得回來。
        static void SnapshotKeyIfMissing(string key)
        {
            var snapKey = SnapshotKeyPrefix + key;
            if (!PlayerPrefs.HasKey(snapKey))
                PlayerPrefs.SetString(snapKey, PlayerPrefs.GetString(key, ""));
        }

        static void RestoreSnapshottedKey(string key)
        {
            var snapKey = SnapshotKeyPrefix + key;
            if (!PlayerPrefs.HasKey(snapKey)) return;
            PlayerPrefs.SetString(key, PlayerPrefs.GetString(snapKey, ""));
            PlayerPrefs.DeleteKey(snapKey);
        }

        static void EnsurePoolsLoaded()
        {
            if (monsterPool != null && spritePool != null) return;

            var allMobs = Resources.LoadAll<MobData>("MobData")
                .Where(m => m != null && !m.name.StartsWith("TestMobData"))
                .ToList();

            monsterPool = allMobs.Select(m => m.name).ToList();
            spritePool = allMobs.Where(m => m.sprite != null).Select(m => m.sprite).ToList();
        }
    }
}
