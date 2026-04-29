using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：只顯示 Mel / Eupie，其餘隱藏，並把 onClick 導向 demo 腳本。
/// </summary>
[DefaultExecutionOrder(-200)]
public class DemoMapAutoProgress : MonoBehaviour
{
    static readonly Dictionary<string, string> DemoScripts = new Dictionary<string, string>
    {
        { "Mei",    "demo_mei"    },
        { "Mel",    "demo_mei"    },
        { "Euphie", "demo_euphie" },
        { "Eupie",  "demo_euphie" },
        { "魅兒",   "demo_mei"    },
        { "優菲",   "demo_euphie" },
    };

    static readonly HashSet<string> HideCharacters = new HashSet<string>
    {
        "Vivia", "Vedia", "薇狄亞", "Nelly", "涅莉",
    };

    void Start()
    {
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null) { mgr.gameObject.SetActive(false); }

        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        // 等 Spawner.Start() + CreateCharacterIcon coroutine + call911.Start() 全部跑完
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.3f);

        var spawners = FindObjectsOfType<MapCharacterSpawner>();
        Debug.Log($"[DemoMap] 找到 {spawners.Length} 個 Spawner");

        foreach (var s in spawners)
        {
            Transform container = s.iconParent != null ? s.iconParent : s.transform;
            Debug.Log($"[DemoMap] container={container.name} 子={container.childCount}");

            var children = new List<Transform>();
            foreach (Transform c in container) children.Add(c);

            foreach (var child in children)
            {
                if (!child.name.StartsWith("Icon_"))
                {
                    Debug.Log($"[DemoMap] 跳過：{child.name}");
                    continue;
                }

                string iconName = child.name;
                Debug.Log($"[DemoMap] 處理：{iconName}");

                // 需要隱藏的角色
                bool shouldHide = false;
                foreach (var h in HideCharacters)
                    if (iconName.Contains(h)) { shouldHide = true; break; }

                if (shouldHide)
                {
                    child.gameObject.SetActive(false);
                    Debug.Log($"[DemoMap] → 隱藏");
                    continue;
                }

                // Demo 角色：替換腳本
                foreach (var kv in DemoScripts)
                {
                    if (!iconName.Contains(kv.Key)) continue;
                    var script = kv.Value;

                    // 停用 call911，避免它再次觸發主線
                    var c911 = child.GetComponentInChildren<call911>(true);
                    if (c911 != null) c911.enabled = false;

                    // 替換 Button 的 onClick
                    var btn = child.GetComponentInChildren<Button>(true);
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            Debug.Log($"[DemoMap] 點擊 → {script}");
                            var ds = DataService.Instance;
                            if (ds != null)
                            {
                                ds.startScript     = script;
                                ds.scriptParameter = new ScriptParameter { scriptName = script };
                            }
                            UnityEngine.SceneManagement.SceneManager.LoadScene("NaniDialogTest");
                        });
                        Debug.Log($"[DemoMap] → {script}");
                    }
                    break;
                }
            }
        }

        Debug.Log("[DemoMap] 完成");
    }
}
