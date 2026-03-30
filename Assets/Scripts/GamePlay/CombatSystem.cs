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

    Debug.Log($"[Debug] Fight Enemy: {monsterID}");

    // ✅ 放這裡！24行 Init 之後，第一次 GameOver 前讀到正確裝備
    LoadEquippedRunes();
}

private void LoadEquippedRunes()
{
    var names = System.Enum.GetNames(typeof(ECardElement));
    int len = names.Length;

    for (int i = 0; i < len; i++)
    {
        string key = $"Equipped_{names[i]}";
        if (PlayerPrefs.HasKey(key))
        {
            string value = PlayerPrefs.GetString(key);
            PlayerData.Instance.usingRuneIDs[i] = value;
            Debug.Log($"[EquippedLoad] {(ECardElement)i} = {value}");
        }
    }
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

        TryEnableEnergySystemFromNaninovel(); // ✅ 能量啟動改這裡

        blackMask.DOFade(0f, 0.3f).OnComplete(() =>
        {
            startBattleUI.SetActive(true);
            DOVirtual.DelayedCall(1f, () => startBattleUI.SetActive(false));
        });
    }

    /// ✅ 根據這場戰鬥是否設定 YellowActive 來啟用能量
    void TryEnableEnergySystemFromNaninovel()
    {
        if (IsTestMode) return;

        bool yellowActive = false;
        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars != null)
            vars.TryGetVariableValue<bool>("YellowActive", out yellowActive);

        if (yellowActive)
        {
            Debug.Log("[CombatSystem] Enable Energy (YellowActive=true)");
            envEffect.SetNextEffect(EEnvEffectType.Energy);
            envEffect.SwitchToNextEffect();
        }
        else
        {
            envEffect.SetCurrentEffect(EEnvEffectType.None);
        }
    }



    [Header("⚡ Mob → Rune Unlock Table")]
public List<MobRuneUnlockData> mobUnlockTable = new List<MobRuneUnlockData>();

[System.Serializable]
public class MobRuneUnlockData
{
    public string mobID;    // 敵人代號
    public string runeID;   // 對應符文 abilityID
}



    /// ✅ YellowActive 變數是否開啟（黃色能量）
    public bool IsEnergyActive()
    {
        if (!IsTestMode)
        {
            bool yellowActive = false;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<bool>("YellowActive", out yellowActive);
            return yellowActive;
        }
        return false;
    }

    void Start()
    {
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

         var audioManager = Engine.GetService<IAudioManager>();
         audioManager.PlayBgmAsync("你的戰鬥BGM檔名", volume: 1f, fadeTime: 0.5f, loop: true).Forget();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.KeypadPeriod))
        {
            GameOver(false);
        }
    }

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
            total += UnityEngine.Random.Range(1, 7);
        return total;
    }

    var mobDmg = pc.ATK - mobActResult.attr.DEF;

    // 清空 orderList，準備重建這一回合的行動順序
    orderList.Clear();

    // =====================
    // 先處理補血
    // =====================

    // 玩家補血（來自卡片計算出的 HEAL）
    if (pc.HEAL > 0)
    {
        CreateOrder((int)Order.PlayerHealing, pc.HEAL, playerUnit, () =>
        {
            playerUnit.ApplyHealing(pc.HEAL);
        });
    }

    // 敵人補血（如果牠行動有 HEAL）
    if (mobActResult.attr.HEAL > 0)
    {
        CreateOrder((int)Order.MobHealing, mobActResult.attr.HEAL, mobUnit, () =>
        {
            mobUnit.ApplyHealing(mobActResult.attr.HEAL);
        });
    }

    // =====================
    // 再處理攻擊
    // =====================

    // 敵人攻擊玩家
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

    // 玩家攻擊敵人
    CreateOrder((int)Order.PlayerDealDamage, pc.ATK, playerUnit, () =>
    {
        visualResource.GetCardFX(pc.result.fxID);

        if (mobUnit.HasEffect(EAbilityEffectType.Shield)) return;

        var dealDamage = mobDmg;
        if (envEffect.curType == EEnvEffectType.MobArmor)
            dealDamage = Mathf.FloorToInt((float)dealDamage * 0.7f);

        mobUnit.ApplyDamage(dealDamage);
    });

    // =====================
    // 依照 enum 順序執行
    // =====================

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
        // left empty (原邏輯不動)
    }





public void GameOver(bool isLose)
{
    Debug.Log($"isLose?: {isLose}");

    // ✅ 把勝敗結果寫進 Naninovel 變數（給 @if CombatResult 用）
    if (!IsTestMode)
    {
var vars = Engine.GetService<ICustomVariableManager>();
vars.SetVariableValue("CombatWin", isLose ? "False" : "True");



    }

    // ✅ 清洗 monsterID
    monsterID = monsterID.Trim().ToLower();
    Debug.Log($"[RuneUnlock-Check] monsterID cleaned = '{monsterID}' (length={monsterID.Length})");

    // ✅ 只有勝利才解鎖符文
    if (!isLose)
        TryUnlockRune(monsterID);

if (isLose)
{
    var go = GameObject.FindGameObjectWithTag("Finish");
    go.transform.Find("Image/Text").GetComponent<Text>().text = "太大意惹...";

    var finishImage = go.transform.Find("Image").gameObject;
    finishImage.SetActive(false); // 暫時不顯示結算遮罩

        // ✅ 戰敗也回 Nani（不然 Nani 永遠不會知道輸贏）
        blackMask.DOFade(1f, 0.5f).SetDelay(0.8f).OnComplete(BackToNani);
    }
    else
    {
        var go = GameObject.FindGameObjectWithTag("Finish");
        go.transform.Find("Image/Text").GetComponent<Text>().text = "贏惹!";
        blackMask.DOFade(1f, 0.5f).SetDelay(0.3f).OnComplete(BackToNani);
    }
}

public void BackToNani()
{
    // ✅ 確保能量狀態不延續到下一場戰鬥
    var vars = Engine.GetService<ICustomVariableManager>();
    if (vars != null)
        vars.SetVariableValue("YellowActive", "false");

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


void TryUnlockRune(string mobID)
{
    mobID = mobID.Trim().ToLower();

    var data = mobUnlockTable.Find(x => 
        x.mobID.Trim().ToLower() == mobID
    );

    if (data == null)
    {
        Debug.Log($"[RuneUnlock] ❌ mobID='{mobID}' 找不到對應解鎖設定");
        return;
    }

    var ab = DataService.Instance.GetAbilityById(data.runeID);
    string key = $"UnlockedRunes_{ab.element}";

    // ✅ 讀取舊清單
    string current = PlayerPrefs.GetString(key, "");
    var list = current.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();

    // ✅ 已有解鎖就不重複
    if (!list.Contains(data.runeID))
        list.Add(data.runeID);

    // ✅ 回寫
    PlayerPrefs.SetString(key, string.Join(",", list));
    PlayerPrefs.Save();

    Debug.Log($"[RuneUnlock] ✅ 加入解鎖清單: {data.runeID} → key:{key} = {PlayerPrefs.GetString(key)}");
}




    public void ReloadScene()
    {
        SceneManager.LoadScene("CombatScene");
    }

    public void SpawnCombatText(string content, ECombatTextType type, bool isPlayer)
        => combatTxtPanel.EnqueueText(content, type, isPlayer);

    public void SpawnSystemText(string content)
        => combatTxtPanel.DisplaySystemText(content);

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
            Instantiate(animPrefab);

        CameraPlay.MangaFlash(2.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
        mainCanvas.FadeIn(0.15f);


        GameOver(false);
    }
}
