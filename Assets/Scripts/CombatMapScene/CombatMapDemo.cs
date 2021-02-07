using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class CombatMapDemo : MonoBehaviour
{
    [SerializeField]
    RectTransform warFogObj;
    [SerializeField]
    GameObject[] frames;
    GameObject canvas;
    GameObject fadeinCanvas;
    AudioSource BGM;
    [SerializeField]
    Button[] buttons;
    [SerializeField]
    GameObject destoryObj;
    // Start is called before the first frame update
    void Start()
    {
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        warFogObj.gameObject.SetActive(true);
        warFogObj.DOLocalMove(new Vector3(245,0,0),2);
        fadeinCanvas = GameObject.Find("Canvas_FadeIn");
        canvas = GameObject.Find("Canvas_BattleMap");
        SceneManager.sceneLoaded += OnSceneLoadCompleted;
        fadeinCanvas.SetActive(false);
        BGM = GetComponent<AudioSource>();
        buttons[1].interactable = false;
        buttons[0].interactable = true;
    }

    public void FightFinalBoss(string monster)
    {
        PlayerPrefs.SetString("enemyName", monster);
        Invoke("AddScene", 1);
        //canvas.SetActive(false);
        fadeinCanvas.SetActive(true);
        fadeinCanvas.GetComponent<Animation>().Play("LoadingFadeIn");
        canvas.GetComponent<CanvasGroup>().interactable = false;
        
    }

    void AddScene()
    {
        SceneManager.LoadSceneAsync("newCombatScene_FixUI", LoadSceneMode.Additive);
    }

    void OnSceneLoadCompleted(Scene newScnee, LoadSceneMode mode)
    {
        if (fadeinCanvas)
        {
            fadeinCanvas.GetComponent<Animation>().Play("LoadingFadeOut");
            fadeinCanvas.SetActive(false);
        }
        else {
            fadeinCanvas = GameObject.Find("Canvas_FadeIn");
        }
        // Invoke("HideLoadingCanvans", 1);
        if (canvas != null)
        {
            canvas.SetActive(false);
        }
        else
        {
            canvas = GameObject.Find("Canvas_BattleMap");
        }
        if (BGM != null)
        {
            BGM = GetComponent<AudioSource>();
            BGM.DOFade(0, 1f);
        }
        else {
            BGM.DOFade(0, 1f);
        }
        
        //BGM.Stop();
        Debug.Log("On Scene Load Completed!!!!!!"+ newScnee);
    }

    void HideLoadingCanvans()
    {
        fadeinCanvas.SetActive(false);
    }

    public void BackMapScene() {
        warFogObj.DOLocalMove(new Vector3(1200, 0, 0), 2);
        frames[0].SetActive(false);
        frames[1].SetActive(true);
        BGM.DOFade(1, 1f);
        canvas.SetActive(true);
        fadeinCanvas.GetComponent<Animation>().Play("LoadingFadeIn");
        buttons[0].interactable = false;
        buttons[1].interactable = true;
        destoryObj.SetActive(true);
        canvas.GetComponent<CanvasGroup>().interactable = true;
    }

}
