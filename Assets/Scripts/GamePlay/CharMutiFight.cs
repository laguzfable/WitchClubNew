using System.Collections;
using UnityEngine;

public class CharMutiFight : MonoBehaviour
{
    CharMutiAbilityCollection charMutiAbilityCollection;
    CharMutiCgCollection charMutiCgCollection;
    public GameObject showCg;
    public float desCgTime = 0f;

    void Awake()
    {
        charMutiAbilityCollection = Resources.Load<CharMutiAbilityCollection>("DataCollections/CharMutiAbilityCollection");
        charMutiCgCollection = Resources.Load<CharMutiCgCollection>("DataCollections/CharMutiCgCollection");
    }

    public bool CheckCharMutiFight(PlayedCardResult result, PlayerUnit unit, UIWitchAbility[] witchCards) {
        bool costSP = false;
        foreach(var item in charMutiAbilityCollection.charMutiAbility)
        {
            if(result.state == item.elementState)
            {   /*
                bool redBlean = false;
                bool blueBlean = false;
                bool yellowBlean = false;
                bool greenBlean = false;
                switch (item.elementState) {
                    case EElementState.Full:
                        redBlean = blueBlean = yellowBlean = greenBlean = true;
                    break;
                    case EElementState.RBY:
                        redBlean = blueBlean = yellowBlean = true;
                    break;
                    case EElementState.RGB:
                        redBlean = blueBlean = greenBlean = true;
                    break;
                    case EElementState.RGY:
                        redBlean = yellowBlean = greenBlean = true;
                    break;
                    case EElementState.GBY:
                        blueBlean = yellowBlean = greenBlean = true;
                    break;
                    case EElementState.GB:
                        blueBlean = greenBlean = true;
                    break;
                    case EElementState.GY:
                        yellowBlean = greenBlean = true;
                    break;
                    case EElementState.RB:
                        redBlean = blueBlean = true;
                    break;
                    case EElementState.RG:
                        redBlean = greenBlean = true;
                    break;
                    case EElementState.RY:
                        redBlean = yellowBlean = true;
                    break;
                    case EElementState.BY:
                        blueBlean = yellowBlean = true;
                    break;
                }
                foreach (var cg in charMutiCgCollection.charMutiCg)
                {
                    if (item.elementState == cg.elementState)
                    {
                        showCg = Instantiate(cg.cgPrefab);
                        desCgTime = cg.showCgTime;
                    }
                }*/
                /*if (redBlean) {
                    witchCards[(int)ECardElement.Red].cost.Value += item.ability.requireEnergy;
                }
                if(blueBlean) {
                    witchCards[(int)ECardElement.Blue].cost.Value += item.ability.requireEnergy;
                }
                if(yellowBlean) {
                    witchCards[(int)ECardElement.Yellow].cost.Value += item.ability.requireEnergy;
                }
                if(greenBlean) {
                    witchCards[(int)ECardElement.Green].cost.Value += item.ability.requireEnergy;
                }*/
                /*if(!costSP) {
                    unit.SP.Value -= item.ability.cost;
                    costSP = true;
                }*/
                //Debug.Log("item: " + item.elementState);
                //return item.ability;
                unit.CastAbility(item.ability);
                return true;
            } 
        } 
        return false;
        //return new Ability();
    }
}
