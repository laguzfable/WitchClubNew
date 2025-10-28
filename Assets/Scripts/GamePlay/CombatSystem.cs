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

    [SerializeField] GameObject localEventSystem;
    public bool IsTestMode => localEventSystem.activeSelf;

    void Awake()
    {
        localEventSystem.SetActive(!Engine.Initialized);

        pc = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        combatTxtPanel = GameObject.FindWithTag("Respawn").GetComponent<UICombatTextPanel>();
        envEffect = new EnvironmentEffect(this);

        monsterID = PlayerPrefs.GetString("enemyName");
        bossName.text = monsterID;

        audioSource = gameObject.GetComponent<AudioSource>();
        dialogText = dialogObj.transform.Find("Image/Text").GetComponent<Text>();
        mobUnit = GameObject.FindGameObjectWithTag("Enemy").GetComponent<EnemyUnit>();
        visualResource = GetComponent<CombatVisualResources>();

        Init();
    }

    void Init()
    {
        if (!IsTestMode)
        {
            SwitchStateToCombatMode();

            BG.sprite = visualResource.GetBGByName(DataService.Instance.scriptParameter.background);

            var runeActive = false;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("RuneActive", out runeActive);

            if (TutorialController.isTutorial)
            {
                tutorController = tutorController1;
                tutorController.Begin();
            }
            else if (TutorialController.isTutorial2)
            {
                tutorController = tutorController2;
                tutorController.Begin();
            }
            tutorController1.uICollection.SetRunesEnabled(runeActive);
        }

        BG.gameObject.SetActive(true);

        // ✅ 這裡加上能量開關初始化
        TryEnableEnergySystemFromNaninovel();

        blackMask.DOFade(0f, 0.3f).OnComplete(() =>
        {
            startBattleUI.SetActive(true);
            DOVirtual.DelayedCall(1f, () => startBattleUI.SetActive(false));
        });
    }

    /// <summary>
    /// 從 Naninovel 變數 YellowActive 讀取是否要開啟能量環境
    /// </summary>
    void TryEnableEnergySystemFromNaninovel()
    {
        if (IsTestMode) return;

        bool yellowActive = false;
        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars != null)
        {
            vars.TryGetVariableValue<bool>("YellowActive", out yellowActive);
        }

        if (yellowActive)
        {
            Debug.Log("[CombatSystem] YellowActive detected → enabling Energy environment.");
            envEffect.SetNextEffect(EEnvEffectType.Energy);
            envEffect.SwitchToNextEffect();
        }
        else
        {
            Debug.Log("[CombatSystem] YellowActive = false → normal environment.");
        }
    }

    public bool IsEnergyActive()
    {
        if (!IsTestMode)
        {
            bool yellowActive = true;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("YellowActive", out yellowActive);
            return yellowActive;
        }
        return true;
    }

    void Start()
    {
        // 避免覆蓋 Energy 狀態
        if (envEffect.curType == EEnvEffectType.None)
            envEffect.SetCurrentEffect(EEnvEffectType.None);

        if (!TutorialController.isTutorial && !TutorialController.isTutorial2)
        {
            PrepareBeginTurn();
        }
    }

    public void SwitchStateToCombatMode()
    {
        GameObject.FindObjectOfType<ContinueInputUI>().Visible = false;
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        advCamera.enabled = true;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.KeypadPeriod))
        {
            GameOver(false);
        }
    }

    // ---------------- 以下全是原始 CombatSystem 功能 ---------------- //

    class ResultOrder
    {
        public int value;
        public BaseCombatUnit owner;
        public System.Action action;
    }

    List<ResultOrder> orderList = new List<ResultOrder>();
    enum Order { PlayerDealDamage = 1, InterruptMobsAction, PlayerHealing, MobDealDamage, MobHealing };

    [SerializeField] CGFadeHelper blackScreen;
    [SerializeField] Image comboSpecialImg;

    public async UniTask PlayCardAsync()
    {
        pc.SetControllable(false);

        var playerUnit = pc.GetPlayerUnit();
        var mobActResult = mobUnit.GetActionResult();
        int GetDiceValue(int diceCount)
        {
            var total = 0;
            for (int i = 0; i < diceCount; i++)
            {
                var dice = UnityEngine.Random.Range(1, 7);
                total += dice;
            }
            return total;
        }

        var mobDmg = pc.ATK - mobActResult.attr.DEF;

        orderList.Clear();
        CreateOrder((int)Order.MobDealDamage, mobActResult.attr.ATK, mobUnit, () =>
        {
            if (mobUnit.HasEffect(EAbilityEffectType.BreakAction)) return;
            var dmg = mobActResult.attr.ATK - pc.DEF;
            if (playerUnit.HasEffect(EAbilityEffectType.Shield)) return;
            if (playerUnit.HasEffect(EAbilityEffectType.Reflect))
                mobUnit.ApplyDamage(mobActResult.attr.ATK);
            else
                playerUnit.ApplyDamage(dmg);
        });

        CreateOrder((int)Order.PlayerDealDamage, pc.ATK, playerUnit, () =>
        {
            visualResource.GetCardFX(pc.result.fxID);
            if (mobUnit.HasEffect(EAbilityEffectType.Shield)) return;
            var dealDamage = mobDmg;
            if (envEffect.curType == EEnvEffectType.MobArmor)
                dealDamage = Mathf.FloorToInt((float)dealDamage * 0.7f);
            mobUnit.ApplyDamage(dealDamage);
        });

        foreach (var or in orderList.OrderBy(or => or.value))
        {
            or.action();
            await UniTask.Delay(TimeSpan.FromSeconds(waitTurnTime));
            if (!isContinue) return;
        }

        if (TutorialController.isTutorial || TutorialController.isTutorial2)
        {
            tutorController.canGoNext = true;
            return;
        }

        PrepareBeginTurn();
    }

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

    public void PrepareBeginTurn()
    {
        if (!isContinue) return;
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
        pc.ResetAttr();
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
        // left empty
    }

    public void GameOver(bool isLose)
    {
        Debug.Log($"isLose?: {isLose}");
        if (isLose)
        {
            var go = GameObject.FindGameObjectWithTag("Finish");
            go.transform.Find("Image/Text").GetComponent<Text>().text = isLose ? "太大意惹..." : "贏惹!";
            go.transform.Find("Image").GetComponent<CGFadeHelper>().FadeIn();
        }
        else
        {
            blackMask.DOFade(1f, 0.5f).SetDelay(0.3f).OnComplete(BackToNani);
        }
    }

    public void BackToNani()
    {
        if (IsTestMode)
        {
            ReloadScene();
            return;
        }

        string mobListStr = "";
        Engine.GetService<ICustomVariableManager>().TryGetVariableValue<string>("MobList", out mobListStr);
        mobListStr += DataService.Instance.scriptParameter.combatTarget + ",";
        Engine.GetService<ICustomVariableManager>().SetVariableValue("MobList", mobListStr);

        var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        advCamera.enabled = false;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;
        GameObject.FindObjectOfType<ContinueInputUI>().Visible = true;

        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene("CombatScene");
    }

    public void SpawnCombatText(string content, ECombatTextType type, bool isPlayer) => combatTxtPanel.EnqueueText(content, type, isPlayer);
    public void SpawnSystemText(string content) => combatTxtPanel.DisplaySystemText(content);

    public async UniTaskVoid DisplayBreakCloth(GameObject animPrefab)
    {
        if (animPrefab == null) return;
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        Instantiate(animPrefab);
        CameraPlay.MangaFlash(2.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
        mainCanvas.FadeIn(0.15f);
    }

    public async UniTaskVoid DisplayBreakFinalCloth(GameObject animPrefab)
    {
        isContinue = false;
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        if (animPrefab != null)
        {
            Instantiate(animPrefab);
        }
        CameraPlay.MangaFlash(2.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
        mainCanvas.FadeIn(0.15f);
        GameOver(false);
    }
}
