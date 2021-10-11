using UnityEngine;
using UniRx.Async;

public class MobDataTester : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        var data = await Resources.LoadAsync<MobData>("MobData/TestMobData");

        Debug.Log($"MobName : {data.name}");
    }
}