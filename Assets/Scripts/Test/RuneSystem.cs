using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;
using System.Linq;

public class RuneSystem : MonoBehaviour
{
    public GameObject localEventSystem;


    // Start is called before the first frame update
    void Start()
    {
        if(!Engine.Initialized)
        {
            Debug.Log($"Engine.Initialized? {Engine.Initialized}");
            localEventSystem.SetActive(true);
            return;
        }
        var cardArr = GameObject.FindObjectsOfType<SelectRuneCard>();

        if(Engine.GetService<ICustomVariableManager>().TryGetVariableValue<string>("MobList", out string mobListStr) && string.IsNullOrEmpty(mobListStr))
        {
            var mobList = mobListStr.Split(',');
            foreach(var card in cardArr)
            {
                card.SetInteractable(mobList.Any(x => x == card.abilityID));
            }
        }
    }
}
