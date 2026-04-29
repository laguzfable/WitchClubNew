using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：隱藏 MapEventManager 按鈕，把所有小人的 call911 腳本改成 demo 專用。
/// </summary>
public class DemoMapAutoProgress : MonoBehaviour
{
    // Demo 只顯示這兩位角色
    static readonly Dictionary<string, string> DemoScripts = new Dictionary<string, string>
    {
        { "Mei",    "demo_mei"    },
        { "Mel",    "demo_mei"    },
        { "Euphie", "demo_euphie" },
        { "Eupie",  "demo_euphie" },
        { "魅兒",   "demo_mei"    },
        { "優菲",   "demo_euphie" },
    };

    // 非 demo 角色（生成後直接隱藏）
    static readonly HashSet<string> HideCharacters = new HashSet<string>
    {
        "Vivia", "Vedia", "薇狄亞", "Nelly", "涅莉",
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
        // Icon GameObject 在 CreateCharacterIcon 的第一個 yield return null 後存在，
        // 但 call911 是在那之後才 AddComponent。
        // → 第 1 幀後先隱藏不需要的角色（不必等 call911）
        // → 再多等幾幀讓 call911 掛好，然後覆寫 onClick

        // ── 第一步：1 幀後立刻隱藏 Vedia/Nelly ─────────────────
        yield return null;

        foreach (Transform child in GetAllIconRoots())
        {
            foreach (var hide in HideCharacters)
            {
                if (child.name.Contains(hide))
                {
                    child.gameObject.SetActive(false);
                    Debug.Log($"[DemoMap] 立即隱藏：{child.name}");
                    break;
                }
            }
        }

        // ── 第二步：再等幾幀讓 call911 全部掛好 ────────────────
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        var all911 = FindObjectsOfType<call911>();
        Debug.Log($"[DemoMap] 找到 {all911.Length} 個 call911 元件");
        int overridden = 0;
        foreach (var c911 in all911)
        {
            string iconName = "";
            var t = c911.transform;
            while (t != null)
            {
                if (t.name.StartsWith("Icon_")) { iconName = t.name; break; }
                t = t.parent;
            }

            foreach (var kv in DemoScripts)
            {
                if (iconName.Contains(kv.Key))
                {
                    var script = kv.Value;
                    var btn = c911.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
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

    // iconParent 下所有直接子物件（即 Icon_* 根節點）
    IEnumerable<Transform> GetAllIconRoots()
    {
        var spawner = FindObjectOfType<MapCharacterSpawner>();
        if (spawner == null || spawner.iconParent == null) yield break;
        foreach (Transform child in spawner.iconParent)
            yield return child;
    }
}
