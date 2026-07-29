using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Naninovel;

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
        const string BestFloorKey = "TowerMode.BestFloor";

        static readonly string[] Elements = { "Blue", "Red", "Yellow", "Green" };
        static readonly string[] BattleBackgrounds =
            { "bg00", "bg01", "bg02", "bg03", "bg04", "bg05", "bg06", "bg07", "bg08", "bg11", "bg12" };

        static List<string> monsterPool;
        static List<Sprite> spritePool;
        static Dictionary<string, string> runeSnapshot;

        public static bool IsActive { get; private set; }
        public static int CurrentFloor { get; private set; }
        public static int BestFloor => PlayerPrefs.GetInt(BestFloorKey, 0);

        public static void StartRun()
        {
            EnsurePoolsLoaded();
            SnapshotAndUnlockAllRunes();

            IsActive = true;
            CurrentFloor = 1;

            // 一開始先讓玩家選符文，按返回後會自動接到第一層（見 NaniBridgeUtility 的攔截）
            SceneManager.LoadScene(ChangeRuneSceneName);
        }

        /// <summary>由 ChangeRuneScene 的返回按鈕呼叫（透過 NaniBridgeUtility）。</summary>
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
                FinishRun();
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

            // 每 10 層讓玩家順路去換符文頁面，要不要換都可以，按返回就會回到下一層
            if (clearedFloor % 10 == 0)
                SceneManager.LoadScene(ChangeRuneSceneName);
            else
                LoadFloor();
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

        static void FinishRun()
        {
            IsActive = false;
            RestoreRuneSnapshot();
            SceneManager.LoadScene(TitleSceneName);
        }

        static void SnapshotAndUnlockAllRunes()
        {
            runeSnapshot = new Dictionary<string, string>();
            foreach (var e in Elements)
            {
                var key = $"UnlockedRunes_{e}";
                runeSnapshot[key] = PlayerPrefs.GetString(key, "");
            }

            PlayerPrefs.SetString("UnlockedRunes_Blue", "blue01,blue02,blue03,blue04,blue05");
            PlayerPrefs.SetString("UnlockedRunes_Red", "red01,red02,red03,red04,red05");
            PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
            PlayerPrefs.SetString("UnlockedRunes_Green", "green01,green02,green03,green04,green05");
            PlayerPrefs.Save();

            var vars = Engine.GetService<ICustomVariableManager>();
            vars?.SetVariableValue("RuneActive", "True");
        }

        static void RestoreRuneSnapshot()
        {
            if (runeSnapshot == null) return;
            foreach (var kv in runeSnapshot)
                PlayerPrefs.SetString(kv.Key, kv.Value);
            PlayerPrefs.Save();
            runeSnapshot = null;
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
