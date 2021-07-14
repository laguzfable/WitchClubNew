using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TutorialController : MonoBehaviour
{
    public CombatUICollection uICollection;
    [SerializeField] PlayerController playerController;
    [SerializeField] Image charImg;
    [SerializeField] GameObject leftDialog;
    [SerializeField] GameObject rightDialog;

    static public bool isTutorial;

    [SerializeField] TutorialObject[] tutorialArr;

    public bool canGoNext = false;

    private void Start()
    {
        if(isTutorial)
        {
            uICollection.TurnOffAll();

            StartCoroutine(RunTutorialSequence());
        }
    }


    public TutorialObject curTutorialObj { private set; get;} = null;

    Sequence seq;

    IEnumerator RunTutorialSequence()
    {
        
        seq = DOTween.Sequence();
        foreach(var tutorial in tutorialArr)
        {
            seq.Kill();
            
            curTutorialObj = tutorial;
            canGoNext = false;

            if(tutorial.isMobUnit)
            {
                charImg.gameObject.SetActive(false);
                uICollection.mob.sprRend.sprite = tutorial.displayImg;
                uICollection.mob.sprRend.enabled = true;
            }
            else
            {
                charImg.sprite = tutorial.displayImg;
                charImg.SetNativeSize();
                charImg.gameObject.SetActive(true);
                charImg.GetComponent<RectTransform>().anchoredPosition = tutorial.unitPos;
                uICollection.mob.sprRend.enabled = false;
            }

            var dialog = tutorial.isRight? rightDialog : leftDialog;
            dialog.SetActive(true);
            
            dialog.transform.localScale = Vector3.zero;
            dialog.GetComponentInChildren<Text>().text = "";
            seq.Append(dialog.GetComponentInChildren<Text>().DOText(tutorial.dialog, 1f).SetEase(Ease.Linear));
            seq.Join(dialog.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.5f);

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
                yield return new WaitUntil(()=> Input.GetMouseButtonUp(0));
                yield return new WaitForSeconds(0.32f);
            }
            else
            {
                playerController.combatSystem.PrepareBeginTurn();
                yield return new WaitUntil(()=> canGoNext);
                yield return new WaitForSeconds(0.32f);
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
}