using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：隱藏 MapEventManager 按鈕，把所有小人的 call911 腳本改成 demo 專用。
/// </summary>
[DefaultExecutionOrder(-100)]   // 確保在 MapCharacterSpawner 之前執行
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

    MapCharacterSpawner _spawner;

    void Awake()
    {
        // Spawner の GameObject ごと非活性化 → Start() が走らないので icon が生成されない
        _spawner = FindObjectOfType<MapCharacterSpawner>();
        if (_spawner != null)
        {
            _spawner.gameObject.SetActive(false);
            Debug.Log("[DemoMap] MapCharacterSpawner 停用（icon 生成防止）");
        }
        else
        {
            Debug.LogError("[DemoMap] MapCharacterSpawner が見つかりません");
        }
    }

    void Start()
    {
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null) { mgr.gameObject.SetActive(false); Debug.Log("[DemoMap] MapEventManager 已隱藏"); }

        StartCoroutine(OverrideCharacterScripts());
    }

    IEnumerator OverrideCharacterScripts()
    {
        if (_spawner == null) yield break;

        // 1. Spawner 再活性化 → Start() は次フレームに予約される
        _spawner.gameObject.SetActive(true);

        // 2. iconParent をその場で非表示（Start() より先に実行される）
        var iconParent = _spawner.iconParent;
        if (iconParent != null)
            iconParent.gameObject.SetActive(false);
        else
            Debug.LogError("[DemoMap] iconParent が null です。MapCharacterSpawner の iconParent を Inspector で設定してください");

        // 3. Spawner Start() が走り、call911 が掛かるまで待つ
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // 4. 不要な icon を非表示
        if (iconParent != null)
        {
            foreach (Transform child in iconParent)
            {
                foreach (var hide in HideCharacters)
                {
                    if (child.name.Contains(hide))
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"[DemoMap] 隱藏：{child.name}");
                        break;
                    }
                }
            }

            // 5. iconParent を再表示（Mel/Eupie のみ見える）
            iconParent.gameObject.SetActive(true);
        }

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

}
