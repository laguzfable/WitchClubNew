using Kenaz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UniRx.Async;
using System.Threading;
using System;

public enum ECardElement { Blue, Red, Yellow, Green, None }

public class ElementCard : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector]
    public ECardElement element;

    bool isSelected = false;

    BaseCombatUnit unit;

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

    // GameObject cardInst;

    [SerializeField] GameObject outline;
    [SerializeField] SpriteRenderer cardPic;
    [SerializeField] TextMeshPro nameTxt;

    public CardData cardData { private set; get; }    

    Color orgOutlineColor;

    public int ID
    {
        set
        {
            id = value;
            // ClearChildren();
            // cardInst = visualResource.GetBaseCard(id, transform);
            cardData = pc.cardDataCollection.cardDict[id];
            cardPic.sprite = cardData.image;
            nameTxt.text = cardData.displayName;
            
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
        orgOutlineColor = outline.GetComponent<SpriteRenderer>().color;
    }

    async UniTaskVoid Start()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f));

        orgPos = transform.localPosition;
        orgRot = transform.localRotation.eulerAngles;
        orgScale = transform.localScale;
        orgOutlineColor = outline.GetComponent<SpriteRenderer>().color;
    }

    void ResetLevel()
    {
        level = 1;
    }

    public void LevelUp()
    {
        // Debug.LogWarning("Level Up ???");
        if (level < 5)
        {
            level++;
        }
        UpdateValue();
    }

    void UpdateValue()
    {
        // var ability = dataService.GetAbilityById(ID.ToString()).cardAttr[level-1];

        var cardAttr = curAttr;

        atkTxt.text = cardAttr.ATK.ToString();
        defTxt.text = cardAttr.DEF.ToString();
        enTxt.text = cardAttr.EN.ToString();
        healTxt.text = cardAttr.HEAL.ToString();

        enTxt.enabled = pc.combatSystem.IsEnergyActive() && cardAttr.EN > 0;
        healTxt.enabled = cardAttr.HEAL > 0;
        atkTxt.enabled = !healTxt.enabled;
    }

    public CardAttribute curAttr => cardData.cardAttr[level-1];

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
        StopFlashOutline();
        audioSource.clip = sfx;
        audioSource.Play();
        if (unit.controllable)
        {
            SetSelectState(!isSelected);
        }
        pc.CalculateAttr();
        pc.CheckSelectable(isSelected);
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

    public void CheckIsAvaliable(EEnvEffectType type)
    {
        // var cardPic = cardInst.transform.Find("CardPic").GetComponent<SpriteRenderer>();
        // Debug.Log($"cardPic? {cardPic}");

        if(type == EEnvEffectType.RedSilence && (id == 1 || id == 101))
        {
            cardPic.color = Color.gray;
        }
        else if(type == EEnvEffectType.BlueSilence && (id == 2 || id == 102))
        {
            cardPic.color = Color.gray;
        }
        else if(type == EEnvEffectType.GreenSilence && (id == 4 || id == 103))
        {
            cardPic.color = Color.gray;
        }
        else if(type == EEnvEffectType.YellowSilence && (id == 8 || id == 104))
        {
            cardPic.color = Color.gray;
        }
        else if(type == EEnvEffectType.NoCharacter && id < 10)// 角色卡最多到8
        {
            cardPic.color = Color.gray;
        }
        else
        {
            cardPic.color = Color.white;
        }
    }

    public bool IsCharacter()
    {
        return ID < 10;
    }

    public void CheckSelectable(ECardElement ele, bool canCombo, PlayedCardResult result, EEnvEffectType type)
    {
        var outline = GetComponentInChildren<SelectOutline>(true);
        var sprRend = outline.GetComponent<SpriteRenderer>();
        StopFlashOutline();
        if(type == EEnvEffectType.LimitCards && result.cardList.Count >= 2)
        {
            cardPic.color = Color.gray;
        }
        else if((element == ele && result.state == EElementState.Single) || pc.GetPlayerUnit().HasEffect(EAbilityEffectType.IgnoreElement))
        {
            cardPic.color = Color.white;
        }
        else if(canCombo && IsCharacter() && (result.state != EElementState.Multiple))
        {
            cardPic.color = Color.white;
            if(isSelected)
            {
                return;
            }
            // Debug.Log("發光");
            sprRend.color = endColor;
            nextColor = orgOutlineColor;
            outline.gameObject.SetActive(true);
            InvokeRepeating(nameof(FlashOutline), 0f, 1.5f);
            // outline.GetComponent<SpriteRenderer>().color
        }
        else
        {
            cardPic.color = Color.gray;
        }
    }

    Color endColor = new Color(0, 0, 0, 0);
    Color nextColor;
    Tween curTween;

    void FlashOutline()
    {
        var sprRend = outline.GetComponent<SpriteRenderer>();
        curTween = sprRend.DOColor(nextColor, 1f);
        nextColor = nextColor == endColor ? orgOutlineColor : endColor;
    }

    public void StopFlashOutline()
    {
        CancelInvoke(nameof(FlashOutline));
        if(curTween != null)
        {
            curTween.Kill();
        }
        var outline = GetComponentInChildren<SelectOutline>(true);
        if(outline != null)
        {
            outline.gameObject.SetActive(isSelected);
        }
        var sprRend = outline.GetComponent<SpriteRenderer>();
        sprRend.color = orgOutlineColor;
    }
}
