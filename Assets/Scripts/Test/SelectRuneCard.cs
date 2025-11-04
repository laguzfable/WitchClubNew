using UnityEngine;
using UnityEngine.UI;

public class SelectRuneCard : MonoBehaviour
{
    public string abilityID;
    public int runeIndex; // 在 Inspector 標註符文位置 (0~3 是預設開啟)

    public Image img;
    public Button btn;

    private Ability ability;
    public Text abilityName;
    public Text abilityDesc;

    void Awake()
    {
        ability = DataService.Instance.GetAbilityById(abilityID);

        // 設定圖片與文字
        if (ability.image != null)
            img.sprite = ability.image;
        abilityName.text = ability.name;
        abilityDesc.text = ability.description;

        // ✅ 設定可否點擊（解鎖狀態）
        btn.interactable = IsRuneUnlocked();

        // ✅ 綁定點擊事件
        btn.onClick.AddListener(OnClick);

        // ✅ 已裝備的符文放大顯示
        if (PlayerData.Instance.usingRuneIDs[(int)ability.element] == abilityID)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

bool IsRuneUnlocked()
{
    // ✅ 前四張符文永遠解鎖
    if (runeIndex < 4)
        return true;

    // ✅ 正確的資料來源
    // 這裡不是用 abilityID 查，而是查這個元素目前解鎖的是哪一個
    string key = $"{ability.element}_UnlockedRune";
    string unlockedRune = PlayerPrefs.GetString(key, ""); // ex: "blue04"

    return unlockedRune == abilityID;
}


    public void OnClick()
    {
        if (!btn.interactable) return; // ✅ 避免未解鎖誤點擊

        // ✅ 裝備這顆符文
        PlayerData.Instance.usingRuneIDs[(int)ability.element] = abilityID;

        // ✅ UI 放大效果
        var parent = transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var card = parent.GetChild(i).GetComponent<SelectRuneCard>();
            card.transform.localScale = (card == this)
                ? new Vector3(1.2f, 1.2f, 1.2f)
                : Vector3.one;
        }
    }
}
