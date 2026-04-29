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
                string rawName = child.name;

                if (!rawName.StartsWith("Icon_"))
                {
                    Debug.Log($"[DemoMap] 跳過（非Icon）：'{rawName}'");
                    continue;
                }

                Debug.Log($"[DemoMap] ── icon 名稱：'{rawName}'");

                // ── 隱藏判定 ──
                string matchedHide = null;
                foreach (var h in HideCharacters)
                    if (rawName.Contains(h)) { matchedHide = h; break; }

                if (matchedHide != null)
                {
                    child.gameObject.SetActive(false);
                    Debug.Log($"[DemoMap]   HideCharacters 命中 '{matchedHide}' → SetActive(false)");
                    continue;
                }

                // ── Demo 腳本替換 ──
                string matchedKey = null;
                string matchedScript = null;
                foreach (var kv in DemoScripts)
                {
                    if (rawName.Contains(kv.Key))
                    {
                        matchedKey    = kv.Key;
                        matchedScript = kv.Value;
                        break;
                    }
                }
                Debug.Log($"[DemoMap]   DemoScripts 命中：key='{matchedKey}' script='{matchedScript}'");

                if (matchedScript == null)
                {
                    Debug.LogWarning($"[DemoMap]   未命中任何規則，保持原樣：'{rawName}'");
                    continue;
                }

                var script = matchedScript;

                // call911 と同じ GO の Button を取得（call911 は GirlButton に AddComponent される）
                var c911 = child.GetComponentInChildren<call911>(true);
                Button btn = null;
                if (c911 != null)
                {
                    btn = c911.GetComponent<Button>();
                    Debug.Log($"[DemoMap]   call911 場所：{c911.gameObject.name}  Button={btn != null}");
                }
                // fallback
                if (btn == null) btn = child.GetComponentInChildren<Button>(true);

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
                    Debug.Log($"[DemoMap]   Button '{btn.gameObject.name}' onClick 替換完成 → {script}");
                }
                else Debug.LogWarning($"[DemoMap]   Button 找不到");
            }
        }

        Debug.Log("[DemoMap] 完成");
    }
}
