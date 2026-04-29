using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Naninovel;
using System;

/// <summary>
/// Demo 戰鬥引導控制器。
/// 掛在 CombatScene 同一個 GameObject 上（可與 TutorialController 共存）。
///
/// mobMei    → 跑 meiSteps
/// mobVivia  → 跑 viviaSteps
/// mobEuphie → 跑 euphieSteps
/// mobNelly  → 自由遊玩（不做任何引導）
/// </summary>
public class DemoCombatController : MonoBehaviour
{
    [Header("Scene References (same objects as TutorialController)")]
    public CombatUICollection uICollection;
    [SerializeField] PlayerController playerController;
    [SerializeField] Image charImg;
    [SerializeField] GameObject leftDialog;
    [SerializeField] GameObject leftArrow;
    [SerializeField] GameObject rightDialog;
    [SerializeField] GameObject rightArrow;
    [SerializeField] GameObject leftUpDialog;
    [SerializeField] GameObject leftUpArrow;
    [SerializeField] GameObject rightDownDialog;
    [SerializeField] GameObject rightDownArrow;
    [SerializeField] GameObject tutorBG;
    [SerializeField] CombatSceneLocalization localization;

    [Header("Demo Steps — 三個角色各自的教學步驟")]
    [SerializeField] TutorialObject[] meiSteps;
    [SerializeField] TutorialObject[] viviaSteps;
    [SerializeField] TutorialObject[] euphieSteps;

    public bool canGoNext = false;

    Sequence seq;

    void Start()
    {
        var target = DataService.Instance?.scriptParameter?.combatTarget?.Value;

        TutorialObject[] steps = target switch
        {
            "mobMei"    => meiSteps,
            "mobVivia"  => viviaSteps,
            "mobEuphie" => euphieSteps,
            _           => null   // mobNelly 或其他 → 自由遊玩
        };

        if (steps != null && steps.Length > 0)
        {
            uICollection.TurnOffAll();
            RunDemoSequenceAsync(steps).Forget();
        }
    }

    async UniTaskVoid RunDemoSequenceAsync(TutorialObject[] steps)
    {
        bool isDialogFinish = false;
        seq = DOTween.Sequence();

        foreach (var step in steps)
        {
            seq.Kill();

            canGoNext      = false;
            isDialogFinish = false;

            // ── 設定角色 / 怪物顯示 ──────────────────────────────
            if (step.isMobUnit)
            {
                charImg.gameObject.SetActive(false);
                uICollection.mob.sprRend.sprite  = step.displayImg;
                uICollection.mob.sprRend.enabled = true;
                uICollection.mob.isMovable       = false;
                uICollection.mob.transform.position = step.unitPos;
                uICollection.mob.SetOrgY(step.unitPos.y);
                uICollection.mob.isMovable = true;
            }
            else
            {
                charImg.sprite = step.displayImg;
                charImg.SetNativeSize();
                charImg.gameObject.SetActive(true);
                charImg.GetComponent<RectTransform>().anchoredPosition = step.unitPos + new Vector2(0f, 540f);
                charImg.GetComponent<CharacterMove>().SetOrgY();
                uICollection.mob.sprRend.enabled = false;
            }

            // ── 選擇方向箭頭 & 對話框 ────────────────────────────
            leftArrow.SetActive(false);
            rightArrow.SetActive(false);
            leftUpArrow?.SetActive(false);
            rightDownArrow?.SetActive(false);

            var arrow  = step.isRightDown ? rightDownArrow
                       : step.isLeftUp    ? leftUpArrow
                       : step.isRight     ? rightArrow
                       :                   leftArrow;

            var dialog = step.isRightDown ? rightDownDialog
                       : step.isLeftUp    ? leftUpDialog
                       : step.isRight     ? rightDialog
                       :                   leftDialog;

            dialog.SetActive(true);
            dialog.transform.localScale = Vector3.zero;
            dialog.GetComponentInChildren<Text>().text = "";

            var content = localization.GetLocalizedContent(step.dialogLocaleId, step.dialog);
            if (content.Contains("P"))
                content = content.Replace("P", Engine.GetService<ICustomVariableManager>().GetVariableValue("PlayerName"));

            seq.Append(
                dialog.GetComponentInChildren<Text>()
                      .DOText(content, 1f)
                      .SetEase(Ease.Linear)
                      .OnComplete(() => { isDialogFinish = true; arrow.SetActive(true); })
            );
            seq.Join(dialog.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.5f);

            // ── 環境效果 ─────────────────────────────────────────
            if (step.isChangeEnv)
                playerController.combatSystem.envEffect.SetNextEffect(step.changeEnv);

            if (step.displayBG != null)
                step.displayBG.SetActive(true);

            // ── 顯示高亮物件 ─────────────────────────────────────
            foreach (var obj in step.displayObjecArr)
            {
                obj.SetActive(true);
                if (step.isClearDisplay)
                {
                    obj.GetComponent<Image>().CrossFadeAlpha(0f, 0f, true);
                    obj.transform.localScale = Vector3.one * 0.6f;
                    obj.GetComponent<Image>().CrossFadeAlpha(1f, 0.3f, true);
                    seq.Join(obj.transform.DOScale(1f, 0.2f));
                    seq.AppendInterval(0.2f);
                }
            }

            // ── 等待互動 ─────────────────────────────────────────
            if (string.IsNullOrEmpty(step.customActionID))
            {
                playerController.SetControllable(false);
                await UniTask.WaitUntil(() => isDialogFinish);
                await UniTask.WaitUntil(() => Input.GetMouseButtonUp(0));
                await UniTask.Delay(TimeSpan.FromSeconds(0.32f));
            }
            else if (step.customActionID == "fullBlue")
            {
                var rune = uICollection.Runes.transform.Find("SkillBG/SkillButtonB").GetComponent<UIWitchAbility>();
                rune.cost.Value = rune.ability.requireEnergy;
            }
            else if (step.customActionID == "emptyBlue")
            {
                var rune = uICollection.Runes.transform.Find("SkillBG/SkillButtonB").GetComponent<UIWitchAbility>();
                rune.cost.Value = 0;
            }
            else
            {
                playerController.combatSystem.PrepareBeginTurn();
                playerController.combatSystem.envEffect.SwitchToNextEffect();
                playerController.combatSystem.envEffect.SetNextEffect(EEnvEffectType.None);
                playerController.SetControllable(true);
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                await UniTask.WaitUntil(() => canGoNext);
                await UniTask.Delay(TimeSpan.FromSeconds(0.32f));
            }

            // ── 清除高亮 & 關閉對話框 ────────────────────────────
            if (step.isClearDisplay)
                foreach (var obj in step.displayObjecArr)
                    obj.SetActive(false);

            leftDialog.SetActive(false);
            rightDialog.SetActive(false);
            leftUpDialog?.SetActive(false);
            rightDownDialog?.SetActive(false);

            if (step.displayBG != null)
                step.displayBG.SetActive(false);
        }

        playerController.combatSystem.GameOver(false);
    }
}
