using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
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
        var totalAbilityList = new List<Ability>();

        var abilityCollectionRelease = Resources.Load<AbilityCollection>("DataCollections/AbilityCollectionRelease"); // 正式抓這一份
        totalAbilityList.AddRange(abilityCollectionRelease.abilityList);

        // var abilityCollectionTest = Resources.Load<AbilityCollection>("DataCollections/AbilityCollection");// 測試用資料
        // totalAbilityList.AddRange(abilityCollectionTest.abilityList);
        
        abilityDict = totalAbilityList.ToDictionary(item => item.id);
        // var abilityList = abilityCollection.abilityList;
        // foreach (var ability in abilityList)
        // {
        //     abilityDict[ability.id] = ability;
        // }
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
