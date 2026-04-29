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

    MapCharacterSpawner _spawner;

    void Awake()
    {
        // 在小人生成前就把 iconParent 整個關掉，完全避免任何閃爍
        _spawner = FindObjectOfType<MapCharacterSpawner>();
        if (_spawner != null && _spawner.iconParent != null)
        {
            _spawner.iconParent.gameObject.SetActive(false);
            Debug.Log("[DemoMap] iconParent 預先隱藏");
        }
    }

    void Start()
    {
        // 隱藏 MapEventManager 的按鈕列
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null)
        {
            mgr.gameObject.SetActive(false);
            Debug.Log("[DemoMap] MapEventManager 已隱藏");
        }

        StartCoroutine(OverrideCharacterScripts());
    }

    IEnumerator OverrideCharacterScripts()
    {
        // 等幾幀讓 MapCharacterSpawner 生成完、call911 全部掛好
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // 隱藏不需要的角色 icon
        if (_spawner != null && _spawner.iconParent != null)
        {
            foreach (Transform child in _spawner.iconParent)
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
        }

        // iconParent 重新顯示（Mel/Eupie 的 icon 會跟著出現）
        if (_spawner != null && _spawner.iconParent != null)
            _spawner.iconParent.gameObject.SetActive(true);

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
