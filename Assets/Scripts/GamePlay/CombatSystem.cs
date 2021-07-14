using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Linq;
using Naninovel;
using UniRx.Async;
using Naninovel.UI;

public class CombatSystem : MonoBehaviour
{
    PlayerController pc;

    public EnvironmentEffect envEffect { private set; get; }

    public UICombatTextPanel combatTxtPanel { private set; get; }

    public bool isContinue = true;

    public GameObject startBattleUI;

    public GameObject dialogObj;

    public GameObject enemyTurnUI;

    public GameObject playerTurnUI;

    Text dialogText;

    Button dialogBtn;

    readonly float waitBattleTime = 3f;

    [SerializeField]
    float waitTurnTime = 0.5f;

    string monsterID;

    bool halfHpBlean = false;

    bool checkHalfHpBlean = false;

    bool zeroHpBlean = false;

    //demo用
    [SerializeField]
    Text bossName;
    
    int nowBossInt = 0;

    [SerializeField]
    GameObject winGetItem;

    [SerializeField]
    GameObject[] endObj;

    CombatMapDemo MapObj;

    GameObject currentEnemy;

    [SerializeField]
    GameObject fadeOutObj;

    AudioSource audioSource;

    [SerializeField]
    GameObject helpInfoCanvas;

    [SerializeField]
    CGFadeHelper mainCanvas;

    public bool isPlayerTurn { private set; get; }

    EnemyUnit mobUnit;

    public CombatVisualResources visualResource { private set; get; }

    [SerializeField]
    SpriteRenderer BG;


    [SerializeField]
    RawImage blackMask;


    // public async UniTask SwitchStateToCombatModeAsync(CancellationToken cancellationToken = default)
    public void SwitchStateToCombatModeAsync()
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

        Init();
    }
    /*
    public async UniTask SwitchStateToNovelModeAsync(CancellationToken cancellationToken = default)
    {
        // 1. Disable character control.
        //var controller = Object.FindObjectOfType<CharacterController3D>();
        //controller.IsInputBlocked = true;

        // 2. Switch cameras.
        var advCamera = GameObject.Find("CombatCamera").GetComponent<Camera>();
        advCamera.enabled = false;
        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        // 3. Load and play specified script (if assigned).
        if (Assigned(ScriptName))
        {
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            await scriptPlayer.PreloadAndPlayAsync(ScriptName, label: Label);
        }

        // 4. Enable Naninovel input.
        //var inputManager = Engine.GetService<IInputManager>();
        //inputManager.ProcessInput = true;
    }
    */
    [SerializeField]
    GameObject localEventSystem;
    public bool IsTestMode => localEventSystem.activeSelf;
    void Awake()
    {
        // SwitchStateToCombatModeAsync().Forget();
        localEventSystem.SetActive(!Engine.Initialized);

        pc = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        combatTxtPanel = GameObject.FindWithTag("Respawn").GetComponent<UICombatTextPanel>();
        envEffect = GetComponent<EnvironmentEffect>();
        monsterID = PlayerPrefs.GetString("enemyName");
        bossName.text = monsterID;
        
        audioSource = gameObject.GetComponent<AudioSource>();

        dialogText = dialogObj.transform.Find("Image/Text").GetComponent<Text>();
        mobUnit = GameObject.FindGameObjectWithTag("Enemy").GetComponent<EnemyUnit>();
        visualResource = GetComponent<CombatVisualResources>();

        //GetComponent<CombatUICollection>().SetRunesEnabled(false);
        if(!IsTestMode)
        {
            SwitchStateToCombatModeAsync();
        }
        else
        {
            Init();
        }
    }

    void Init()
    {
        if(!IsTestMode)
        {
            Debug.Log($"Toolbox.Instance.GetOrAddComponent<DataService>().paramArr[0] : {Toolbox.Instance.GetOrAddComponent<DataService>().scriptParameter.background}");
            BG.sprite = visualResource.GetBGByName(Toolbox.Instance.GetOrAddComponent<DataService>().scriptParameter.background);
        }
        BG.gameObject.SetActive(true);

        blackMask.DOFade(0f, 0.3f).OnComplete(() =>
        {
            startBattleUI.SetActive(true);
            DOVirtual.DelayedCall(1f, () => startBattleUI.SetActive(false));
            //StartCoroutine(CloseStartBattleUI());
        });

    }
    /*
    IEnumerator CloseStartBattleUI()
    {
        yield return new WaitForSeconds(1f);
        startBattleUI.SetActive(false);
    }*/

    void Start()
    {
        if (GameObject.Find("MainCamera_Map"))
        {
            MapObj = GameObject.Find("MainCamera_Map").GetComponent<CombatMapDemo>();
        }

        PrepareBeginTurn();
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.KeypadPeriod))
        {
            GameOver(false);
        }
    }

    public void PlayCard()
    {
        StartCoroutine(PlayResult());
    }

    class ResultOrder
    {
        public int value;
        public BaseCombatUnit owner;
        public System.Action action;
    }

    List<ResultOrder> orderList = new List<ResultOrder>();
    bool isBreakAciton = false;

    WaitForSeconds waitPlay = null;

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

    IEnumerator PlayResult()
    {
        pc.SetControllable(false);
        pc.MoveBaseCards(true);

        var playerUnit = pc.GetPlayerUnit();
        var mobActResult = mobUnit.GetActionResult();

        if(null == waitPlay)
        {
            waitPlay = new WaitForSeconds(waitTurnTime);
        }

        var mobDmg = pc.ATK - mobActResult.attr.DEF;

        if (mobActResult.curAct.type == EMobActionType.Power)
        {
            if (mobActResult.breakType == EBreakConditionType.ATK && pc.ATK >= mobActResult.breakValue)
            {
                isBreakAciton = true;
            }
            else if (mobActResult.breakType == EBreakConditionType.HP)
            {
                mobActResult.breakValue -= mobDmg;
                if (mobActResult.breakValue <= 0)
                {
                    isBreakAciton = true;
                }
            }
        }

        orderList.Clear();
        if(!isBreakAciton)
        {
            CreateOrder(4, mobActResult.attr.ATK, mobUnit, () => { // Mob deal damage

                var dmg = mobActResult.attr.ATK - pc.DEF;

                if (playerUnit.HasEffect(EAbilityEffectType.Shield)) // shield effect
                {
                    return;
                }
                if(playerUnit.HasEffect(EAbilityEffectType.Reflect)) // reflect effect
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

            CreateOrder(5, mobActResult.attr.HEAL, mobUnit, () => { // Mob deal healing

                mobUnit.ApplyHealing(mobActResult.attr.HEAL);
                if (mobUnit.HasEffect(EAbilityEffectType.HealingAttack)) // healing attack
                {
                    playerUnit.ApplyDamage(mobActResult.attr.HEAL);
                }

            });
        }

        CreateOrder(1, pc.ATK, playerUnit, () => { // player deal damage
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
                mobUnit.ApplyDamage(mobDmg);
            }

            if (playerUnit.HasEffect(EAbilityEffectType.LifeSteal) && (!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType != EEnvEffectType.NoHeal) && mobDmg > 0) // life steal
            {
                playerUnit.ApplyHealing(!playerUnit.HasEffect(EAbilityEffectType.IgnoreEnvironmentEffect) && envEffect.curType == EEnvEffectType.Heal? mobDmg * 2f : mobDmg);
            }
        });

        CreateOrder(2, isBreakAciton? 1: 0, playerUnit, () => { // interrupt
            if(!mobUnit.HasEffect(EAbilityEffectType.BreakAction))
            {
                combatTxtPanel.EnqueueText("成功打斷行動", ECombatTextType.Debuff, false);
            }
        });

        CreateOrder(3, pc.HEAL, playerUnit, () => { // player deal healing

            playerUnit.ApplyHealing(pc.HEAL);
            if(playerUnit.HasEffect(EAbilityEffectType.HealingAttack)) // healing attack
            {
                mobUnit.ApplyDamage(pc.HEAL);
            }
        });

        if(pc.result != null && pc.result.IsCombo())
        {
            CameraPlay.Shockwave(0.9f, 0.5f, 1.25f, 2f);
            CameraPlay.WidescreenH_ON(0.2f);
            blackScreen.FadeIn(0.15f);
            comboSpecialImg.sprite = pc.GetCompboSpr();

            comboSpecialImg.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            comboSpecialImg.gameObject.SetActive(false);

            blackScreen.FadeOut(0.15f);
            CameraPlay.WidescreenH_OFF(0.2f);
            yield return new WaitForSeconds(0.2f);
        }

        foreach (var or in orderList.OrderBy(or => or.value)/*orderList.OrderByDescending(or => or.value)*/)
        {
            or.action();
            yield return waitPlay;
            if(!isContinue)
            {
                yield break;
            }
        }

        if(mobActResult.targetEnvEffect != EEnvEffectType.None && !isBreakAciton)
        {
            envEffect.SetNextEffect(mobActResult.targetEnvEffect, 1);
        }

        PrepareBeginTurn();
    }

    void PrepareBeginTurn()
    {

        if (!isContinue)
        {
            return;
        }

        pc.ReflashCards(true);

        envEffect.remainTurn--;
        if (envEffect.remainTurn <= 0)
        {
            envEffect.SwitchToNextEffect();
            envEffect.GetNextEffect();
        }

        MobGetNewAction();
        isPlayerTurn = true;
        pc.MoveBaseCards(false);
        pc.ResetAttr();
        //mobUnit.ClearEffect();
        var playerUnit = pc.GetPlayerUnit();
        playerUnit.controllable = true;
        mobUnit.controllable = false;
        pc.SetControllable(true);
    }

    public void MobGetNewAction()
    {
        var act = mobUnit.GetNewAction(isBreakAciton);
        actTxt.text = mobUnit.GetActionString();
        isBreakAciton = false;
    }

    public void UpdateMobActionInfo(string actionStr)
    {
        actTxt.text = actionStr;
    }

    WaitForSeconds waitForNextTurn = new WaitForSeconds(0.5f);

    IEnumerator DisplayDialog(string str)
    {
        dialogText.text = str;
        yield return new WaitForSeconds(0.5f);
        dialogObj.SetActive(true);
        yield return new WaitForSeconds(4f);
        dialogObj.SetActive(false);
    }

    void RemoveClickDialog(BaseCombatUnit unit)
    {
        dialogObj.SetActive(false);
        dialogBtn.onClick.RemoveListener(() => RemoveClickDialog(unit));
        //NextTurn(unit);
    }

    [SerializeField]
    Text actTxt;

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

    public void CloseFightScene()
    {
        var playerData = PlayerData.Instance;// Toolbox.Instance.GetOrAddComponent<PlayerData>();
        playerData.inventory.AddItem("HealPotion", 10);
        playerData.inventory.AddItem("ManaPotion", 10);
        MapObj.BackMapScene();
        SceneManager.UnloadSceneAsync("newCombatScene");
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
        StartCoroutine(ShowEnd(waitBattleTime, isVictory));
    }

    IEnumerator ShowEnd(float time, bool isPlayerVictory) 
    {
        yield return new WaitForSeconds(3f);
        if (!isPlayerVictory)
        {
            endObj[0].SetActive(true);
        }
        else
        {
            endObj[1].SetActive(true);
        }
        yield return new WaitForSeconds(time);
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

    public IEnumerator DisplayBreakCloth(GameObject animPrefab)
    {
        if(animPrefab == null)
        {
            yield break;
        }
        pc.MoveBaseCards(true);
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        Instantiate(animPrefab);
        CameraPlay.MangaFlash(2.5f);
        yield return new WaitForSeconds(2.5f);
        mainCanvas.FadeIn(0.15f);
        pc.MoveBaseCards(false);
    }

    public IEnumerator DisplayBreakFinalCloth(GameObject animPrefab)
    {
        isContinue = false;
        pc.MoveBaseCards(true);
        mainCanvas.FadeOut(0.15f);
        Camera.main.transform.DOPunchPosition(Vector3.right, 0.2f);
        if (animPrefab != null)
        {
            Instantiate(animPrefab);
        }
        CameraPlay.MangaFlash(2.5f);
        yield return new WaitForSeconds(2.5f);
        mainCanvas.FadeIn(0.15f);
        pc.MoveBaseCards(false);
        GameOver(false);
    }

}