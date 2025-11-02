using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectRuneCard : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Text nameText;
    public Text descText;
    public Image iconImage;

    private int index;
    private RuneData data;
    private bool unlocked;

    public void Setup(int index, RuneData data, bool unlocked)
    {
        this.index = index;
        this.data = data;
        this.unlocked = unlocked;

        if (nameText) nameText.text = data.runeName;
        if (descText) descText.text = data.desc;
        if (iconImage) iconImage.sprite = data.icon;

        Debug.Log($"[RuneCard] 顯示資料：{data.runeName}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!unlocked)
        {
            Debug.Log($"[RuneCard] ❌ 尚未解鎖 index={index}");
            return;
        }

        Debug.Log($"[RuneCard] ✅ 點擊符文 index={index}");
        FindObjectOfType<RuneUIManager>()?.OnRuneClicked(index);
    }
}
