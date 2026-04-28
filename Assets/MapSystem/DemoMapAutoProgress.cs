using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：隱藏 MapEventManager 按鈕，把所有小人的 call911 腳本改成 demo 專用。
/// </summary>
public class DemoMapAutoProgress : MonoBehaviour
{
    // 角色名稱 → demo 腳本對應表（配合 MapCharacterSpawner 的 characterName）
    static readonly Dictionary<string, string> DemoScripts = new Dictionary<string, string>
    {
        { "魅兒",  "demo_mei"    },
        { "薇狄亞", "demo_vivia"  },
        { "優菲",  "demo_euphie" },
        { "涅莉",  "demo_nelly"  },
    };

    void Start()
    {
        // 隱藏 MapEventManager 的按鈕列
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null)
        {
            mgr.gameObject.SetActive(false);
            Debug.Log("[DemoMap] MapEventManager 已隱藏");
        }

        // 等小人生成完再覆寫腳本
        StartCoroutine(OverrideCharacterScripts());
    }

    IEnumerator OverrideCharacterScripts()
    {
        // MapCharacterSpawner 用 coroutine 生成，等兩幀確保完成
        yield return null;
        yield return null;

        int overridden = 0;
        foreach (var c911 in FindObjectsOfType<call911>())
        {
            // icon 名稱格式：Icon_{characterName}_{eventName}
            string iconName = c911.transform.parent?.parent?.name ?? "";

            foreach (var kv in DemoScripts)
            {
                if (iconName.Contains(kv.Key))
                {
                    c911.劇本名 = kv.Value;
                    overridden++;
                    Debug.Log($"[DemoMap] {iconName} → {kv.Value}");
                    break;
                }
            }
        }

        Debug.Log($"[DemoMap] 共覆寫 {overridden} 個小人腳本");
    }
}
