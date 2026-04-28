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
        { "Mei",    "demo_mei"    },
        { "Mel",    "demo_mei"    },
        { "Vivia",  "demo_vivia"  },
        { "Vedia",  "demo_vivia"  },
        { "Euphie", "demo_euphie" },
        { "Eupie",  "demo_euphie" },
        { "Nelly",  "demo_nelly"  },
        // 中文備用
        { "魅兒",   "demo_mei"    },
        { "薇狄亞",  "demo_vivia"  },
        { "優菲",   "demo_euphie" },
        { "涅莉",   "demo_nelly"  },
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
        // call911 是在 CreateCharacterIcon 的 yield return null 之後才 AddComponent，
        // 多等幾幀確保全部 4 個圖示都掛好 call911
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        var all911 = FindObjectsOfType<call911>();
        Debug.Log($"[DemoMap] 找到 {all911.Length} 個 call911 元件");
        int overridden = 0;
        foreach (var c911 in all911)
        {
            // 往上找到 Icon_{characterName}_{eventName} 的節點
            string iconName = "";
            var t = c911.transform;
            while (t != null)
            {
                if (t.name.StartsWith("Icon_")) { iconName = t.name; break; }
                t = t.parent;
            }

            Debug.Log($"[DemoMap] call911 找到 icon：'{iconName}'");

            foreach (var kv in DemoScripts)
            {
                if (iconName.Contains(kv.Key))
                {
                    var script = kv.Value;
                    var btn = c911.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        // 完全替換 onClick，繞過 call911 的 fallback 路徑
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            Debug.Log($"[DemoMap] 點擊小人 → {script}");
                            var ds = DataService.Instance;
                            if (ds != null)
                            {
                                ds.startScript     = script;
                                ds.scriptParameter = new ScriptParameter { scriptName = script };
                            }
                            UnityEngine.SceneManagement.SceneManager.LoadScene("NaniDialogTest");
                        });
                    }
                    overridden++;
                    Debug.Log($"[DemoMap] {iconName} → {script}");
                    break;
                }
            }
        }

        Debug.Log($"[DemoMap] 共覆寫 {overridden} 個小人腳本");
    }
}
