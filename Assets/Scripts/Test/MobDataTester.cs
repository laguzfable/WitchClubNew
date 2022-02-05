using Naninovel;
using UnityEngine;

public class MobDataTester : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        var data = await Resources.LoadAsync<MobData>("MobData/TestMobData");

        Debug.Log($"MobName : {data.name}");
    }
}