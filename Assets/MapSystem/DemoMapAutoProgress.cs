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

    MapCharacterSpawner[] _spawners;

    void Awake()
    {
        _spawners = FindObjectsOfType<MapCharacterSpawner>();
        Debug.Log($"[DemoMap] Awake ── 找到 {_spawners.Length} 個 MapCharacterSpawner，全部停用");
        foreach (var s in _spawners)
        {
            Debug.Log($"[DemoMap]   停用：{s.gameObject.name}  iconParent={(s.iconParent != null ? s.iconParent.name : "null")}");
            s.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null) { mgr.gameObject.SetActive(false); Debug.Log("[DemoMap] MapEventManager 隱藏"); }

        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        // ── 1. 重新啟動所有 Spawner，同幀先把容器藏起來
        foreach (var s in _spawners)
        {
            if (s == null) continue;
            s.gameObject.SetActive(true);
            if (s.iconParent != null)
                s.iconParent.gameObject.SetActive(false);
        }

        // ── 2. 等 Spawner.Start() 跑完、call911.Start() 也跑完
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.3f);

        // ── 3. 逐 Spawner 處理
        foreach (var s in _spawners)
        {
            if (s == null) continue;

            // iconParent が null なら Spawner.transform 自体を親として扱う
            Transform container = s.iconParent != null ? s.iconParent : s.transform;
            Debug.Log($"[DemoMap] Spawner={s.gameObject.name}  container={container.name}  子={container.childCount}");

            var children = new List<Transform>();
            foreach (Transform c in container) children.Add(c);

            foreach (var child in children)
            {
                string name = child.name;
                Debug.Log($"[DemoMap]   icon: '{name}'");

                // 隱藏判定
                bool shouldHide = false;
                foreach (var h in HideCharacters)
                    if (name.Contains(h)) { shouldHide = true; break; }

                if (shouldHide)
                {
                    child.gameObject.SetActive(false);
                    Debug.Log($"[DemoMap]   → 隱藏");
                    continue;
                }

                // demo 腳本替換
                bool matched = false;
                foreach (var kv in DemoScripts)
                {
                    if (!name.Contains(kv.Key)) continue;
                    matched = true;
                    var script = kv.Value;

                    // call911 停用（Stop() が AddListener を再追加するのを防ぐ）
                    var c911 = child.GetComponentInChildren<call911>(true);
                    if (c911 != null) { c911.enabled = false; Debug.Log($"[DemoMap]   call911 停用"); }
                    else Debug.LogWarning($"[DemoMap]   call911 が見つかりません: {name}");

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
                        Debug.Log($"[DemoMap]   → {script}");
                    }
                    else Debug.LogWarning($"[DemoMap]   Button が見つかりません: {name}");
                    break;
                }

                if (!matched)
                    Debug.LogWarning($"[DemoMap]   '{name}' は DemoScripts にも HideCharacters にも一致しない");
            }

            // 容器を再表示
            if (s.iconParent != null)
                s.iconParent.gameObject.SetActive(true);
        }

        Debug.Log("[DemoMap] Setup 完了");
    }
}
