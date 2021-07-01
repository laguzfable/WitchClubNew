using System.Collections;
using System.Collections.Generic;
using Naninovel.Commands;
using UnityEngine;

public class DataService : MonoBehaviour {

    AbilityCollection abilityCollection;

    Ability emptyAbility = new Ability();

    public DataCollection<ItemData> itemData { private set; get; } = new DataCollection<ItemData>();


    //暫時先用這邊紀錄NANI過來的資源
    //public Naninovel.Commands.StringParameter[] paramArr;
    public ScriptParameter scriptParameter;

    public StringParameter startScript;

    public ScriptParameter afterChatScript;


    // Use this for initialization
    void Awake()
    {
        Toolbox.RegisterComponent<DataService>();
        abilityCollection = Resources.Load<AbilityCollection>("DataCollections/AbilityCollection");
        itemData.Init("ItemCollection");
    }

    public Ability GetAbilityById(string id)
    {
        foreach (var abi in abilityCollection.abilityList)
        {
            if(abi.id.Equals(id))
            {
                return abi;
            }
        }
        return emptyAbility;
    }

    static public bool IsEmpty(Ability ability)
    {
        return string.IsNullOrEmpty(ability.id);
    }


}

public class ScriptParameter
{
    public StringParameter background;

    public StringParameter combatTarget;
    public StringParameter scriptName;
    public StringParameter scriptLabel;
}
