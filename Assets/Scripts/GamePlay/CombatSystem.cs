using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Linq;
using Naninovel;
using Naninovel.UI;
using System;

public class CombatSystem : MonoBehaviour
{
    PlayerController pc;

    public EnvironmentEffect envEffect { private set; get; }

    UICombatTextPanel combatTxtPanel;

    public bool isContinue = true;

    public GameObject startBattleUI;

    public GameObject dialogObj;

    public GameObject enemyTurnUI;

    public GameObject playerTurnUI;

    Text dialogText;

    Button dialogBtn;

    readonly float waitBattleTime = 3f;

    [SerializeField] float waitTurnTime = 0.5f;

    string monsterID;

    bool halfHpBlean = false;

    bool checkHalfHpBlean = false;

    bool zeroHpBlean = false;

    //demo用
    [SerializeField] Text bossName;
    
    int nowBossInt = 0;

    [SerializeField] GameObject winGetItem;

    [SerializeField] GameObject[] endObj;

    GameObject currentEnemy;

    [SerializeField] GameObject fadeOutObj;

    AudioSource audioSource;

    [SerializeField] GameObject helpInfoCanvas;

    [SerializeField] CGFadeHelper mainCanvas;

    public bool isPlayerTurn { private set; get; }

    EnemyUnit mobUnit;

    public CombatVisualResources visualResource { private set; get; }

    [SerializeField] SpriteRenderer BG;


    [SerializeField] RawImage blackMask;

    public TutorialController tutorController { private set; get; }
    [SerializeField] TutorialController tutorController1;
    [SerializeField] TutorialController tutorController2;


    // public async UniTask SwitchStateToCombatModeAsync(CancellationToken cancellationToken = default)
    public void SwitchStateToCombatMode()
    {
        // 1. Disable Naninovel input.
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = false;

        GameObject.FindObjectOfType<ContinueInputUI>().Visible = false;

        // 2. Stop script player.
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. Reset state. // 這一條指令會使整個nani重設回初始狀態 而我們只是想暫停而已
        //var stateManager = Engine.GetService<IStateManager>();
        //await stateManager.ResetStateAsync();

        
        // 4. Switch cameras.
        var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        advCamera.enabled = true;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }

    [SerializeField]
    GameObject localEventSystem;
    public bool IsTestMode => localEventSystem.activeSelf;
    void Awake()
    {
        localEventSystem.SetActive(!Engine.Initialized);

        pc = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        combatTxtPanel = GameObject.FindWithTag("Respawn").GetComponent<UICombatTextPanel>();
        
        // envEffect = GetComponent<EnvironmentEffect>();
        envEffect = new EnvironmentEffect(this);

        monsterID = PlayerPrefs.GetString("enemyName");
        bossName.text = monsterID;
        
        audioSource = gameObject.GetComponent<AudioSource>();

        dialogText = dialogObj.transform.Find("Image/Text").GetComponent<Text>();
        mobUnit = GameObject.FindGameObjectWithTag("Enemy").GetComponent<EnemyUnit>();
        visualResource = GetComponent<CombatVisualResources>();

        //GetComponent<CombatUICollection>().SetRunesEnabled(false);
        
        Init();
    }

    void Init()
    {
        if(!IsTestMode)
        {
            SwitchStateToCombatMode();

            Debug.Log($"Toolbox.Instance.GetOrAddComponent<DataService>().paramArr[0] : {DataService.Instance.scriptParameter.background}");
            BG.sprite = visualResource.GetBGByName(DataService.Instance.scriptParameter.background);

            var runeActive = false;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("RuneActive", out runeActive);

            if(TutorialController.isTutorial)
            {
                tutorController = tutorController1;
                tutorController.Begin();
            }
            else if(TutorialController.isTutorial2)
            {
                tutorController = tutorController2;
                tutorController.Begin();
            }
            tutorController1.uICollection.SetRunesEnabled(runeActive);

            Debug.Log(Engine.GetService<ILocalizationManager>().SelectedLocale);
        }
        BG.gameObject.SetActive(true);

        blackMask.DOFade(0f, 0.3f).OnComplete(() =>
        {
            startBattleUI.SetActive(true);
            DOVirtual.DelayedCall(1f, () => startBattleUI.SetActive(false));
            //StartCoroutine(CloseStartBattleUI());
        });

    }

    public bool IsEnergyActive()
    {
        if(!IsTestMode)
        {
            bool yellowActive = true;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("YellowActive", out yellowActive);
            return yellowActive;
        }
        return true;
    }

    /*
    IEnumerator CloseStartBattleUI()
    {
        yield return new WaitForSeconds(1f);
        startBattleUI.SetActive(false);
    }*/

    void Start()
    {
        envEffect.SetCurrentEffect(EEnvEffectType.None);
        if(!TutorialController.isTutorial && !TutorialController.isTutorial2)
        {
            PrepareBeginTurn();
        }
    }

    private void Update()
    {
        // TODO: 正式版要拿掉
        if(Input.GetKeyUp(KeyCode.KeypadPeriod))
        {
            GameOver(false);
        }
    }

    public async UniTask PlayCardAsync()
    {
        //StartCoroutine(PlayResult());

        pc.SetControllable(false);

        var playerUnit = pc.GetPlayerUnit();
        var mobActResult = mobUnit.GetActionResult();

        int GetDiceValue(int diceCount)
        {
            var total = 0;
            for(int i = 0; i < diceCount; i++)
            {
                var dice = UnityEngine.Random.Range(1, 7);// 1 ~ 6
                total += dice;
            }
            return total;
        }


        var mobDmg = pc.ATK - mobActResult.attr.DEF;

        orderList.Clear();
        // if (!isBreakAciton)
        // {
            CreateOrder((int)Order.MobDealDamage, mobActResult.attr.ATK, mobUnit, () => { // Mob deal damage

                if(mobUnit.HasEffect(EAbilityEffectType.BreakAction))
                {
                    return;
                }
                var dmg = mobActResult.attr.ATK - pc.DEF;

                if (playerUnit.HasEffect(EAbilityEffectType.Shield)) // shield effect
                {
                    return;
                }
                if (playerUnit.HasEffect(EAbilityEffectType.Reflect)) // reflect effect
                {
                    mobUnit.ApplyDamage(mobActResult.attr.ATK);
                }
                else // normal
                {
                    playerUnit.ApplyDamage(dmg);
                }

                if (mobUnit.HasEffect(EAbilityEffectType.LifeSteal) && (!mobUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType != EEnvEffectType.NoHeal) && dmg > 0)// life steal
                {
                    mobUnit.ApplyHealing(!mobUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType == EEnvEffectType.Heal ? dmg * 2f : dmg);
                }

            });

            CreateOrder((int)Order.MobHealing, mobActResult.attr.HEAL, mobUnit, () => { // Mob deal healing

                if(mobUnit.HasEffect(EAbilityEffectType.BreakAction))
                {
                    return;
                }
                
                var heal = mobActResult.attr.HEAL;

                mobUnit.ApplyHealing(heal);
                if (mobUnit.HasEffect(EAbilityEffectType.HealingAttack)) // healing attack
                {
                    playerUnit.ApplyDamage(heal);
                }

            });
        // }

        CreateOrder((int)Order.PlayerDealDamage, pc.ATK, playerUnit, () => { // player deal damage
            visualResource.GetCardFX(pc.result.fxID);

            if (mobUnit.HasEffect(EAbilityEffectType.Shield)) // shield effect
            {
                return;
            }
            if (mobUnit.HasEffect(EAbilityEffectType.Reflect)) // reflect effect
            {
                playerUnit.ApplyDamage(mobActResult.attr.ATK);
            }
            else // normal
            {
                var dealDamage = mobDmg;
                if(envEffect.curType == EEnvEffectType.MobArmor/* || mobUnit.HasEffect(EAbilityEffectType.MagicArmor) || mobUnit.HasEffect(EAbilityEffectType.MagicArmorEX)*/)
                {
                    dealDamage = Mathf.FloorToInt((float)dealDamage * 0.7f);
                }
                mobUnit.ApplyDamage(dealDamage);
                mobUnit.CheckCostMagicArmor(pc.result.IsCombo()? ECardElement.None : pc.result.cardList[0].element, pc.result.cardList.Count);
            }

            if (playerUnit.HasEffect(EAbilityEffectType.LifeSteal) && (!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType != EEnvEffectType.NoHeal) && mobDmg > 0) // life steal
            {
                playerUnit.ApplyHealing(!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType == EEnvEffectType.Heal ? mobDmg * 2f : mobDmg);
            }
        });

        CreateOrder((int)Order.InterruptMobsAction, /*isBreakAciton ? 1 : 0*/1, playerUnit, () => { // interrupt
            if (mobUnit.HasEffect(EAbilityEffectType.BreakAction))
            {
                SpawnCombatText("INTERUPT", ECombatTextType.Debuff, false);
            }
            
        });

        CreateOrder((int)Order.PlayerHealing, pc.HEAL, playerUnit, () => { // player deal healing

            var heal = pc.HEAL;

            playerUnit.ApplyHealing(heal);
            if (playerUnit.HasEffect(EAbilityEffectType.HealingAttack)) // healing attack
            {
                mobUnit.ApplyDamage(heal);
            }
            if(!pc.result.IsCombo())
            {
                mobUnit.CheckCostMagicArmor(pc.result.cardList[0].element, pc.result.cardList.Count);
            }
            
        });

        if (pc.result != null && pc.result.IsCombo())
        {
            CameraPlay.Shockwave(0.9f, 0.5f, 1.25f, 2f);
            CameraPlay.WidescreenH_ON(0.2f);
            blackScreen.FadeIn(0.15f);
            comboSpecialImg.sprite = pc.GetCompboSpr();

            comboSpecialImg.gameObject.SetActive(true);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            comboSpecialImg.gameObject.SetActive(false);

            blackScreen.FadeOut(0.15f);
            CameraPlay.WidescreenH_OFF(0.2f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        }

        foreach (var or in orderList.OrderBy(or => or.value)/*orderList.OrderByDescending(or => or.value)*/)
        {
            or.action();
            await UniTask.Delay(TimeSpan.FromSeconds(waitTurnTime));
            if (!isContinue)
            {
                return;
            }
        }

        // if (mobActResult.targetEnvEffect != EEnvEffectType.None && !isBreakAciton)
        // {
        //     envEffect.SetNextEffect(mobActResult.targetEnvEffect, 1);
        // }

        if (TutorialController.isTutorial || TutorialController.isTutorial2)
        {
            tutorController.canGoNext = true;
            return;
        }
        PrepareBeginTurn();
    }

    class ResultOrder
    {
        public int value;
        public BaseCombatUnit owner;
        public System.Action action;
    }

    List<ResultOrder> orderList = new List<ResultOrder>();

    //WaitForSeconds waitPlay = null;


    enum Order { PlayerDealDamage = 1, InterruptMobsAction, PlayerHealing, MobDealDamage, MobHealing };

    void CreateOrder(int order, int value, BaseCombatUnit owner, System.Action action)
    {
        if (value > 0 && !owner.HasEffect(EAbilityEffectType.Stun))
        {
            var resultOrder = new ResultOrder();
            resultOrder.value = order;
            resultOrder.owner = owner;
            resultOrder.action = action;
            orderList.Add(resultOrder);
        }
    }

    [SerializeField]
    CGFadeHelper blackScreen;
    [SerializeField]
    Image comboSpecialImg;

    public void PrepareBeginTurn()
    {

        if (!isContinue)
        {
            return;
        }

        pc.ReflashCards(true);

        if (!TutorialController.isTutorial && !TutorialController.isTutorial2)
        {

            envEffect.remainTurn--;
            if (envEffect.remainTurn <= 0)
            {
                envEffect.SwitchToNextEffect();
                envEffect.GetNextEffect();
            }
        }
        
        var playerUnit = pc.GetPlayerUnit();
        
        playerUnit.BeforeAction();
        mobUnit.BeforeAction();

        MobGetNewAction();
        isPlayerTurn = true;
        // pc.MoveBaseCards(false);
        pc.ResetAttr();
        //mobUnit.ClearEffect();
        playerUnit.controllable = true;
        mobUnit.controllable = false;
        pc.SetControllable(true);
    }

    public void MobGetNewAction()
    {
        mobUnit.GetNewAction();
        UpdateMobActionInfo(mobUnit.GetActionString());
    }

    public void UpdateMobActionInfo(string actionStr)
    {
        // mobActTxt.text = actionStr;
    }

    async UniTaskVoid DisplayDialog(string str)
    {
        dialogText.text = str;
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        dialogObj.SetActive(true);
        await UniTask.Delay(TimeSpan.FromSeconds(4f));
        dialogObj.SetActive(false);
    }

    void RemoveClickDialog(BaseCombatUnit unit)
    {
        dialogObj.SetActive(false);
        dialogBtn.onClick.RemoveListener(() => RemoveClickDialog(unit));
        //NextTurn(unit);
    }

    [SerializeField]
    Text mobActTxt;

    public void GameOver(bool isLose)
    {
        //actTxt.text = "";
        //StartCoroutine(ShowEnd(waitBattleTime, isPlayerVictory));
        //pc.GetPlayerUnit().HP.Restore();
        //mobUnit.HP.Restore();
        Debug.Log($"isLose?: {isLose}");
        if(isLose)
        {
            var go = GameObject.FindGameObjectWithTag("Finish");
            go.transform.Find("Image/Text").GetComponent<Text>().text = isLose ? "太大意惹..." : "贏惹!";
            go.transform.Find("Image").GetComponent<CGFadeHelper>().FadeIn();
        }
        else
        {
            //SwitchStateToNovelModeAsync().ContinueWith(()=> SceneManager.LoadSceneAsync("NaniDialogTest")) .Forget();

            blackMask.DOFade(1f, 0.5f).SetDelay(0.3f).OnComplete(BackToNani);
        }
    }

    public void BackToNani()
    {
        if(IsTestMode)
        {
            ReloadScene();
            return;
        }
        var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        advCamera.enabled = false;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = true;
        GameObject.FindObjectOfType<ContinueInputUI>().Visible = true;
        // Engine.GetService<ICustomVariableManager>().SetVariableValue("PlayerName", PlayerData.Instance.playerName);
        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene("CombatScene");
    }

    public void GotoChangeRuneScene()
    {
        // SceneManager.LoadScene("ChangeRuneScene");
    }

    void ClickEndDialog(bool isVictory)
    {
        if (isVictory)
        {
            endObj[0].SetActive(true);
        }
        else
        {
            endObj[1].SetActive(true);
        }
       // fadeOutObj.SetActive(true);
        dialogObj.SetActive(false);
        dialogBtn.onClick.RemoveListener(() => ClickEndDialog(isVictory));
        audioSource.DOFade(0, 1f);
        ShowEnd(waitBattleTime, isVictory).Forget();
    }

    async UniTaskVoid ShowEnd(float time, bool isPlayerVictory) 
    {
        await UniTask.Delay(TimeSpan.FromSeconds(3f));
        if (!isPlayerVictory)
        {
            endObj[0].SetActive(true);
        }
        else
        {
            endObj[1].SetActive(true);
        }
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        endObj[0].SetActive(false);
        endObj[1].SetActive(false);
        
        Debug.Log("ShowEnd");
        if (isPlayerVictory)
        {
            Debug.Log(" you win ");
        }
        else
        {
            Debug.Log(" you loss ");
        }
    }
    
    public void SpawnCombatText(string content, ECombatTextType type, bool isPlayer) => combatTxtPanel.EnqueueText(content, type, isPlayer);
    public void SpawnSystemText(string content) => combatTxtPanel.DisplaySystemText(content);

    public async UniTaskVoid DisplayBreakCloth(GameObject animPrefab)
    {
        if(animPrefab == null)
        {
            return;
        }
        // pc.MoveBaseCards(true);
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        Instantiate(animPrefab);
        CameraPlay.MangaFlash(2.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
        mainCanvas.FadeIn(0.15f);
        // pc.MoveBaseCards(false);
    }

    public async UniTaskVoid DisplayBreakFinalCloth(GameObject animPrefab)
    {
        isContinue = false;
        // pc.MoveBaseCards(true);
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        if (animPrefab != null)
        {
            Instantiate(animPrefab);
        }
        CameraPlay.MangaFlash(2.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
        mainCanvas.FadeIn(0.15f);
        // pc.MoveBaseCards(false);
        GameOver(false);
    }

}