using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 休息頁的普通卡型態選擇按鈕，一個 element+variant 組合對應一個實例。
/// 用法與 SelectRuneCard 相同：在 Inspector 裡對每個按鈕設定 element / variant。
/// </summary>
public class SelectCardVariant : MonoBehaviour
{
    public ECardElement element;
    public string variant; // "" = 原版, "A", "B"

    public Image img;
    public Button btn;
    public Text nameText;
    public GameObject selectedFrame; // 選中時顯示的框框

    CardData cardData;

    void Awake()
    {
        int baseId = BaseIdForElement(element);
        int resolvedId = variant == "A" ? baseId * 10 + 1
                        : variant == "B" ? baseId * 10 + 2
                        : baseId;

        // 直接從 Resources 讀取，不依賴場景裡是否有 Player/CardDataCollection
        cardData = Resources.Load<CardData>("DataCollections/CardData_" + resolvedId);

        if (cardData != null)
        {
            if (img != null) img.sprite = cardData.image;
            if (nameText != null) nameText.text = RuneEnTranslation.TranslateName(cardData.displayName);
        }

        // 自動找 SelectedFrame 子物件（不需要手動拖接線）
        if (selectedFrame == null)
            selectedFrame = transform.Find("SelectedFrame")?.gameObject;

        bool isUnlocked = IsUnlocked();
        btn.interactable = isUnlocked;
        btn.onClick.AddListener(OnClick);

        bool isEquipped = PlayerData.Instance.usingCardVariant[(int)element] == variant;
        SetEquippedVisual(isEquipped);
    }

    bool IsUnlocked()
    {
        // 原版永遠可選，不需解鎖
        if (string.IsNullOrEmpty(variant)) return true;

        string key = $"UnlockedCardVariant_{element}";
        string list = PlayerPrefs.GetString(key, "");
        return list.Split(',').Contains(variant);
    }

    void SetEquippedVisual(bool equip)
    {
        transform.localScale = equip ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
        if (selectedFrame != null)
            selectedFrame.SetActive(equip);
    }

    public void OnClick()
    {
        if (!btn.interactable) return;

        // 記錄目前裝備的型態
        PlayerData.Instance.usingCardVariant[(int)element] = variant;
        PlayerPrefs.SetString($"EquippedCardVariant_{element}", variant);
        PlayerPrefs.Save();

        // 只更新「同色」卡片的放大狀態，不要動到其他顏色目前裝備中的卡
        var parent = transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var card = parent.GetChild(i).GetComponent<SelectCardVariant>();
            if (card == null || card.element != element) continue;
            card.SetEquippedVisual(card.variant == variant);
        }
    }

    static int BaseIdForElement(ECardElement element)
    {
        switch (element)
        {
            case ECardElement.Red: return 101;
            case ECardElement.Blue: return 102;
            case ECardElement.Green: return 103;
            case ECardElement.Yellow: return 104;
            default: return -1;
        }
    }
}
