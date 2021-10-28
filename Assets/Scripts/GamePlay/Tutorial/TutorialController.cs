using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Naninovel;
using UniRx.Async;
using System;

public class TutorialController : MonoBehaviour
{
    public CombatUICollection uICollection;
    [SerializeField] PlayerController playerController;
    [SerializeField] Image charImg;
    [SerializeField] GameObject leftDialog;
    [SerializeField] GameObject leftArrow;
    [SerializeField] GameObject rightDialog;
    [SerializeField] GameObject rightArrow;

    static public bool isTutorial;

    [SerializeField] TutorialObject[] tutorialArr;

    [SerializeField] GameObject tutorBG;

    public bool canGoNext = false;

    private void Start()
    {
        if(isTutorial)
        {
            uICollection.TurnOffAll();

            RunTutorialSequenceAsync().Forget();
        }
    }


    public TutorialObject curTutorialObj { private set; get;} = null;

    Sequence seq;

    async UniTaskVoid RunTutorialSequenceAsync()
    {
        bool isDialogFinish = false;
        seq = DOTween.Sequence();
        foreach(var tutorial in tutorialArr)
        {
            seq.Kill();
            
            curTutorialObj = tutorial;
            canGoNext = false;
            isDialogFinish = false;

            if(tutorial.isMobUnit)
            {
                charImg.gameObject.SetActive(false);
                uICollection.mob.sprRend.sprite = tutorial.displayImg;
                uICollection.mob.sprRend.enabled = true;
                uICollection.mob.isMovable = false;
                uICollection.mob.transform.position = tutorial.unitPos;
                uICollection.mob.SetOrgY(tutorial.unitPos.y);
                uICollection.mob.isMovable = true;
            }
            else
            {
                charImg.sprite = tutorial.displayImg;
                charImg.SetNativeSize();
                charImg.gameObject.SetActive(true);
                charImg.GetComponent<RectTransform>().anchoredPosition = tutorial.unitPos;
                charImg.GetComponent<CharacterMove>().SetOrgY();
                charImg.GetComponent<CharacterMove>().enabled = tutorial.displayBG == null;
                uICollection.mob.sprRend.enabled = false;
            }
            
            leftArrow.SetActive(false);
            rightArrow.SetActive(false);
            var arrow = tutorial.isRight? rightArrow : leftArrow;

            var dialog = tutorial.isRight? rightDialog : leftDialog;
            dialog.SetActive(true);
            
            dialog.transform.localScale = Vector3.zero;
            dialog.GetComponentInChildren<Text>().text = "";
            var dialogContent = tutorial.dialog;
            if(dialogContent.Contains("P"))
            {
                dialogContent = dialogContent.Replace("P", Engine.GetService<ICustomVariableManager>().GetVariableValue("PlayerName"));
            }
            seq.Append(dialog.GetComponentInChildren<Text>().DOText(dialogContent, 1f).SetEase(Ease.Linear)
                .OnComplete(()=>
                {
                    isDialogFinish = true;
                    arrow.SetActive(true);
                })
            );
            seq.Join(dialog.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.5f);
            
            if(tutorial.isChangeEnv)
            {
                playerController.combatSystem.envEffect.SetNextEffect(tutorial.changeEnv);
            }

            if(tutorial.displayBG != null)
            {
                tutorial.displayBG.SetActive(true);
            }



            foreach(var displayObj in tutorial.displayObjecArr)
            {
                displayObj.SetActive(true);

                if(tutorial.isClearDisplay)
                {
                    displayObj.GetComponent<Image>().CrossFadeAlpha(0f, 0f, true);
                    displayObj.transform.localScale = Vector3.one * 0.6f;
                    
                    displayObj.GetComponent<Image>().CrossFadeAlpha(1f, 0.3f, true);
                    // seq.Join(displayObj.GetComponent<Image>().DOFade(1f, 0.2f));
                    seq.Join(displayObj.transform.DOScale(1f, 0.2f));
                    seq.AppendInterval(0.2f);
                }
            }

            if(string.IsNullOrEmpty(tutorial.customActionID))
            {
                playerController.SetControllable(false);
                await UniTask.WaitUntil(()=> isDialogFinish);
                await UniTask.WaitUntil(()=> Input.GetMouseButtonUp(0));
                await UniTask.Delay(TimeSpan.FromSeconds(0.32f));
            }
            else
            {
                playerController.combatSystem.PrepareBeginTurn();
                playerController.combatSystem.envEffect.SwitchToNextEffect();
                playerController.combatSystem.envEffect.SetNextEffect(EEnvEffectType.None);
                playerController.SetControllable(true);
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                await UniTask.WaitUntil(()=> canGoNext);
                await UniTask.Delay(TimeSpan.FromSeconds(0.32f));
            }
            if(tutorial.isClearDisplay)
            {
                foreach(var displayObj in tutorial.displayObjecArr)
                {
                    displayObj.SetActive(false);
                }
            }

            leftDialog.SetActive(false);
            rightDialog.SetActive(false);
            

            if(tutorial.displayBG != null)
            {
                tutorial.displayBG.SetActive(false);
            }
        }
        playerController.combatSystem.GameOver(false);
    }

}

[System.Serializable]
public class TutorialObject
{
    public string dialog;
    public bool isRight;
    public Sprite displayImg;    
    public Vector2 unitPos;
    public bool isMobUnit;
    public GameObject[] displayObjecArr;
    public bool isClearDisplay;
    public string customActionID;
    public bool isChangeEnv;
    public EEnvEffectType changeEnv;

    public GameObject displayBG;
}