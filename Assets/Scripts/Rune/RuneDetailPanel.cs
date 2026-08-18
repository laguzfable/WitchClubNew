using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hexe.TowerMode;

/// <summary>
/// 掛在右側詳情面板上。
/// Singleton — 卡片／符文／護身符按鈕滑鼠移上去時直接呼叫 RuneDetailPanel.Instance.Show*()。
/// 三個換裝頁（ChangeRuneScene / ChangeCardTypeScene / ChangeAmuletScene）共用同一份面板結構。
/// </summary>
public class RuneDetailPanel : MonoBehaviour
{
    static RuneDetailPanel instance;

    /// <summary>
    /// 換場景之後，上一頁的面板已經被銷毀，但 static 欄位還握著它。
    /// C# 的 ?. 不認得 Unity 的「已銷毀」狀態，會照樣呼叫下去然後噴 MissingReference，
    /// 所以這裡用 Unity 的 == 比一次，把已銷毀的換成真正的 null。
    /// （高塔選單頁也有護身符按鈕，但那頁沒有詳情面板，走的就是這條路。）
    /// </summary>
    public static RuneDetailPanel Instance => instance != null ? instance : null;

    [Header("Portrait")]
    [SerializeField] private Image characterPortraitImage;

    [Header("Texts")]
    [SerializeField] private Text runeNameText;
    [SerializeField] private Text runeTypeText;
    [SerializeField] private Text effectText;

    [Header("Empty State")]
    [Tooltip("還沒選任何東西時的提示字。留空的話依場景自動判斷（符文／卡片／護身符）。")]
    [SerializeField] private string emptyHint;

    // 目前這格畫的是什麼，記成一個動作：切語言時直接重跑一次就好。
    // （不能拿畫面上的譯文再去查表——那查不回中文，會卡在舊語言。）
    private Action render;

    private void Awake()
    {
        // 場景裡的說明框只有一行多高、而且設成垂直截斷（符文說明本來就只有一句）。
        // 卡片的數值表和護身符的說明是好幾行，會直接被切掉，所以這裡統一放寬成往下溢出；
        // 說明框本來就排在面板最下面，往下長不會壓到名稱或屬性。
        if (effectText != null) effectText.verticalOverflow = VerticalWrapMode.Overflow;
        if (runeNameText != null) runeNameText.verticalOverflow = VerticalWrapMode.Overflow;

        instance = this;
        ShowEmpty();

        // 玩家在設定選單切語言時，右側面板跟著重畫
        LocaleRefresher.For(gameObject).OnRefresh(() => render?.Invoke());
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Show(Ability ability) => SetRender(() =>
    {
        SetTexts(RuneEnTranslation.TranslateName(ability.name),
                 GetLocalizedTypeName(ability.element),
                 RuneEnTranslation.TranslateDesc(ability.description));

        // 頭像：優先用 Resources/RunePortraits/<abilityID>，沒有則 fallback 用卡片圖
        var portrait = Resources.Load<Sprite>($"RunePortraits/{ability.id}");
        SetPortrait(portrait != null ? portrait : ability.image);
    });

    /// <summary>更換卡片頁：卡片資料沒有說明欄位，效果由 CardEffectText 從數值表組出來。</summary>
    public void ShowCard(CardData card)
    {
        if (card == null) return;

        SetRender(() =>
        {
            SetTexts(RuneEnTranslation.TranslateName(card.displayName),
                     GetLocalizedTypeName(card.element),
                     CardEffectText.Build(card));
            SetPortrait(card.image);
        });
    }

    /// <summary>護身符頁：說明來自 AmuletInfo 表；圖示由按鈕自己帶進來（護身符沒有獨立的立繪資源）。</summary>
    public void ShowAmulet(string amuletId, Sprite icon)
    {
        if (!AmuletInfo.TryGet(amuletId, out var info))
        {
            Debug.LogWarning($"[RuneDetailPanel] AmuletInfo 沒有這個代號的說明：'{amuletId}'");
            return;
        }

        SetRender(() =>
        {
            SetTexts(RuneEnTranslation.TranslateName(info.name),
                     RuneEnTranslation.TranslateName(info.category),
                     RuneEnTranslation.TranslateDesc(info.description));
            SetPortrait(icon);
        });
    }

    public void Hide() => ShowEmpty();

    private void ShowEmpty() => SetRender(() =>
    {
        SetTexts(RuneEnTranslation.TranslateName(EmptyHint), "", "");
        SetPortrait(null);
    });

    /// <summary>記住這次要怎麼畫，並立刻畫一次。</summary>
    private void SetRender(Action action)
    {
        render = action;
        action();
    }

    private void SetTexts(string name, string type, string effect)
    {
        if (runeNameText != null) runeNameText.text = name;
        if (runeTypeText != null) runeTypeText.text = type;
        if (effectText != null) effectText.text = effect;
    }

    private void SetPortrait(Sprite sprite)
    {
        if (characterPortraitImage == null) return;

        characterPortraitImage.sprite = sprite;
        characterPortraitImage.enabled = sprite != null;
    }

    /// <summary>Inspector 有填就用填的，沒填就看目前在哪個換裝頁。</summary>
    private string EmptyHint
    {
        get
        {
            if (!string.IsNullOrEmpty(emptyHint)) return emptyHint;

            var scene = SceneManager.GetActiveScene().name;
            if (scene == TowerModeManager.ChangeCardSceneName) return "← 選擇一張卡片";
            if (scene == TowerModeManager.ChangeAmuletSceneName) return "← 選擇一個護身符";
            return "← 選擇一個符文";
        }
    }

    private static string GetLocalizedTypeName(ECardElement element)
    {
        switch (element)
        {
            case ECardElement.Blue:   return RuneEnTranslation.TranslateName("學院");
            case ECardElement.Red:    return RuneEnTranslation.TranslateName("血系");
            case ECardElement.Green:  return RuneEnTranslation.TranslateName("自然");
            case ECardElement.Yellow: return RuneEnTranslation.TranslateName("惡魔");
            default:                  return "";
        }
    }
}
