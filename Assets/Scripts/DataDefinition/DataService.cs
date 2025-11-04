using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using Naninovel.Commands;
using UnityEngine;

public class DataService : Singleton<DataService>
{
    Dictionary<string, Ability> abilityDict = new Dictionary<string, Ability>();
    readonly Ability emptyAbility = new Ability();

    public ScriptParameter scriptParameter;
    public StringParameter startScript;
    public ScriptParameter afterChatScript;

    protected override void Init()
    {
        var totalAbilityList = new List<Ability>();

        var abilityCollectionRelease = Resources.Load<AbilityCollection>("DataCollections/AbilityCollectionRelease");
        totalAbilityList.AddRange(abilityCollectionRelease.abilityList);

        abilityDict = totalAbilityList.ToDictionary(item => item.id);

        // ✅ 初始化預設符文（只會做一次）
        InitializeDefaultRunes();
    }

    private void InitializeDefaultRunes()
    {
        // ✅ 避免覆蓋玩家現有進度
        if (PlayerPrefs.HasKey("InitializedDefaultRunes"))
        {
            Debug.Log("[DefaultRune] ⏩ 已初始化過，跳過");
            return;
        }

        foreach (var ability in abilityDict.Values)
        {
            if (ability.id.EndsWith("00")) // 找四顆預設符文
            {
                int index = (int)ability.element;

                // ✅ 設定使用中符文
                PlayerData.Instance.usingRuneIDs[index] = ability.id;

                // ✅ 設定解鎖狀態
                string key = $"{ability.element}_UnlockedRune";
                PlayerPrefs.SetString(key, ability.id);
            }
        }


        PlayerPrefs.Save();
Debug.Log(">>> [InitDefaultRunes] usingRuneIDs:" +
    $" Red={PlayerData.Instance.usingRuneIDs[0]}, " +
    $" Blue={PlayerData.Instance.usingRuneIDs[1]}, " +
    $" Yellow={PlayerData.Instance.usingRuneIDs[2]}, " +
    $" Green={PlayerData.Instance.usingRuneIDs[3]}");


        PlayerPrefs.SetInt("InitializedDefaultRunes", 1);
        PlayerPrefs.Save();
        Debug.Log("[DefaultRune] ✅ 4 顆預設符文初始化完成！");
    }

    public Ability GetAbilityById(string id)
    {
        if (abilityDict.ContainsKey(id))
            return abilityDict[id];

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
