using Kenaz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UniRx.Async;

public enum ECardElement { Blue, Red, Yellow, Green, None }

public class ElementCard : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector]
    public ECardElement element;

    bool isSelected = false;

    BaseCombatUnit unit;

    [SerializeField]
    GameObject info;
    AudioSource audioSource;
    public bool controlable = true;
    [SerializeField]
    AudioClip sfx;
    CombatVisualResources visualResource;

    int id;
    public int level { private set; get; } = 1;

    /*戰鬥教學用*/
    PlayerController pc;

    TextMeshPro atkTxt, defTxt, enTxt, healTxt;

    DataService dataService;// = Toolbox.Instance.GetOrAddComponent<DataService>();//.GetAbilityById(abilityID)

    Vector3 orgPos, orgRot, orgScale;

    [SerializeField]
    Vector3 selectedPos;

    public int ID
    {
        set
        {
            id = value;
            ClearChildren();
            visualResource.GetBaseCard(id, transform);
            ResetLevel();
            UpdateValue();
        }
        get
        {
            return id;
        }
    }

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        visualResource = GameObject.FindGameObjectWithTag("GameController").GetComponent<CombatVisualResources>();
        //Utility.CreateEvent(gameObject, EventTriggerType.PointerClick, OnClick);
        //Utility.CreateEvent(gameObject, EventTriggerType.PointerDown, OnPressDown);
        //Utility.CreateEvent(gameObject, EventTriggerType.PointerUp, OnPressUp);

        pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        unit = pc.GetPlayerUnit();

        dataService = DataService.Instance;//.GetAbilityById(abilityID)

        atkTxt = transform.Find("ATKText").GetComponent<TextMeshPro>();
        defTxt = transform.Find("DEFText").GetComponent<TextMeshPro>();
        enTxt = transform.Find("ENText").GetComponent<TextMeshPro>();
        // enTxt.enabled = pc.combatSystem.IsEnergyActive();
        healTxt = transform.Find("HEALText").GetComponent<TextMeshPro>();

        orgPos = transform.localPosition;
        orgRot = transform.localRotation.eulerAngles;
        orgScale = transform.localScale;
    }

    async UniTaskVoid Start()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f));

        orgPos = transform.localPosition;
        orgRot = transform.localRotation.eulerAngles;
        orgScale = transform.localScale;
    }

    void ResetLevel()
    {
        level = 1;
    }

    public void LevelUp()
    {
        if (level < 5)
        {
            level++;
        }
        UpdateValue();
    }

    void UpdateValue()
    {
        var ability = dataService.GetAbilityById(ID.ToString()).cardAttr[level-1];

        atkTxt.text = ability.ATK.ToString();
        defTxt.text = ability.DEF.ToString();
        enTxt.text = ability.EN.ToString();
        healTxt.text = ability.HEAL.ToString();

        enTxt.enabled = pc.combatSystem.IsEnergyActive() && ability.EN > 0;
        healTxt.enabled = ability.HEAL > 0;
        atkTxt.enabled = !healTxt.enabled;
    }

    public void ChangeBtnEvent(bool isEnabled)
    {
        Utility.EnabledEvent(gameObject, isEnabled);
    }

    void OnPressDown(BaseEventData e)
    {
        if (!controlable)
        {
            return;
        }
    }

    void OnPressUp(BaseEventData e)
    {
        if (!controlable)
        {
            return;
        }
    }

    public void ClearChildren()
    {
        if (transform.childCount > 0)
        {
            Destroy(transform.GetChild(0).gameObject);
        }
    }

    public GameObject DisplayCardEffect(GameObject fx)
    {
        if (fx == null)
        {
            return null;
        }
        ClearChildren();
        var go = Instantiate(fx, transform);
        go.transform.localPosition = new Vector3(0, 0, 0);
        return go;
    }

    private void ShowInfo()
    {
        //info.SetActive(true);
        //info.GetComponent<Button>().onClick.AddListener(HideInfo);
    }

    private void HideInfo()
    {
        //info.GetComponent<Button>().onClick.RemoveListener(HideInfo);
        //info.SetActive(false);
    }

    public void OnClick(BaseEventData e)
    {
        if (!controlable)
        {
            return;
        }
        OnSelect();
    }

    void OnSelect()
    {
        audioSource.clip = sfx;
        audioSource.Play();
        if (unit.controllable)
        {
            SetSelectState(!isSelected);
        }
        pc.CalculateAttr();
    }

    public bool GetSelectState()
    {
        return isSelected;
    }

    readonly Vector3 selectedSize = new Vector3(1.1f, 1.1f, 1.1f);
    readonly float selectDuration = 0.15f;
    public void SetSelectState(bool newState)
    {
        isSelected = newState;

        transform.DOScale(isSelected ? selectedSize : orgScale, selectDuration).SetEase(Ease.OutBack);
        transform.DOLocalRotate(isSelected ? Vector3.zero : orgRot, selectDuration).SetEase(Ease.OutBack);
        transform.DOLocalMoveY(isSelected ? /*orgPos.y + 1f*/selectedPos.y : orgPos.y, selectDuration).SetEase(Ease.OutBack);

        var outline = GetComponentInChildren<SelectOutline>(true);
        if(outline != null)
        {
            outline.gameObject.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!controlable)
        {
            return;
        }
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnSelect();
        }
    }
}
