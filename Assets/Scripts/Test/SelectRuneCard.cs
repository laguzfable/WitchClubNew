using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class SelectRuneCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string abilityID;
    public int runeIndex; // 僅用於 UI 排序，不參與解鎖判斷

    public Image img;
    public Button btn;

    private Ability ability;
    public Text abilityName;
    public Text abilityDesc;

    private void Awake()
    {
        Debug.Log($"=== [RuneCard Awake] abilityID:{abilityID} ===");

        ability = DataService.Instance.GetAbilityById(abilityID);

        // ✅ 圖片避免被清成 null
        if (ability.image != null)
            img.sprite = ability.image;
        else
            Debug.LogWarning($"[RuneCard] {abilityID} 沒有指定圖片!");

        abilityName.text = RuneEnTranslation.TranslateName(ability.name);
        abilityDesc.text = RuneEnTranslation.TranslateDesc(ability.description);

        // 效果說明已移至右側 DetailPanel，小卡上不顯示
        if (abilityDesc != null)
            abilityDesc.gameObject.SetActive(false);

        // ✅ 解鎖狀態
        bool isUnlocked = IsRuneUnlocked();
        btn.interactable = isUnlocked;

        btn.onClick.AddListener(OnClick);

        // ✅ 已裝備放大顯示
        bool isEquipped = PlayerData.Instance.usingRuneIDs[(int)ability.element] == abilityID;
        SetEquippedVisual(isEquipped);

        // ✅ Debug
        DebugLog();
    }

bool IsRuneUnlocked()
{
    // 預設符文（xx00）永遠可選，不需解鎖
    if (abilityID.EndsWith("00")) return true;

    string key = $"UnlockedRunes_{ability.element}";
    string list = PlayerPrefs.GetString(key, "");
    return list.Split(',').Contains(abilityID);
}


    void SetEquippedVisual(bool equip)
    {
        transform.localScale = equip ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
    }

public void OnClick()
{
    if (!btn.interactable) return;

    // ✅ 記錄目前裝備
    PlayerData.Instance.usingRuneIDs[(int)ability.element] = abilityID;
    PlayerPrefs.SetString($"Equipped_{ability.element}", abilityID);
    PlayerPrefs.Save();

    // ✅ 更新 UI 放大
    var parent = transform.parent;
    for (int i = 0; i < parent.childCount; i++)
    {
        var card = parent.GetChild(i).GetComponent<SelectRuneCard>();
        bool equip = card.ability.element == ability.element &&
                     card.abilityID == abilityID;
        card.SetEquippedVisual(equip);
    }

    // ✅ 核心 Fix：戰鬥符文立即更新
    foreach (var w in FindObjectsOfType<UIWitchAbility>())
        w.RefreshRune();
}


    // ── Hover → 顯示詳情 ────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        RuneDetailPanel.Instance?.Show(ability);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RuneDetailPanel.Instance?.Hide();
    }

    private void DebugLog()
    {
        string key = $"{ability.element}_UnlockedRune";
        string unlockedRune = PlayerPrefs.GetString(key, "");

        Debug.Log($"[RuneCard] abilityID:{abilityID} | element:{ability.element} | unlocked:'{unlockedRune}' | using:'{PlayerData.Instance.usingRuneIDs[(int)ability.element]}'");
    }
}
