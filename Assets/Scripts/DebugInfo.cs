using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DebugInfo : MonoBehaviour
{
    CombatSystem combatSystem;
    PlayerController pc;

    [SerializeField]
    Text debugTxt;

    // Start is called before the first frame update
    void Start()
    {
        combatSystem = GameObject.FindGameObjectWithTag("GameController").GetComponent<CombatSystem>();
        pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();


        debugTxt.text = "";
    }

    /*
    private void LateUpdate()
    {
        var env = combatSystem.envEffect;
        //string str = $"當前環境效果:{env.GetCurDescription((int)env.curType)} 回合:{env.remainTurn}\n下一個環境效果:{env.GetCurDescription((int)env.nextType)}";        
        debugTxt.text = str;
    }
    */
}
