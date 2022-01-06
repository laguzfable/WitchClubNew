using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EnvironmentInfo : MonoBehaviour
{
    [SerializeField] Text envNameTxt, envDescTxt, envNextTxt;

    [SerializeField] Image environmentImg;
    [SerializeField] Image envBackImg;
    [SerializeField] Sprite[] envSprArr;
    
    CombatSystem combatSystem;

    EnvironmentEffect envEff;

    int lastEnvIndex;

    void Start()
    {
        combatSystem = GameObject.FindGameObjectWithTag("GameController").GetComponent<CombatSystem>();
        envEff = combatSystem.envEffect;
        envEff.envEffChangeEvent += UpdateInfo;

    }

    public float transSpeed = 0.5f;

    public void UpdateInfo()
    {
        envNameTxt.text = envEff.GetCurEffectName((int)envEff.curType);
        envDescTxt.text = envEff.GetCurDescription((int)envEff.curType);
        envNextTxt.text = "Next:" + envEff.GetCurEffectName((int)envEff.nextType);
        
        if(lastEnvIndex == (int)envEff.curType)
        {
            return;
        }
        // environmentImg.sprite = envSprArr[(int)envEff.curType];
        envBackImg.sprite = envSprArr[(int)envEff.curType];
        envBackImg.DOFade(1f, transSpeed);
        environmentImg.DOFade(0f, transSpeed).onComplete += ()=> 
        {
            environmentImg.sprite = envSprArr[(int)envEff.curType];
            environmentImg.DOFade(1f, 0f);
            envBackImg.DOFade(0f, 0f);
            lastEnvIndex = (int)envEff.curType;
        };

        //string str = $"當前環境效果:{env.GetCurEffectName((int)env.curType)}\n{env.GetCurDescription((int)env.curType)}\n\n下一個環境效果:{env.GetCurEffectName((int)env.nextType)}";
        //txt.text = str;
    }
}