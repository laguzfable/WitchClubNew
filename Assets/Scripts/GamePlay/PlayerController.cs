using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UniRx.Async;

public class PlayerController : MonoBehaviour
{

    static readonly int MaxCardCount = 5;
    static readonly int MaxRuneAbilityCount = 4;

    public CombatSystem combatSystem { private set; get; }

    PlayerUnit playerUnit;

    [SerializeField]
    ElementCard[] cards = new ElementCard[MaxCardCount];

    [SerializeField]
    UIWitchAbility[] witchCards = new UIWitchAbility[MaxRuneAbilityCount];

    AudioSource audioSource;

    public Button playBtn;

    [SerializeField]
    AudioClip[] sfx;

    [SerializeField]
    TextMeshProUGUI[] textArr;

    DataService dataService;


    /*
    1 R
    2 B
    4 G
    8 Y

    1+2 = 3
    1+4 = 5
    1+8 = 9
    2+4 = 6
    2+8 = 10
    4+8 = 12

    1+2+4 = 7
    1+2+8 = 11
    1+4+8 = 13
    2+4+8 = 14

    1+2+4+8 = 15
    */
    [SerializeField]
    Sprite[] combinationCGArr = new Sprite[16];// or amimation clip


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        //playerUnit = GetComponent<PlayerUnit>();
        combatSystem = GameObject.FindWithTag("GameController").GetComponent<CombatSystem>();
        dataService = DataService.Instance;

        playBtn.onClick.AddListener(PlayAction);
        MoveBaseCards(false);
    }

    // Use this for initialization
    void Start()
    {
        ReflashCards(false);
    }

    Transform cardContriner = null;
    [SerializeField]
    Transform downPos, upPos;

    public void MoveBaseCards(bool isDown)
    {
        if (null == cardContriner)
        {
            cardContriner = cards[0].transform.parent;
        }

        cardContriner.DOMoveY(isDown ? downPos.position.y : upPos.position.y, 0.2f);
    }

    public PlayerUnit GetPlayerUnit()
    {
        if (null == playerUnit)
        {
            playerUnit = GetComponent<PlayerUnit>();
        }
        return playerUnit;
    }

    public void PlayAction()
    {
        // 這邊插入教學的邏輯
        if (TutorialController.isTutorial)
        {
            var tutorObj = combatSystem.tutorController.curTutorialObj;

            var isEmptyCards = result == null || result.cardList.Count == 0;

            // 檢查id
            switch (tutorObj.customActionID)
            {
                // 透過事件id判斷教學條件是否達成
                case "playRed":
                case "playRed2":
                    {
                        if (isEmptyCards)
                        {
                            combatSystem.SpawnSystemText("請依教學指示執行動作");
                            return;
                        }
                        foreach (var card in result.cardList)
                        {
                            if (card.element != ECardElement.Red)
                            {
                                combatSystem.SpawnSystemText("請依教學指示執行動作");
                                return;
                            }
                        }
                    }
                    break;
                case "playBlue":
                    {
                        if (isEmptyCards)
                        {
                            combatSystem.SpawnSystemText("請依教學指示執行動作");
                            return;
                        }
                        foreach (var card in result.cardList)
                        {
                            if (card.element != ECardElement.Blue)
                            {
                                combatSystem.SpawnSystemText("請依教學指示執行動作");
                                return;
                            }
                        }
                    }
                    break;
                case "playGreen":
                    {
                        if (isEmptyCards)
                        {
                            combatSystem.SpawnSystemText("請依教學指示執行動作");
                            return;
                        }
                        foreach (var card in result.cardList)
                        {
                            if (card.element != ECardElement.Green)
                            {
                                combatSystem.SpawnSystemText("請依教學指示執行動作");
                                return;
                            }
                        }
                    }
                    break;
                default:
                    // combatSystem.SpawnSystemText("請依教學指示執行動作");
                    return;

            }
        }

        if (result != null)
        {
            if (result.state == EElementState.Multiple && !playerUnit.HasEffect(EAbilityEffectType.IgnoreElement))
            {
                combatSystem.SpawnSystemText("不能選擇不同系列生物");
                return;
            }
            var envEff = combatSystem.envEffect;
            if (envEff.curType == EEnvEffectType.LimitCards && result.cardList.Count > 2)
            {
                combatSystem.SpawnSystemText("無法出超過2張卡片");
                return;
            }

            int comboSprIndex = 0;

            foreach (var card in result.cardList)
            {
                comboSprIndex += card.ID;
                if (!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect))
                {
                    if (envEff.curType == EEnvEffectType.NoCharacter && card.ID < 100)
                    {
                        combatSystem.SpawnSystemText("無法出角色卡");
                        return;
                    }
                    if (envEff.curType == EEnvEffectType.RedSilence && card.element == ECardElement.Red)
                    {
                        combatSystem.SpawnSystemText("無法出紅色卡");
                        return;
                    }
                    if (envEff.curType == EEnvEffectType.BlueSilence && card.element == ECardElement.Blue)
                    {
                        combatSystem.SpawnSystemText("無法出藍色卡");
                        return;
                    }
                    if (envEff.curType == EEnvEffectType.GreenSilence && card.element == ECardElement.Green)
                    {
                        combatSystem.SpawnSystemText("無法出綠色卡");
                        return;
                    }
                    if (envEff.curType == EEnvEffectType.YellowSilence && card.element == ECardElement.Yellow)
                    {
                        combatSystem.SpawnSystemText("無法出黃色卡");
                        return;
                    }
                }
            }

            AddEN(cardAttr.EN);
        }

        /*
        if(result?.state == EElementState.Combination)
        {
            var targetCG = combinationCGArr[comboSprIndex];
            //do something
        }
        */

        audioSource.clip = sfx[0];
        audioSource.Play();
        combatSystem.PlayCardAsync().Forget();
    }

    public void SetControllable(bool controllable)
    {
        playBtn.interactable = controllable;
        //開trigger
        foreach (ElementCard card in cards)
        {
            card.controlable = controllable;
            //card.ChangeBtnEvent(controllable);
        }
        foreach (var witchCard in witchCards)
        {
            witchCard.isControllable = controllable;
        }
    }

    public void AddEN(float EN)
    {
        foreach (var witchCard in witchCards)
        {
            witchCard.cost.Value += EN;
        }
    }

    public void CostEN(float EN)
    {
        foreach (var witchCard in witchCards)
        {
            witchCard.cost.Value -= EN;
        }
    }

    public int ATK
    {
        get
        {
            return cardAttr.ATK;
        }
    }

    public int DEF
    {
        get
        {
            return cardAttr.DEF;
        }
    }

    public int HEAL
    {
        get
        {
            return cardAttr.HEAL;
        }
    }

    public int EN
    {
        get
        {
            return cardAttr.EN;
        }
    }

    CardAttribute cardAttr = new CardAttribute();
    public PlayedCardResult result { private set; get; }

    public void ResetAttr()
    {
        cardAttr.Init();
        playerUnit.ClearEffect();
        for (int i = 0; i < AttributeIndex.Length; i++)
        {
            //attrArr[i] = 0;
            textArr[i].text = "0";
        }

        result = null;
    }

    public void CalculateAttr()
    {
        result = GetPlayedResult();
        cardAttr = playerUnit.bonusAttr;

        foreach (var card in result.cardList)
        {
            var ability = dataService.GetAbilityById(card.ID.ToString());
            cardAttr.ATK += ability.cardAttr[card.level - 1].ATK;
            cardAttr.DEF += ability.cardAttr[card.level - 1].DEF;
            cardAttr.HEAL += ability.cardAttr[card.level - 1].HEAL;
            cardAttr.EN += ability.cardAttr[card.level - 1].EN;
        }
        if (result.state == EElementState.Combination)
        {
            cardAttr.ATK *= result.cardList.Count;
            cardAttr.DEF *= result.cardList.Count;
            cardAttr.HEAL *= result.cardList.Count;
            cardAttr.EN *= result.cardList.Count;
        }
        if (!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect))
        {
            var envEff = combatSystem.envEffect;
            switch (envEff.curType)
            {
                case EEnvEffectType.Attack:
                    {
                        cardAttr.ATK = cardAttr.ATK * 2;
                    }
                    break;
                case EEnvEffectType.Defense:
                    {
                        cardAttr.DEF = cardAttr.DEF * 2;
                    }
                    break;
                case EEnvEffectType.Heal:
                    {
                        cardAttr.HEAL = cardAttr.HEAL * 2;
                    }
                    break;
                case EEnvEffectType.Energy:
                    {
                        cardAttr.EN = cardAttr.EN * 2;
                    }
                    break;
                case EEnvEffectType.NoHeal:
                    {
                        cardAttr.HEAL = 0;
                    }
                    break;
                case EEnvEffectType.NoDefense:
                    {
                        cardAttr.DEF = 0;
                    }
                    break;
            }
        }
        if(playerUnit.HasEffect(EAbilityEffectType.NoArmor))
        {
            cardAttr.DEF = 0;
        }

        textArr[0].text = cardAttr.ATK.ToString();
        textArr[1].text = cardAttr.DEF.ToString();
        textArr[2].text = cardAttr.HEAL.ToString();
        textArr[3].text = cardAttr.EN.ToString();
    }

    bool HasCharacterCard(int id)
    {
        foreach(var card in cards)
        {
            if(/*!card.GetSelectState() &&*/card.ID == id)
            {
                return true;
            }
        }
        return false;
    }

    readonly int[] cardIDArr = new int[] { 2, 1, 4, 8, 102, 101, 103, 104 };
    //readonly float[] getCharacterCardChanceArr = new float[] { 0.15f, 0.2f, 0.25f };
    readonly float getCharacterCardChance = 0.15f;

    int DrawRandomCard(AbilityEffectRef effectRef)
    {
        int rndElementIndex = Random.Range(0, 4);
        if (effectRef != null)
        {
            rndElementIndex = effectRef.effect.GetValue();
        }
        if(HasCharacterCard(cardIDArr[rndElementIndex]))
        {
            return cardIDArr[rndElementIndex + 4];
        }
        else
        {
            return Random.Range(0f, 1f) <= /*getCharacterCardChanceArr[chanceIndex]*/getCharacterCardChance ? cardIDArr[rndElementIndex] : cardIDArr[rndElementIndex + 4];
        }
    }

    public void ResetCardsSelectState()
    {
        foreach (var card in cards)
        {
            card.SetSelectState(false);
        }
    }

    public void CardLevelUp()
    {
        foreach (var card in cards)
        {
            card.LevelUp();
        }
    }

    public void ReflashCards(bool isOnlySelected)
    {
        // 這裡塞教學用指定的卡
        if(TutorialController.isTutorial)
        {
            int[] cardArr = null;

            var tutorObj = combatSystem.tutorController.curTutorialObj;
            if(tutorObj != null)
            {
                // 檢查id
                switch(tutorObj.customActionID)
                {
                // 透過事件id判斷教學條件是否達成
                    case "playRed":
                        {
                            cardArr = new int[]{101, 102, 101, 101, 103};
                        }
                        break;
                    case "playRed2":
                        {
                            cardArr = new int[]{102, 101, 101, 102, 101};
                        }
                        break;
                    case "playBlue":
                        {
                            cardArr = new int[]{102, 102, 103, 102, 103};
                        }
                        break;
                    case "playGreen":
                        {
                            cardArr = new int[]{103, 101, 103, 102, 103};
                        }
                        break;
                }
            }
            if(cardArr == null)
            {
                cardArr = new int[]{101, 102, 101, 101, 103};
            }
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if(card.ID == cardArr[i])
                {
                    card.LevelUp();
                }
                else
                {
                    card.ID = cardArr[i];
                }
                card.element = GetCardElement(card.ID);
                card.SetSelectState(false);
            }
            return;
        }
        foreach (var card in cards)
        {
            if (isOnlySelected)
            {
                if (card.GetSelectState())
                {
                    //usedCard.Add(card.ID);
                    card.ID = DrawRandomCard(null);// DrawCard();
                    card.element = GetCardElement(card.ID);
                }
                else
                {
                    card.LevelUp();
                }
            }
            else  //牌堆重洗
            {
                var effectRef = playerUnit.GetEffect(EAbilityEffectType.Shuffle);
                if (effectRef != null && card.element == (ECardElement)effectRef.effect.GetValue())
                {
                    continue;
                }
                card.ID = DrawRandomCard(effectRef);// DrawCard();
                card.element = GetCardElement(card.ID);
            }
            card.SetSelectState(false);
        }
    }

    ECardElement GetCardElement(int id)
    {
        if(1 == id || 101 == id)
        {
            return ECardElement.Red;
        }
        if (2 == id || 102 == id)
        {
            return ECardElement.Blue;
        }
        if (4 == id || 103 == id)
        {
            return ECardElement.Green;
        }
        if (8 == id || 104 == id)
        {
            return ECardElement.Yellow;
        }
        return ECardElement.None;
    }

    PlayedCardResult GetPlayedResult()
    {
        PlayedCardResult result = new PlayedCardResult();

        bool isCombination = true;
        bool isSameElement = true;
        bool isSelectAny = false;
        ECardElement firstEle = ECardElement.None;

        int fxID = 10000;

        foreach (var card in cards)
        {
            if (card.GetSelectState())
            {
                result.cardList.Add(card);
                if(card.ID > 100)
                {
                    isCombination = false;
                }
                if(firstEle != ECardElement.None && card.element != firstEle)
                {
                    isSameElement = false;
                }
                else
                {
                    firstEle = card.element;
                }
                isSelectAny = true;

                if(card.ID < fxID)
                {
                    fxID = card.ID;
                }
            }
        }

        result.fxID = fxID;

        if(isSelectAny)
        {
            if (isCombination)
            {
                result.state = EElementState.Combination;
            }
            else if (isSameElement)
            {
                result.state = EElementState.Single;
            }
            else
            {
                result.state = EElementState.Multiple;
            }
        }

        return result;
    }

    public Sprite GetCompboSpr()
    {
        return combinationCGArr[result.GetComboID()-1];
    }
}

public enum EElementState { Single, Multiple, Combination, None }

public class PlayedCardResult
{
    public EElementState state = EElementState.None;
    public List<ElementCard> cardList = new List<ElementCard>();
    public int fxID;

    public int GetComboID()
    {
        int comboID = 0;
        foreach (var card in cardList)
        {
            comboID += card.ID;
        }
        return comboID;
    }

    public bool IsCombo()
    {
        int comboID = GetComboID();
        return comboID > 0 && comboID < 16 && comboID != 1 && comboID != 2 && comboID != 4 && comboID != 8;
    }
}

// Don't wanna cast enum to int
public static class AttributeIndex
{
    public readonly static int ATK = 0;
    public readonly static int DEF = 1;
    public readonly static int HEAL = 2;
    public readonly static int EN = 3;
    public readonly static int Length = 4;
}