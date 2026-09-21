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
        // 換裝頁的場景名稱是 public 的：右側詳情面板要靠它判斷自己在哪一頁（符文／卡片／護身符）
        public const string ChangeRuneSceneName = "ChangeRuneScene";
        public const string ChangeCardSceneName = "ChangeCardTypeScene";
        public const string ChangeAmuletSceneName = "ChangeAmuletScene";
        const string HubSceneName = "TowerHubScene";
        const string BestFloorKey = "TowerMode.BestFloor";
        const string IsActiveKey = "TowerMode.IsActive";
        const string CurrentFloorKey = "TowerMode.CurrentFloor";

        /// <summary>從這層開始不再每場補滿血，帶著傷繼續打（蘇菲亞會在這層的休息頁解說）。</summary>
        public const int NoHealFromFloor = 50;
        const string CarriedHPKey = "TowerMode.CarriedHP";
        const string LastBackgroundKey = "TowerMode.LastBackground";
        const string SnapshotKeyPrefix = "TowerMode.Snapshot.";
        const string EquippedAmuletsKey = "TowerMode.EquippedAmulets";
        const string AdapterAmuletId = "Adapter";
        const string DimensionAmuletId = "Dimension";
        const string FullEnergyAmuletId = "FullEnergy";
        const string WitchRuneAmuletId = "WitchRune";
        const string LilyRuneAmuletId = "LilyRune";
        const string LilyReadyKey = "TowerMode.LilyReady";

        static readonly string[] Elements = { "Blue", "Red", "Yellow", "Green" };
        static readonly string[] BattleBackgrounds =
            { "bg00", "bg01", "bg02", "bg03", "bg04", "bg05", "bg06", "bg07", "bg08", "bg11", "bg12" };

        static List<string> monsterPool;
        static List<Sprite> spritePool;
        static MobData arenaMobData;

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

        /// <summary>「百合符文」的觸發條件是否成立：上一場戰鬥是不是滿血結束的。</summary>
        public static bool LilyBonusReady => PlayerPrefs.GetInt(LilyReadyKey, 0) == 1;

        /// <summary>
        /// 戰鬥開場保證會有幾張角色卡在手上。
        /// 女巫符文 +1（每場都給），百合符文 +2（要上一場滿血結束）。兩者相加。
        /// 護身符欄位平常只有 1 格，要同時裝這兩顆得先上「護符轉接頭」，
        /// 所以實際上限就是 3 張——剛好也是題目說的那個數字。
        /// </summary>
        public static int GuaranteedCharacterCards
        {
            get
            {
                if (!IsActive) return 0;

                var count = 0;
                if (IsAmuletEquipped(WitchRuneAmuletId)) count += 1;
                if (IsAmuletEquipped(LilyRuneAmuletId) && LilyBonusReady) count += 2;
                return count;
            }
        }

        /// <summary>
        /// 由 CombatSystem 在一場戰鬥結束時呼叫，記下這場是不是滿血過關，給下一場的百合符文用。
        /// 每場都會覆寫，所以條件不會累積——沒滿血的那一場就把加成關掉了。
        /// </summary>
        public static void RecordBattleEndHP(bool isLose, float curHP, float maxHP)
        {
            if (!IsActive) return;

            // 輸掉當然不算。血量留一點容差，免得治療/減傷算出 99.9999 這種浮點結果被判成沒滿血。
            var full = !isLose && maxHP > 0f && curHP >= maxHP - 0.01f;

            PlayerPrefs.SetInt(LilyReadyKey, full ? 1 : 0);

            // 剩多少血留給下一場（第 50 層起才會用到，但每場都記——第 50 層開打時
            // 要接的就是第 49 層打完剩下的血）。輸了就不用留，反正要從第一層重來。
            if (isLose) PlayerPrefs.DeleteKey(CarriedHPKey);
            else PlayerPrefs.SetFloat(CarriedHPKey, curHP);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 第 <see cref="NoHealFromFloor"/> 層起，這場的血要接著上一場打完剩下的。
        /// 讀得到值才回 true；讀不到（還沒打過、或剛重來）就照原本滿血開打。
        /// </summary>
        public static bool TryGetCarriedHP(out float hp)
        {
            hp = 0f;
            if (!IsActive || CurrentFloor < NoHealFromFloor) return false;
            if (!PlayerPrefs.HasKey(CarriedHPKey)) return false;

            hp = PlayerPrefs.GetFloat(CarriedHPKey, 0f);
            return hp > 0f;
        }

        /// <summary>重來一輪時要清掉，不然會帶著上一輪的殘血開打。</summary>
        static void ResetCarriedHP()
        {
            PlayerPrefs.DeleteKey(CarriedHPKey);
            PlayerPrefs.Save();
        }

        /// <summary>開新挑戰／放棄挑戰時清掉百合符文的狀態，不然會沿用上一輪最後一場的結果。</summary>
        static void ResetLilyBonus()
        {
            PlayerPrefs.SetInt(LilyReadyKey, 0);
            PlayerPrefs.Save();
        }

        public static void StartRun()
        {
            UnlockArenaOpenAchievement();
            EnsurePoolsLoaded();
            SnapshotAndUnlockAllRunes();
            SnapshotAndUnlockAllCardVariants();
            ResetLilyBonus(); // 上一輪最後一場的滿血狀態不能帶進新挑戰的第一場
            ResetCarriedHP();

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

            UnlockArenaOpenAchievement();
            EnsurePoolsLoaded();
            IsActive = true;
            CurrentFloor = PlayerPrefs.GetInt(CurrentFloorKey, 1);

            // 續關也要重開一次符文/卡片。原本只有「開始新挑戰」開，靠的是 PlayerPrefs 裡還留著
            // 上次開好的值——但那份值只要被清掉過（開新遊戲、進度重置），續關就只剩玩家自己解的，
            // 競技場會突然變得很難打。快照已經存在的話不會被覆蓋，所以重跑是安全的。
            SnapshotAndUnlockAllRunes();
            SnapshotAndUnlockAllCardVariants();

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
                RecordBestFloor();
                ResetCarriedHP();
                CurrentFloor = 1; // 死掉重新從第一層開始（跟主動「退出」不一樣，退出不會重置）
                BackToHub();
                return;
            }

            RecordBestFloor();

            var clearedFloor = CurrentFloor;

            if (clearedFloor >= MaxFloor)
            {
                FinishRun();
                return;
            }

            CurrentFloor++;

            // 平常打贏直接接下一場，但規則要變的那一層得先停下來——
            // 不回休息頁的話，蘇菲亞沒機會講「這裡開始不補血了」（見 TowerHubAmbientLive2D）。
            if (CurrentFloor == NoHealFromFloor) BackToHub();
            else LoadFloor();
        }

        /// <summary>由戰鬥畫面的「退出」按鈕呼叫，放棄本場戰鬥、記錄目前樓層，回到高塔選單頁（本次挑戰不會結束）。</summary>
        public static void QuitRun()
        {
            if (!IsActive) return;

            RecordBestFloor();

            // 百合符文的加成要清掉：中途退出不算「滿血結束」，那場根本沒打完。
            // 不清的話會變成——滿血贏一場拿到加成後，之後每次打不順就退出重來，
            // 加成永遠留著，等於無限次帶著 2 張角色卡重試同一層。
            ResetLilyBonus();
            StopBgm();          // 中途退出時戰鬥曲還在放，不收會跟著進大廳

            BackToHub();
        }

        /// <summary>由選單頁的「回標題」按鈕呼叫。跟直接關掉遊戲一樣：本次挑戰不會結束，
        /// 樓層/符文快照維持原樣，下次從標題進高塔模式會直接續關（見 HasSavedRun）。</summary>
        public static async void QuitToTitle()
        {
            if (!IsActive) return;

            RecordBestFloor();

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

            RecordBestFloor();

            RestoreRuneSnapshot();
            RestoreCardVariantSnapshot();
            StartRun();
        }

        /// <summary>
        /// 更新最高樓層紀錄，順便看看有沒有踩到成就門檻。
        /// 戰敗／退出／回標題／放棄挑戰／打贏一場都會呼叫——記的是「到達過的最高層」，
        /// 不是通關層數，所以死在第 30 層也算到達過 30。
        /// </summary>
        /// <summary>把音樂收掉。競技場的曲子是直接 PlayBgmAsync 放的，
        /// 不在 Naninovel 的狀態裡，換場景不會自己停。</summary>
        static void StopBgm()
        {
            try
            {
                var audio = Engine.GetService<IAudioManager>();
                if (audio != null) audio.StopAllBgmAsync(0.3f).Forget();
            }
            catch (System.Exception ex) { Debug.LogWarning($"[TowerMode] 停 BGM 失敗：{ex.Message}"); }
        }

        static void RecordBestFloor()
        {
            if (CurrentFloor > BestFloor)
            {
                PlayerPrefs.SetInt(BestFloorKey, CurrentFloor);
                PlayerPrefs.Save();
            }

            // 用 >= 而不是 ==：Steam 還沒就緒時 Unlock 會直接放棄，門檻只判一次的話那次就永遠掉了。
            // 重複呼叫是安全的（AchievementManager 自己會先查已解鎖狀態）。
            var best = BestFloor;
            if (best >= 10) AchievementManager.Instance?.Unlock(AchievementManager.ACH_ARENA_FLOOR_10);
            if (best >= 25) AchievementManager.Instance?.Unlock(AchievementManager.ACH_ARENA_FLOOR_25);
            if (best >= 50) AchievementManager.Instance?.Unlock(AchievementManager.ACH_ARENA_FLOOR_50);
        }

        /// <summary>
        /// 「女巫競技場開啟」成就。正常情況下拿到第一個結局的當下就會由
        /// AchievementManager 發出去，這裡是補給「更新前就已經有結局」的存檔。
        /// </summary>
        static void UnlockArenaOpenAchievement()
        {
            if (EndingRecord.Count <= 0) return;

            AchievementManager.Instance?.Unlock(AchievementManager.ACH_ARENA_OPEN);
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

        /// <summary>
        /// 女巫競技場專用的怪物數值表。競技場的怪只借用池子裡的立繪，戰鬥數值一律走這張表——
        /// 劇情怪的數值是照各章難度曲線調的（monster03~05 是前期弱怪，一回合只打 6），
        /// 直接丟進隨機池會讓同一層的強度差到 2 倍以上。分開算之後，調競技場平衡不會動到劇情。
        /// </summary>
        public static MobData GetArenaMobData()
        {
            if (arenaMobData == null)
                arenaMobData = Resources.Load<MobData>("TowerMode/ArenaMob");

            return arenaMobData;
        }

        /// <summary>上一層抽到的立繪名。跨場景要留著，所以連 PlayerPrefs 一起存
        /// （挑戰中途關掉遊戲、下次再接著打，也不會第一場就撞上剛剛那隻）。</summary>
        const string LastSpriteKey = "TowerMode.LastMobSprite";

        /// <summary>
        /// 抽一張怪物立繪。玩家眼裡「這層打的是誰」就是這張圖，所以不讓它跟上一層同一張——
        /// 連續兩層一樣的話，看起來像卡住了。池子只剩一張時就只好重複。
        /// </summary>
        public static Sprite RollMonsterSprite()
        {
            EnsurePoolsLoaded();
            if (spritePool == null || spritePool.Count == 0) return null;

            var last = PlayerPrefs.GetString(LastSpriteKey, "");
            var picked = spritePool[Random.Range(0, spritePool.Count)];
            if (spritePool.Count > 1 && picked.name == last)
            {
                // 重抽一次就夠：從「除了上一張以外」的那些裡面挑。
                var index = Random.Range(0, spritePool.Count - 1);
                for (int i = 0, seen = 0; i < spritePool.Count; i++)
                {
                    if (spritePool[i].name == last) continue;
                    if (seen++ == index) { picked = spritePool[i]; break; }
                }
            }

            PlayerPrefs.SetString(LastSpriteKey, picked.name);
            PlayerPrefs.Save();
            return picked;
        }

        static void LoadFloor()
        {
            EnsurePoolsLoaded();

            // 保險：進戰鬥前再確認符文是全開的。挑戰中途要是有什麼清掉了 PlayerPrefs，
            // 下一場就會補回來，不用重開挑戰。
            SnapshotAndUnlockAllRunes();

            TutorialController.isTutorial = false;
            TutorialController.isTutorial2 = false;

            var monsterId = monsterPool[Random.Range(0, monsterPool.Count)];
            var bg = RollBackground();

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

        /// <summary>抽戰鬥背景。跟立繪一樣，不讓它跟上一層同一張。</summary>
        static string RollBackground()
        {
            if (BattleBackgrounds.Length == 0) return "";

            var last = PlayerPrefs.GetString(LastBackgroundKey, "");
            var picked = BattleBackgrounds[Random.Range(0, BattleBackgrounds.Length)];
            if (BattleBackgrounds.Length > 1 && picked == last)
            {
                var index = Random.Range(0, BattleBackgrounds.Length - 1);
                for (int i = 0, seen = 0; i < BattleBackgrounds.Length; i++)
                {
                    if (BattleBackgrounds[i] == last) continue;
                    if (seen++ == index) { picked = BattleBackgrounds[i]; break; }
                }
            }

            PlayerPrefs.SetString(LastBackgroundKey, picked);
            PlayerPrefs.Save();
            return picked;
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

            // 戰鬥音樂要自己收掉。ResetStateAsync 清的是 Naninovel 自己記錄的狀態，
            // 而競技場的曲子是我們直接 PlayBgmAsync 放的，不在那份狀態裡——
            // 不停的話會一路跟著玩家回到標題畫面。
            try
            {
                var audio = Engine.GetService<IAudioManager>();
                if (audio != null) await audio.StopAllBgmAsync(0.3f);
            }
            catch (System.Exception ex) { Debug.LogWarning($"[TowerMode] 停 BGM 失敗：{ex.Message}"); }

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

            // 王要排除掉：牠的數值是照劇情戰鬥調的（單張卡 100+ ATK），
            // 混進隨機池的話玩家會在某一層被一發打死。順便也不讓王的立繪被當成雜魚皮膚用掉。
            var allMobs = Resources.LoadAll<MobData>("MobData")
                .Where(m => m != null && !m.isBoss && !m.name.StartsWith("TestMobData"))
                .ToList();

            monsterPool = allMobs.Select(m => m.name).ToList();
            spritePool = allMobs.Where(m => m.sprite != null).Select(m => m.sprite).ToList();
        }
    }
}
