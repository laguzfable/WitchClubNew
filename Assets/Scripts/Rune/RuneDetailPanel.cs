using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在右側詳情面板上。
/// Singleton — SelectRuneCard 點擊時直接呼叫 RuneDetailPanel.Instance.Show()。
/// </summary>
public class RuneDetailPanel : MonoBehaviour
{
    public static RuneDetailPanel Instance { get; private set; }

    [Header("Portrait")]
    [SerializeField] private Image characterPortraitImage;

    [Header("Texts")]
    [SerializeField] private Text runeNameText;
    [SerializeField] private Text runeTypeText;
    [SerializeField] private Text effectText;

    private void Awake()
    {
        Instance = this;
        ShowEmpty();
    }

    public void Show(Ability ability)
    {
        if (runeNameText != null)
            runeNameText.text = RuneEnTranslation.TranslateName(ability.name);

        if (runeTypeText != null)
            runeTypeText.text = GetLocalizedTypeName(ability.element);

        if (effectText != null)
            effectText.text = RuneEnTranslation.TranslateDesc(ability.description);

        // 頭像：優先用 Resources/RunePortraits/<abilityID>，沒有則 fallback 用卡片圖
        if (characterPortraitImage != null)
        {
            var portrait = Resources.Load<Sprite>($"RunePortraits/{ability.id}");
            characterPortraitImage.sprite  = portrait != null ? portrait : ability.image;
            characterPortraitImage.enabled = characterPortraitImage.sprite != null;
        }
    }

    public void Hide() => ShowEmpty();

    private void ShowEmpty()
    {
        if (runeNameText != null) runeNameText.text = "← 選擇一個符文";
        if (runeTypeText != null) runeTypeText.text = "";
        if (effectText   != null) effectText.text   = "";
        if (characterPortraitImage != null) characterPortraitImage.enabled = false;
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
