using System.Collections;
using System.Collections.Generic;
using Naninovel.Commands;
using UnityEngine;

public class DataService : Singleton<DataService>
{
    // AbilityCollection abilityCollection;

    Dictionary<string, Ability> abilityDict = new Dictionary<string, Ability>();

    readonly Ability emptyAbility = new Ability();


    //暫時先用這邊紀錄NANI過來的資源
    //public Naninovel.Commands.StringParameter[] paramArr;
    public ScriptParameter scriptParameter;

    public StringParameter startScript;

    public ScriptParameter afterChatScript;

    protected override void Init()
    {
        var abilityCollection = Resources.Load<AbilityCollection>("DataCollections/AbilityCollection");
        var abilityList = abilityCollection.abilityList;
        foreach (var ability in abilityList)
        {
            abilityDict[ability.id] = ability;
        }
    }

    public Ability GetAbilityById(string id)
    {
        if(abilityDict.ContainsKey(id))
        {
            return abilityDict[id];
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
