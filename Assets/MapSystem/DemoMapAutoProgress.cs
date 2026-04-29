using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 MapTest 場景任意 GameObject 上。
/// Demo 模式：隱藏 MapEventManager 按鈕，把所有小人的 call911 腳本改成 demo 專用。
/// </summary>
[DefaultExecutionOrder(-100)]
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
        // 全ての MapCharacterSpawner を無効化して icon 生成をブロック
        _spawners = FindObjectsOfType<MapCharacterSpawner>();
        Debug.Log($"[DemoMap] Awake: {_spawners.Length} 個の Spawner を停用");
        foreach (var s in _spawners)
            s.gameObject.SetActive(false);
    }

    void Start()
    {
        var mgr = FindObjectOfType<MapEventManager>();
        if (mgr != null) { mgr.gameObject.SetActive(false); Debug.Log("[DemoMap] MapEventManager 隱藏"); }

        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        // 全 Spawner を再有効化、同フレームで iconParent を非表示
        foreach (var s in _spawners)
        {
            if (s == null) continue;
            var ip = s.iconParent;
            s.gameObject.SetActive(true);               // Start() は次フレーム以降
            if (ip != null) ip.gameObject.SetActive(false);
        }

        // Spawner の Start() + CreateCharacterIcon coroutine + call911.Start() が全部終わるまで待つ
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.3f);

        // --- 全 call911 を処理 ---
        // call911.Start() が AddListener した後に、全リスナーを削除して demo 用に差し替える
        var all911 = FindObjectsOfType<call911>(true);   // inactive も含む
        Debug.Log($"[DemoMap] call911 {all911.Length} 個");

        foreach (var c911 in all911)
        {
            // icon ルートの名前を取得
            string iconName = GetIconRootName(c911.transform);
            Debug.Log($"[DemoMap] 処理: '{iconName}'");

            // 非 demo キャラ → icon ごと隠す
            bool hide = false;
            foreach (var h in HideCharacters)
            {
                if (iconName.Contains(h)) { hide = true; break; }
            }
            if (hide)
            {
                Transform root = GetIconRoot(c911.transform);
                if (root != null) root.gameObject.SetActive(false);
                Debug.Log($"[DemoMap] 隱藏: {iconName}");
                continue;
            }

            // demo キャラ → call911 を無効化 + onClick を差し替え
            foreach (var kv in DemoScripts)
            {
                if (!iconName.Contains(kv.Key)) continue;

                var script = kv.Value;
                c911.enabled = false;   // call911.CallNaniScript が呼ばれないようにする

                var btn = c911.GetComponent<Button>();
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
                    Debug.Log($"[DemoMap] {iconName} → {script}");
                }
                break;
            }
        }

        // iconParent を再表示（隱したもの以外が見える）
        foreach (var s in _spawners)
        {
            if (s == null || s.iconParent == null) continue;
            s.iconParent.gameObject.SetActive(true);
        }

        Debug.Log("[DemoMap] Setup 完了");
    }

    static string GetIconRootName(Transform t)
    {
        while (t != null)
        {
            if (t.name.StartsWith("Icon_")) return t.name;
            t = t.parent;
        }
        return "";
    }

    static Transform GetIconRoot(Transform t)
    {
        while (t != null)
        {
            if (t.name.StartsWith("Icon_")) return t;
            t = t.parent;
        }
        return null;
    }
}
