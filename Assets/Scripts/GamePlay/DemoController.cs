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

        BuildOverlayUI();
        RunDemoForTarget().Forget();
    }

    bool ShouldActivate()
    {
        if (debugAlwaysRun) return true;
        var t = DataService.Instance?.scriptParameter?.combatTarget?.Value;
        return t == "demoMob"   || t == "mobMei" ||
               t == "mobVivia"  || t == "mobEuphie" || t == "mobNelly";
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
        await ShowLabel("RED ASSAULT", "Same-element cards stack damage — the more RED, the harder it hits");
        await WaitTurn();
        ForceHand(new[] { 101, 101, 101, 102, 103 });
        await SelectByElement(ECardElement.Red, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("CRIMSON NIGHT", "ENVIRONMENT EFFECT — Attack x2! Blood magic goes critical");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Attack, 2);
        await WaitTurn();
        ForceHand(new[] { 101, 101, 102, 103, 104 });
        await SelectByElement(ECardElement.Red, 2);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("4-COLOR COMBO", "All four elements at once — damage x4!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro("Mei's Challenge Complete!", "Blood Magic Showcase — END");
    }

    // ═════════════════════════════════════════════════════════════
    // 薇狄亞・自然系女巫  ─  綠系治癒 → 生命之雨 → 藍系防禦
    // ═════════════════════════════════════════════════════════════
    async UniTask RunViviaScenario()
    {
        await ShowLabel("NATURE HEALING", "Green cards restore HP — stack them for big recovery");
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("LIFE RAIN", "ENVIRONMENT EFFECT — Healing x2! Rain washes all wounds");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Heal, 2);
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("GUARDIAN SHIELD", "DEFENSE — Blue cards build a shield, blocking incoming damage");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowOutro("Vivia's Challenge Complete!", "Nature Magic Showcase — END");
    }

    // ═════════════════════════════════════════════════════════════
    // 優菲・象牙塔女巫  ─  藍系護盾 → 高塔之暮 → 符文技能
    // ═════════════════════════════════════════════════════════════
    async UniTask RunEuphieScenario()
    {
        await ShowLabel("ARCANE SHIELD", "DEFENSE — Ivory Tower formation, more blue cards = harder wall");
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("TOWER DUSK", "ENVIRONMENT EFFECT — Defense x2! Iron wall activated");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Defense, 2);
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("RUNE ABILITY", "Arcane energy fully charged — unleash the special skill!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 101 });
        await SelectByElement(ECardElement.Red, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowOutro("Euphie's Challenge Complete!", "Ivory Tower Magic Showcase — END");
    }

    // ═════════════════════════════════════════════════════════════
    // 涅莉・瓶中精靈  ─  符文技能 → 魔力狂潮 → 四色合技 × 2
    // ═════════════════════════════════════════════════════════════
    async UniTask RunNellyScenario()
    {
        await ShowLabel("SPIRIT RUNE", "RUNE ABILITY — Nelly's fairy power sealed for centuries, unleashed!");
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 8 });
        await SelectByElement(ECardElement.Yellow, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("MANA SURGE", "ENVIRONMENT EFFECT — Energy gain x2! Rune charges again instantly");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Energy, 2);
        await WaitTurn();
        ForceHand(new[] { 8, 8, 104, 101, 102 });
        await SelectByElement(ECardElement.Yellow, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("4-COLOR COMBO", "All four elements — damage x4! Fairy's ultimate combination!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro("Nelly's Challenge Complete!", "Spirit Rune Showcase — END");
    }

    // ═════════════════════════════════════════════════════════════
    // 完整 Demo（demoMob）  ─  6 幕全功能展示
    // ═════════════════════════════════════════════════════════════
    async UniTask RunFullDemo()
    {
        await ShowLabel("BASIC ATTACK", "Same-color cards stack power — more RED = more damage");
        await WaitTurn();
        ForceHand(new[] { 101, 101, 101, 102, 103 });
        await SelectByElement(ECardElement.Red, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("DEFENSE", "Blue cards build a shield — block incoming enemy damage");
        await WaitTurn();
        ForceHand(new[] { 102, 102, 102, 101, 103 });
        await SelectByElement(ECardElement.Blue, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("CRIMSON NIGHT", "ENVIRONMENT EFFECT — Attack x2! The battlefield changes every turn");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Attack, 2);
        await WaitTurn();
        ForceHand(new[] { 101, 101, 102, 103, 104 });
        await SelectByElement(ECardElement.Red, 2);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("LIFE RAIN", "HEALING — Green cards restore HP, doubled by environment effect!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.Heal, 2);
        await WaitTurn();
        ForceHand(new[] { 103, 103, 103, 101, 102 });
        await SelectByElement(ECardElement.Green, 3);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("RUNE ABILITY", "Charge energy then release — trigger a powerful special effect!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        pc.AddEN(9999f);
        await Delay(0.6f);
        TriggerFirstRune();
        await Delay(3.2f);
        ForceHand(new[] { 101, 102, 103, 104, 101 });
        await SelectByElement(ECardElement.Red, 1);
        await Delay(0.4f); pc.PlayAction(); await Delay(postPlayWait); await Delay(actGap);

        await ShowLabel("4-COLOR COMBO", "All four elements at once — damage x4, combo animation!");
        cs.envEffect.SetCurrentEffect(EEnvEffectType.None);
        await WaitTurn();
        ForceHand(new[] { 1, 2, 4, 8, 101 });
        await SelectBaseCards();
        await Delay(0.5f); pc.PlayAction(); await Delay(postPlayWait + 2f); await Delay(actGap);

        await ShowOutro("Thank you for watching!", "HEXE — Where strategy meets magic");
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

        // Dark panel (bottom 28%)
        var panelGO = new GameObject("DarkPanel");
        panelGO.transform.SetParent(rootGO.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        SetAnchors(panelGO, new Vector2(0f, 0f), new Vector2(1f, 0.28f));

        // Gold accent line
        var lineGO = new GameObject("AccentLine");
        lineGO.transform.SetParent(rootGO.transform, false);
        accentLine = lineGO.AddComponent<Image>();
        accentLine.color = new Color(1f, 0.85f, 0.3f, 1f);
        var lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0f, 0.277f);
        lineRT.anchorMax = new Vector2(1f, 0.282f);
        lineRT.offsetMin = lineRT.offsetMax = Vector2.zero;

        // Title
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(rootGO.transform, false);
        titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize  = 58;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        if (chineseFont != null) titleTMP.font = chineseFont;
        SetAnchors(titleGO, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.27f));

        // Description
        var descGO = new GameObject("DescText");
        descGO.transform.SetParent(rootGO.transform, false);
        descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.fontSize  = 30;
        descTMP.color     = new Color(1f, 0.92f, 0.6f, 1f);
        if (chineseFont != null) descTMP.font = chineseFont;
        SetAnchors(descGO, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.15f));

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
        tmp.fontSize  = 22;
        tmp.color     = Color.white;
        SetAnchors(go, new Vector2(0.72f, 0.92f), new Vector2(0.98f, 0.99f));
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
