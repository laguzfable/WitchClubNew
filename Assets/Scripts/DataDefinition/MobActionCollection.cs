using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "MobActionCollection.asset", menuName = "Witch Club/MobAction")]
public class MobActionCollection : ScriptableObject
{
    public MobAction[] mobActions;
}