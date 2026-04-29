using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：Mel / Eupie だけ表示、全ての call911 を処理。
/// </summary>
[DefaultExecutionOrder(-200)]
public class DemoMapAutoProgress : MonoBehaviour
{
    // 白天：Mel / Eupie 出現
    static readonly Dictionary<string, string> DayCharToScript = new Dictionary<string, string>
    {
        { "Mei",    "demo_mei"    }, { "Mel",    "demo_mei"    },
        { "Euphie", "demo_euphie" }, { "Eupie",  "demo_euphie" },
        { "魅兒",   "demo_mei"    }, { "優菲",   "demo_euphie" },
    };
    static readonly HashSet<string> DayHideChars = new HashSet<string>
    {
        "Vivia", "Vedia", "薇狄亞", "Nelly", "涅莉",
    };

    // 晚上：Nelly / Vedia 出現
    static readonly Dictionary<string, string> NightCharToScript = new Dictionary<string, string>
    {
        { "Nelly",  "demo_nelly" }, { "涅莉",   "demo_nelly" },
        { "Vivia",  "demo_vedia" }, { "Vedia",  "demo_vedia" }, { "薇狄亞", "demo_vedia" },
    };
    static readonly HashSet<string> NightHideChars = new HashSet<string>
    {
        "Mel", "Mei", "魅兒", "Euphie", "Eupie", "優菲",
    };

    Dictionary<string, string> CharToScript;
    HashSet<string> HideChars;

    void Start()
    {
        bool isDay = PlayerPrefs.GetInt("MapIsDay", 1) == 1;
        CharToScript = isDay ? DayCharToScript : NightCharToScript;
        HideChars    = isDay ? DayHideChars    : NightHideChars;
        Debug.Log($"[DemoMap] 時段：{(isDay ? "白天(Mel/Eupie)" : "晚上(Nelly/Vedia)")}");

        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null) { mgr.gameObject.SetActive(false); Debug.Log("[DemoMap] MapEventManager 隱藏"); }
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        // ── Phase 1：1 幀後立刻隱藏 Vedia/Nelly 的 Icon_* ──────────
        // Spawner 的 Instantiate 在 frame 0 Update 裡就完成，
        // 所以 1 幀後 Icon_* 已存在，不需要等 call911
        yield return null;
        HideNonDemoIcons();

        // ── Phase 2：等 call911.Start() 全部跑完再替換 onClick ──────
        yield return null; yield return null; yield return null;
        yield return new WaitForSeconds(0.3f);

        // ── Step 1：從 Spawner 的 characterEventTable 建立 naniScript → 角色名 逆引き ──
        var naniToChar = new Dictionary<string, string>();
        var spawners = FindObjectsOfType<MapCharacterSpawner>();
        Debug.Log($"[DemoMap] 找到 {spawners.Length} 個 Spawner");
        foreach (var sp in spawners)
        {
            foreach (var entry in sp.characterEventTable)
            {
                foreach (var evt in entry.dayEvents)
                    if (!string.IsNullOrEmpty(evt.naninovelScript))
                        naniToChar[evt.naninovelScript] = entry.characterName;
                foreach (var evt in entry.nightEvents)
                    if (!string.IsNullOrEmpty(evt.naninovelScript))
                        naniToChar[evt.naninovelScript] = entry.characterName;
            }
        }
        Debug.Log($"[DemoMap] naniToChar 表：{string.Join(", ", naniToChar.Count > 0 ? new List<string>(naniToChar.Keys) : new List<string>{"(空)"} )}");

        // ── Step 2：全シーンの call911 を処理 ──
        var all911 = FindObjectsOfType<call911>();
        Debug.Log($"[DemoMap] 全シーンの call911：{all911.Length} 個");

        foreach (var c911 in all911)
        {
            // 角色名を特定：階層名 → naniToChar の順
            string charName = FindCharInHierarchy(c911.transform);
            if (string.IsNullOrEmpty(charName))
                naniToChar.TryGetValue(c911.劇本名, out charName);

            Debug.Log($"[DemoMap] call911 on '{c911.gameObject.name}'  劇本='{c911.劇本名}'  → 角色='{charName}'");

            // Demo 角色 → 替換 onClick
            if (!string.IsNullOrEmpty(charName) && CharToScript.TryGetValue(charName, out var demoScript))
            {
                var btn = c911.GetComponent<Button>();
                if (btn == null) btn = c911.GetComponentInParent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    var script = demoScript;
                    btn.onClick.AddListener(() =>
                    {
                        Debug.Log($"[DemoMap] 點擊 → {script}");
                        MapReturnPoint.Set(script, "");
                        var ds = DataService.Instance;
                        if (ds != null)
                        {
                            ds.startScript     = script;
                            ds.scriptParameter = new ScriptParameter { scriptName = script };
                        }
                        UnityEngine.SceneManagement.SceneManager.LoadScene("NaniDialogTest");
                    });
                    c911.enabled = false;
                    Debug.Log($"[DemoMap]   → demo 腳本替換：{demoScript}  Button='{btn.gameObject.name}'");
                }
                else Debug.LogWarning($"[DemoMap]   Button 找不到（call911 on {c911.gameObject.name}）");
                continue;
            }

            // 非 Demo 角色 or 不明 → icon root を非表示
            var iconRoot = FindIconRoot(c911.transform);
            if (iconRoot != null)
            {
                iconRoot.gameObject.SetActive(false);
                Debug.Log($"[DemoMap]   → 隱藏 icon root：'{iconRoot.name}'");
            }
            else
            {
                // icon root が見つからない場合は call911 自体を無効化
                c911.enabled = false;
                var btn = c911.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();
                Debug.LogWarning($"[DemoMap]   icon root 找不到、call911 停用：{c911.gameObject.name}");
            }
        }

        Debug.Log("[DemoMap] 完成");
    }

    // Phase 1：Spawner 容器の Icon_* 子物件を走査して非 Demo 角色を即時隱藏
    void HideNonDemoIcons()
    {
        var spawners = FindObjectsOfType<MapCharacterSpawner>();
        foreach (var sp in spawners)
        {
            var container = sp.iconParent != null ? sp.iconParent : sp.transform;
            foreach (Transform child in container)
            {
                if (!child.name.StartsWith("Icon_")) continue;
                foreach (var h in HideChars)
                {
                    if (child.name.Contains(h))
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"[DemoMap] Phase1 即時隱藏：{child.name}");
                        break;
                    }
                }
            }
        }
    }

    // 階層を上に向かって走査し、CharToScript / HideChars に一致する名前を探す
    static string FindCharInHierarchy(Transform t)
    {
        while (t != null)
        {
            string n = t.name;
            foreach (var key in CharToScript.Keys)
                if (n.Contains(key)) return key;
            foreach (var key in HideChars)
                if (n.Contains(key)) return key;
            t = t.parent;
        }
        return "";
    }

    // 「Icon_」で始まる最初の祖先を返す。なければ call911 の一つ上の親を返す
    static Transform FindIconRoot(Transform t)
    {
        var orig = t;
        while (t != null)
        {
            if (t.name.StartsWith("Icon_")) return t;
            t = t.parent;
        }
        // fallback：call911 の直接の親
        return orig.parent;
    }
}
