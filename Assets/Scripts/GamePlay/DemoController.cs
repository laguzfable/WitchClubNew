using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Naninovel;

/// <summary>
/// 自動化展示 Demo 控制器。
/// 掛在 CombatScene 的任意 GameObject 上。
///
/// 啟動條件：DataService.scriptParameter.combatTarget 為以下其中一個值
///   "demoMob"   → 完整 6 幕展示（由主 demo.nani 觸發）
///   "mobMei"    → 魅兒：紅系強攻 + 環境效果 + 四色合技
///   "mobVivia"  → 薇狄亞：綠系治癒 + 環境效果 + 藍系防禦
///   "mobEuphie" → 優菲：藍系護盾 + 環境效果 + 符文技能
///   "mobNelly"  → 涅莉：符文技能 + 魔力狂潮 + 四色合技
///
/// 編輯器測試時勾選 debugAlwaysRun 可無條件執行完整展示。
/// </summary>
[DefaultExecutionOrder(1000)]
public class DemoController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────
    [Header("Activation")]
    [Tooltip("勾選後無視 Target，強制執行完整 Demo（編輯器測試用）")]
    [SerializeField] bool debugAlwaysRun = false;

    [Header("Font")]
    [Tooltip("支援中文的 TMP Font Asset，拖入 NotoSansCJKtc-Regular SDF")]
    [SerializeField] TMP_FontAsset chineseFont;

    [Header("Timing (seconds)")]
    [SerializeField] float initDelay       = 3.5f;
    [SerializeField] float labelShowTime   = 2.0f;
    [SerializeField] float cardSelectDelay = 0.28f;
    [SerializeField] float postPlayWait    = 3.2f;
    [SerializeField] float actGap          = 0.8f;

    // ── Runtime ───────────────────────────────────────────────────
    CombatSystem    cs;
    PlayerController pc;

    CanvasGroup      overlayRoot;
    TextMeshProUGUI  titleTMP;
    TextMeshProUGUI  descTMP;
    Image            accentLine;

    // ═════════════════════════════════════════════════════════════
    void Start()
    {
        if (!ShouldActivate()) return;

        cs = GameObject.FindWithTag("GameController").GetComponent<CombatSystem>();
        pc = GameObject.FindWithTag("Player").GetComponent<PlayerController>();

        var target = DataService.Instance?.scriptParameter?.combatTarget?.Value ?? "";
        var label  = DataService.Instance?.scriptParameter?.scriptLabel?.Value ?? "";

        // monster03 出現兩次：練習戰(afterpractice) 和 最終戰(finalending)
        // 只有最終戰才需要強制四色角色牌
        bool isFinalBattle = target == "mobFinal"
                          || (target == "monster03" && label == "finalending");

        if (isFinalBattle)
        {
            // 解鎖全部符文（key 格式必須與 SelectRuneCard 一致：enum 名稱）
            PlayerPrefs.SetString("UnlockedRunes_Blue",   "blue01,blue02,blue03,blue04,blue05");
            PlayerPrefs.SetString("UnlockedRunes_Red",    "red01,red02,red03,red04,red05");
            PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
            PlayerPrefs.SetString("UnlockedRunes_Green",  "green01,green02,green03,green04,green05");
            PlayerPrefs.SetString("UnlockedRunes_None",   "mon02,mon04,mon08,mon09,mon10,mon12");
            PlayerPrefs.Save();

            // [DefaultExecutionOrder(1000)] 確保此時 PlayerController.Start() 和
            // CombatSystem.Start()（PrepareBeginTurn）都已跑完，直接覆蓋手牌
            // 第5張明確給元素牌（101–104 隨機），避免隨機發到角色牌造成重複
            int[] elementPool = { 101, 102, 103, 104 };
            int rand5th = elementPool[UnityEngine.Random.Range(0, elementPool.Length)];
            ForceHand(new[] { 1, 2, 4, 8, rand5th }); // 四色女角牌 + 隨機元素牌
            return;
        }

        BuildOverlayUI();
        RunDemoForTarget().Forget();
    }

    bool ShouldActivate()
    {
        if (debugAlwaysRun) return true;
        var t = DataService.Instance?.scriptParameter?.combatTarget?.Value;
        var l = DataService.Instance?.scriptParameter?.scriptLabel?.Value;
        // mobMei / mobVivia / mobEuphie 由 DemoCombatController 處理（教學樣式）
        // mobNelly 自由遊玩，不需要 overlay
        // monster03 出現兩次：只有 label=finalending 的最終戰才啟動
        if (t == "monster03") return l == "finalending";
        return t == "demoMob" || t == "mobFinal";
    }

    // ═════════════════════════════════════════════════════════════
    // SCENARIO ROUTER
    // ═════════════════════════════════════════════════════════════
    async UniTaskVoid RunDemoForTarget()
    {
        await Delay(initDelay);

        var target = DataService.Instance?.scriptParameter?.combatTarget?.Value ?? "";

        switch (target)
        {
            case "mobMei":    await RunMeiScenario();    break;
            case "mobVivia":  await RunViviaScenario();  break;
            case "mobEuphie": await RunEuphieScenario(); break;
            case "mobNelly":  await RunNellyScenario();  break;
            default:          await RunFullDemo();       break;
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 魅兒・血系女巫  ─  紅系強攻 → 絳紅之夜 → 四色合技
    // ═════════════════════════════════════════════════════════════
    async UniTask RunMeiScenario()
    {
        await ShowLabel(
            Loc("RED ASSAULT",   "紅系強攻", "血系猛攻"),
            Loc("Same-element cards stack damage — the more RED, the harder it hits",
                "同色卡疊加傷害——越多紅色，傷害越高",
                "同色カードでダメージ上昇——赤が多いほど強力！"));
        await WaitTurn();
        ForceHand(new[] { 101, 101, 101, 102, 103 });
        await SelectByElement(ECardElement.Red, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("CRIMSON NIGHT", "絳紅之夜", "深紅の夜"),
            Loc("ENVIRONMENT EFFECT — Attack x2! Blood magic goes critical",
                "環境效果——攻擊翻倍！血系魔法暴走",
                "環境効果——攻撃力2倍！血魔法が暴走する"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Attack, 2);
        await WaitTurn();
        ForceHand(new[] { 101, 101, 102, 103, 104 });
        await SelectByElement(ECardElement.Red, 2);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("4-COLOR COMBO", "四色合技", "四色コンボ"),
            Loc("All four elements at once — damage x4!",
                "四種屬性同時出擊——傷害×4！",
                "全四属性を同時に——ダメージ×4！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro(
            Loc("Mei's Challenge Complete!", "魅兒的挑戰完成！", "メルの挑戦クリア！"),
            Loc("Blood Magic Showcase — END", "鮮血魔法展示——結束", "血魔法ショーケース——終了"));
    }

    // ═════════════════════════════════════════════════════════════
    // 薇狄亞・自然系女巫  ─  綠系治癒 → 生命之雨 → 藍系防禦
    // ═════════════════════════════════════════════════════════════
    async UniTask RunViviaScenario()
    {
        await ShowLabel(
            Loc("NATURE HEALING", "自然治癒", "自然の回復"),
            Loc("Green cards restore HP — stack them for big recovery",
                "綠色卡恢復HP——越多越有效",
                "緑カードでHP回復——重ねるほど効果大！"));
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("LIFE RAIN", "生命之雨", "命の雨"),
            Loc("ENVIRONMENT EFFECT — Healing x2! Rain washes all wounds",
                "環境效果——治療翻倍！雨水洗去所有傷痕",
                "環境効果——回復力2倍！雨がすべての傷を癒す"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Heal, 2);
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("GUARDIAN SHIELD", "守護護盾", "守護の盾"),
            Loc("DEFENSE — Blue cards build a shield, blocking incoming damage",
                "防禦——藍色卡建構護盾，阻擋傷害",
                "防御——青カードで盾を構築し、ダメージを防ぐ"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowOutro(
            Loc("Vivia's Challenge Complete!", "薇狄亞的挑戰完成！", "ヴィヴィアの挑戦クリア！"),
            Loc("Nature Magic Showcase — END", "自然魔法展示——結束", "自然魔法ショーケース——終了"));
    }

    // ═════════════════════════════════════════════════════════════
    // 優菲・象牙塔女巫  ─  藍系護盾 → 高塔之暮 → 符文技能
    // ═════════════════════════════════════════════════════════════
    async UniTask RunEuphieScenario()
    {
        await ShowLabel(
            Loc("ARCANE SHIELD", "魔法護盾", "魔法の盾"),
            Loc("DEFENSE — Ivory Tower formation, more blue cards = harder wall",
                "防禦——象牙塔陣型，藍色越多護盾越厚",
                "防御——象牙塔陣形、青が多いほど堅固な壁に"));
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("TOWER DUSK", "高塔之暮", "塔の黄昏"),
            Loc("ENVIRONMENT EFFECT — Defense x2! Iron wall activated",
                "環境效果——防禦翻倍！鐵壁啟動",
                "環境効果——防御力2倍！鉄壁発動"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Defense, 2);
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("RUNE ABILITY", "符文能力", "ルーン能力"),
            Loc("Arcane energy fully charged — unleash the special skill!",
                "魔力充滿——釋放特殊技能！",
                "魔力が満ちた——特殊スキルを解放！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 101 });
        await SelectByElement(ECardElement.Red, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowOutro(
            Loc("Euphie's Challenge Complete!", "優菲的挑戰完成！", "ユーフィの挑戦クリア！"),
            Loc("Ivory Tower Magic Showcase — END", "象牙塔魔法展示——結束", "象牙塔魔法ショーケース——終了"));
    }

    // ═════════════════════════════════════════════════════════════
    // 涅莉・瓶中精靈  ─  符文技能 → 魔力狂潮 → 四色合技 × 2
    // ═════════════════════════════════════════════════════════════
    async UniTask RunNellyScenario()
    {
        await ShowLabel(
            Loc("SPIRIT RUNE", "精靈符文", "精霊ルーン"),
            Loc("RUNE ABILITY — Nelly's fairy power sealed for centuries, unleashed!",
                "符文能力——涅莉封印百年的精靈之力，解放！",
                "ルーン能力——ネリーの封じられた精霊の力が解放される！"));
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 8 });
        await SelectByElement(ECardElement.Yellow, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("MANA SURGE", "魔力狂潮", "魔力の奔流"),
            Loc("ENVIRONMENT EFFECT — Energy gain x2! Rune charges again instantly",
                "環境效果——能量獲取翻倍！符文瞬間再充能",
                "環境効果——獲得エネルギー2倍！ルーンが即再充電"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Energy, 2);
        await WaitTurn();
        ForceHand(new[] { 8, 8, 104, 101, 102 });
        await SelectByElement(ECardElement.Yellow, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("4-COLOR COMBO", "四色合技", "四色コンボ"),
            Loc("All four elements — damage x4! Fairy's ultimate combination!",
                "四種屬性——傷害×4！精靈的終極組合！",
                "全四属性——ダメージ×4！精霊の究極コンビネーション！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro(
            Loc("Nelly's Challenge Complete!", "涅莉的挑戰完成！", "ネリーの挑戦クリア！"),
            Loc("Spirit Rune Showcase — END", "精靈符文展示——結束", "精霊ルーンショーケース——終了"));
    }

    // ═════════════════════════════════════════════════════════════
    // 完整 Demo（demoMob）  ─  6 幕全功能展示
    // ═════════════════════════════════════════════════════════════
    async UniTask RunFullDemo()
    {
        await ShowLabel(
            Loc("BASIC ATTACK", "基礎攻擊", "基本攻撃"),
            Loc("Same-color cards stack power — more RED = more damage",
                "同色卡疊加力量——越多紅色=傷害越高",
                "同色カードで力が増す——赤が多いほどダメージ大！"));
        await WaitTurn();
        ForceHand(new[] { 101, 101, 101, 102, 103 });
        await SelectByElement(ECardElement.Red, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("DEFENSE", "防禦", "防御"),
            Loc("Blue cards build a shield — block incoming enemy damage",
                "藍色卡建構護盾——阻擋敵人傷害",
                "青カードで盾を構築——敵のダメージを防ぐ"));
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("CRIMSON NIGHT", "絳紅之夜", "深紅の夜"),
            Loc("ENVIRONMENT EFFECT — Attack x2! The battlefield changes every turn",
                "環境效果——攻擊翻倍！戰場每回合都在變化",
                "環境効果——攻撃力2倍！戦場は毎ターン変化する"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Attack, 2);
        await WaitTurn();
        ForceHand(new[] { 101, 101, 102, 103, 104 });
        await SelectByElement(ECardElement.Red, 2);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("LIFE RAIN", "生命之雨", "命の雨"),
            Loc("HEALING — Green cards restore HP, doubled by environment effect!",
                "治療——綠色卡恢復HP，被環境效果加倍！",
                "回復——緑カードでHP回復、環境効果で2倍！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Heal, 2);
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("RUNE ABILITY", "符文能力", "ルーン能力"),
            Loc("Charge energy then release — trigger a powerful special effect!",
                "充能後釋放——觸發強力特殊效果！",
                "エネルギーを溜めて解放——強力な特殊効果を発動！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 101 });
        await SelectByElement(ECardElement.Red, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel(
            Loc("4-COLOR COMBO", "四色合技", "四色コンボ"),
            Loc("All four elements at once — damage x4, combo animation!",
                "四種屬性同時出擊——傷害×4，連擊動畫！",
                "全四属性同時出撃——ダメージ×4、コンボアニメーション！"));
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro(
            Loc("Thank you for watching!", "感謝您的體驗！", "ご体験ありがとうございました！"),
            Loc("HEXE — Where strategy meets magic", "HEXE——策略與魔法的交匯之地", "HEXE——戦略と魔法が交わる世界"));
    }

    // ═════════════════════════════════════════════════════════════
    // HELPERS
    // ═════════════════════════════════════════════════════════════

    async UniTask WaitTurn()
    {
        await UniTask.WaitUntil(() => pc.playBtn != null && pc.playBtn.interactable);
        await Delay(0.25f);
    }

    void ForceHand(int[] ids)
    {
        var cards = pc.GetCards();
        for (int i = 0; i < Mathf.Min(ids.Length, cards.Length); i++)
        {
            cards[i].ID      = ids[i];
            cards[i].element = ElementFromID(ids[i]);
        }
        pc.ResetAttr();
    }

    async UniTask SelectByElement(ECardElement element, int count)
    {
        int selected = 0;
        foreach (var card in pc.GetCards())
        {
            if (card.element == element && selected < count)
            {
                card.SetSelectState(true);
                pc.CalculateAttr();
                selected++;
                await Delay(cardSelectDelay);
            }
        }
    }

    async UniTask SelectBaseCards()
    {
        foreach (var card in pc.GetCards())
        {
            if (card.ID <= 8)
            {
                card.SetSelectState(true);
                pc.CalculateAttr();
                await Delay(cardSelectDelay);
            }
        }
    }

    void TriggerFirstRune()
    {
        foreach (var rune in pc.GetWitchCards())
        {
            if (rune != null && rune.cost != null &&
                rune.cost.Value >= rune.cost.GetTotalValue() &&
                rune.isControllable)
            {
                rune.OnClick();
                return;
            }
        }
    }

    static ECardElement ElementFromID(int id)
    {
        if (id == 1  || id == 101) return ECardElement.Red;
        if (id == 2  || id == 102) return ECardElement.Blue;
        if (id == 4  || id == 103) return ECardElement.Green;
        if (id == 8  || id == 104) return ECardElement.Yellow;
        return ECardElement.None;
    }

    // ─────────────────────────────────────────────────────────────
    // LOCALIZATION HELPER
    // ─────────────────────────────────────────────────────────────

    /// <summary>根據 PlayerPrefs Language 選擇對應語言字串，不支援的語系 fallback 到 en。</summary>
    static string Loc(string en, string zhTW, string ja)
    {
        var lang = PlayerPrefs.GetString("Language", "").ToLower();
        if (lang.StartsWith("zh")) return zhTW;
        if (lang.StartsWith("ja")) return ja;
        return en;
    }

    static UniTask Delay(float s) => UniTask.Delay(TimeSpan.FromSeconds(s));

    // ═════════════════════════════════════════════════════════════
    // OVERLAY UI
    // ═════════════════════════════════════════════════════════════

    void BuildOverlayUI()
    {
        var canvasGO = new GameObject("[Demo] Overlay");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var rootGO = new GameObject("Root");
        rootGO.transform.SetParent(canvasGO.transform, false);
        SetAnchors(rootGO, Vector2.zero, Vector2.one);
        overlayRoot = rootGO.AddComponent<CanvasGroup>();
        overlayRoot.alpha = 0f;
        overlayRoot.blocksRaycasts = false;

        // Floating panel — upper-right, beside the monster, does not cover cards
        var panelGO = new GameObject("DarkPanel");
        panelGO.transform.SetParent(rootGO.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        SetAnchors(panelGO, new Vector2(0.52f, 0.60f), new Vector2(0.98f, 0.88f));

        // Gold accent line (top edge of panel)
        var lineGO = new GameObject("AccentLine");
        lineGO.transform.SetParent(rootGO.transform, false);
        accentLine = lineGO.AddComponent<Image>();
        accentLine.color = new Color(1f, 0.85f, 0.3f, 1f);
        var lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.52f, 0.878f);
        lineRT.anchorMax = new Vector2(0.98f, 0.883f);
        lineRT.offsetMin = lineRT.offsetMax = Vector2.zero;

        // Title
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(rootGO.transform, false);
        titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize  = 42;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        if (chineseFont != null) titleTMP.font = chineseFont;
        SetAnchors(titleGO, new Vector2(0.53f, 0.74f), new Vector2(0.97f, 0.87f));

        // Description
        var descGO = new GameObject("DescText");
        descGO.transform.SetParent(rootGO.transform, false);
        descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.fontSize  = 24;
        descTMP.color     = new Color(1f, 0.92f, 0.6f, 1f);
        if (chineseFont != null) descTMP.font = chineseFont;
        SetAnchors(descGO, new Vector2(0.53f, 0.61f), new Vector2(0.97f, 0.74f));

        // Watermark
        BuildWatermark(canvasGO.transform);
    }

    void BuildWatermark(Transform parent)
    {
        var go  = new GameObject("Watermark");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "HEXE  ·  Card Combat Demo";
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.fontSize  = 20;
        tmp.color     = Color.white;
        SetAnchors(go, new Vector2(0.52f, 0.88f), new Vector2(0.98f, 0.94f));
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0.55f;
        cg.blocksRaycasts = false;
    }

    static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    async UniTask ShowLabel(string title, string desc)
    {
        titleTMP.text = title;
        descTMP.text  = desc;

        var titleRT = titleTMP.GetComponent<RectTransform>();
        var origPos = titleRT.anchoredPosition;
        titleRT.anchoredPosition = origPos + new Vector2(-60f, 0f);

        overlayRoot.DOFade(1f, 0.35f);
        titleRT.DOAnchorPos(origPos, 0.4f).SetEase(Ease.OutCubic);

        await Delay(labelShowTime);

        overlayRoot.DOFade(0f, 0.3f);
        await Delay(0.35f);
    }

    async UniTask ShowOutro(string title, string desc)
    {
        titleTMP.text  = title;
        descTMP.text   = desc;
        titleTMP.fontSize = 64;

        overlayRoot.DOFade(1f, 0.6f);
        accentLine.DOColor(new Color(1f, 0.4f, 0.4f, 1f), 0.8f)
                  .SetLoops(-1, LoopType.Yoyo);

        await Delay(5f);
    }
}
