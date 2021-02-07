using UnityEngine;
using System.Collections;

public class BossUnit : EnemyUnit
{

    [SerializeField]
    GameObject[] brokenClothFx;

    [SerializeField]
    MeshRenderer[] fullCloths;

    [SerializeField]
    MeshRenderer[] halfCloths;

    enum EClothBreakLevel { Normal, Half, Full };

    [SerializeField]
    GameObject breakClothAnimPrefab;//only for alpha build.

    [SerializeField]
    GameObject breakFinalClothAnimPrefab;//only for alpha build.

    EClothBreakLevel breakLv = EClothBreakLevel.Normal;

    /*
    protected override void Init()
    {
        base.Init();
        HP.OnValueChanged += CheckHpStatus;
    }
    */
    public void CheckHpStatus(float value)
    {
        if(brokenClothFx.Length < 0)
        {
            return;
        }
        if (breakLv == EClothBreakLevel.Normal && HP.Value <= (HP.GetTotalValue() * 0.7f))
        {
            breakLv = EClothBreakLevel.Half;

            if(brokenClothFx.Length > 1 && brokenClothFx[0] != null)
            {
                brokenClothFx[0].SetActive(true);
            }
            //50%
            foreach (var item in halfCloths)
            {
                item.enabled = false;
            }
            
        }
        else if (breakLv == EClothBreakLevel.Half && HP.Value <= (HP.GetTotalValue() * 0.3f))
        {
            breakLv = EClothBreakLevel.Full;
            StartCoroutine(combatSystem.DisplayBreakCloth(breakClothAnimPrefab));

            if(brokenClothFx.Length > 2 && brokenClothFx[1] != null)
            {
                brokenClothFx[1].SetActive(true);
            }
            //100%
            foreach (var item in halfCloths)
            {
                item.enabled = false;
            }
            foreach (var item in fullCloths)
            {
                item.enabled = false;
            }
        }
    }
    /*
    protected override void OnDefeated()
    {
        //base.OnDefeated();
        StartCoroutine(combatSystem.DisplayBreakFinalCloth(breakFinalClothAnimPrefab));
    }
    */
}
