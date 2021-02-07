using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SelectRuneCard : MonoBehaviour
{
    public string abilityID;

    public Image img;

    public Button btn;

    Ability ability;

    public Text abilityName;
    public Text abilityDesc;

    // Use this for initialization
    void Start()
    {
        ability = Toolbox.Instance.GetOrAddComponent<DataService>().GetAbilityById(abilityID);

        if (ability.image != null)
        {
            img.sprite = ability.image;
        }
        if (ability.requireEnergy <= 0)
        {
            // not avaliable yet;
            btn.interactable = false;
        }
        abilityName.text = ability.name;
        abilityDesc.text = ability.description;

        btn.onClick.AddListener(OnClick);

        if(PlayerData.Instance.usingRuneIDs[(int)ability.element] == abilityID)
        {
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }

    public void OnClick()
    {
        PlayerData.Instance.usingRuneIDs[(int)ability.element] = abilityID;
        var parent = transform.parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).localScale = parent.GetChild(i).GetComponent<SelectRuneCard>() == this ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
        }
    }
}
