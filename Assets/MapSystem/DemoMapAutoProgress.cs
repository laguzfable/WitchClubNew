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
        Debug.Log($"[DemoMap] Awake ── 找到 {_spawners.Length} 個 Spawner");

        // Spawner は止めない（iconParent が自身と同じ可能性があるため）
        // 代わりに CanvasGroup で視覚的に隱す
        foreach (var s in _spawners)
        {
            var container = s.iconParent != null ? s.iconParent.gameObject : s.gameObject;
            var cg = container.GetComponent<CanvasGroup>() ?? container.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            Debug.Log($"[DemoMap]   隱藏 container：{container.name}");
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
        // Spawner.Start() → CreateCharacterIcon → call911.Start() が全部終わるまで待つ
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.3f);

        foreach (var s in _spawners)
        {
            if (s == null) continue;
            Transform container = s.iconParent != null ? s.iconParent : s.transform;
            Debug.Log($"[DemoMap] Spawner={s.gameObject.name}  container={container.name}  子={container.childCount}");

            var children = new List<Transform>();
            foreach (Transform c in container) children.Add(c);

            foreach (var child in children)
            {
                // 動的生成された Icon_* だけ処理（CharacterIcon / MapBG 等はスキップ）
                if (!child.name.StartsWith("Icon_"))
                {
                    Debug.Log($"[DemoMap]   スキップ（Icon_ 以外）：{child.name}");
                    continue;
                }

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

                    var c911 = child.GetComponentInChildren<call911>(true);
                    if (c911 != null) c911.enabled = false;

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
                    else Debug.LogWarning($"[DemoMap]   Button 找不到：{name}");
                    break;
                }

                if (!matched)
                    Debug.LogWarning($"[DemoMap]   '{name}' 沒有對應的腳本");
            }

            // CanvasGroup を戻す（Vedia/Nelly は SetActive=false 済なので見えない）
            var containerGO = s.iconParent != null ? s.iconParent.gameObject : s.gameObject;
            var cg = containerGO.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        Debug.Log("[DemoMap] Setup 完了");
    }
}
