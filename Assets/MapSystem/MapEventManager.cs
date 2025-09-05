// Assets/MapSystem/MapEventManager.cs
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-800)]  // 盡量提早跑，先把依賴補好
public class MapEventManager : MonoBehaviour
{
    [Header("基本設定")]
    [Tooltip("這個腳本預期運作的地圖場景名。")]
    public string mapSceneName = "MapTest";

    [Tooltip("進場時是否從 PlayerPrefs 讀取 MapIsDay 並套用日/夜。")]
    public bool applyDayNightOnStart = true;

    [Header("相依物件（可不填，會自動尋找）")]
    public MonoBehaviour mapBackgroundController; // 你們的 MapBackgroundController（或等價腳本）
    public Camera mainCamera;

    // 反射快取
    MethodInfo setDayModeMI;
    MethodInfo applyDayNightMI;

    void Awake()
    {
        // 僅在指定地圖場景運作；其他場景直接停用自己
        var active = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(mapSceneName) && active != mapSceneName)
        {
            Debug.Log("[MapEventManager] 當前場景 '" + active + "' 非地圖 '" + mapSceneName + "'，停用自身。");
            enabled = false;
            return;
        }

        // 尋找 Map 背景控制器（舊版相容，不用 includeInactive 多載）
        if (mapBackgroundController == null)
            mapBackgroundController = ResolveBackgroundController();

        // 找主相機（多層後備，避免拿不到）
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var camObj = UnityEngine.Object.FindObjectOfType(typeof(Camera)) as Camera; // 只找啟用的
            if (camObj != null) mainCamera = camObj;
        }
        if (mainCamera == null)
        {
            // 最後後備：抓任何（含停用）的 Camera
            var allCams = Resources.FindObjectsOfTypeAll(typeof(Camera)) as UnityEngine.Object[];
            if (allCams != null && allCams.Length > 0) mainCamera = allCams[0] as Camera;
        }

        if (mapBackgroundController == null)
        {
            Debug.LogError("[MapEventManager] 缺少 MapBackgroundController；停用自身避免 NRE。");
            enabled = false; return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("[MapEventManager] 找不到任何 Camera（先繼續；你的地圖可能全 UI 或之後會生成）。");
        }

        // 反射抓方法（兩種名稱都支援）
        var t = mapBackgroundController.GetType();
        setDayModeMI = t.GetMethod("SetDayMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null);
        applyDayNightMI = t.GetMethod("ApplyDayNight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null);

        if (setDayModeMI == null && applyDayNightMI == null)
        {
            Debug.LogWarning("[MapEventManager] 在 " + t.Name + " 找不到 SetDayMode(bool)/ApplyDayNight(bool)；之後僅記錄狀態不呼叫。");
        }

        Debug.Log("[MapEventManager] Awake 完成：依賴就緒（Controller=" + t.Name + "）。");
    }

    void Start()
    {
        try
        {
            if (!applyDayNightOnStart)
            {
                Debug.Log("[MapEventManager] applyDayNightOnStart=false，略過日/夜套用。");
                return;
            }

            // 讀 PlayerPrefs
            bool isDay = PlayerPrefs.GetInt("MapIsDay", 1) == 1;
            Debug.Log("[MapEventManager] 讀取 MapIsDay = " + isDay);

            // 嘗試呼叫 SetDayMode / ApplyDayNight
            if (setDayModeMI != null)
            {
                setDayModeMI.Invoke(mapBackgroundController, new object[] { isDay });
                Debug.Log("[MapEventManager] SetDayMode 呼叫完成。");
            }
            else if (applyDayNightMI != null)
            {
                applyDayNightMI.Invoke(mapBackgroundController, new object[] { isDay });
                Debug.Log("[MapEventManager] ApplyDayNight 呼叫完成。");
            }
            else
            {
                Debug.LogWarning("[MapEventManager] 沒有可呼叫的方法（SetDayMode/ApplyDayNight）；僅記錄狀態。");
            }

            // 相機保險（避免 Game 視窗黑）
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
                mainCamera.targetDisplay = 0;           // Display 1
                if (mainCamera.clearFlags == CameraClearFlags.Nothing)
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                if (mainCamera.cullingMask == 0)
                    mainCamera.cullingMask = ~0;       // 若剛好是 0，先全開
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[MapEventManager] Start 例外，已阻擋避免 NRE：\n" + e);
            enabled = false; // 不再重複炸
        }
    }

    // —— Helper（舊版相容的 Controller 尋找器）——
    MonoBehaviour ResolveBackgroundController()
    {
        // 1) 優先找名為 MapBackgroundController 的元件（若你們類名就是這個）
        Type t = Type.GetType("MapBackgroundController");
        if (t != null)
        {
            var comp = UnityEngine.Object.FindObjectOfType(t) as MonoBehaviour; // 只找啟用的
            if (comp != null) return comp;

            // 後備：抓任何（含停用）
            var all = Resources.FindObjectsOfTypeAll(t);
            if (all != null && all.Length > 0) return all[0] as MonoBehaviour;
        }

        // 2) 廣義搜尋：找到任何擁有 SetDayMode(bool)/ApplyDayNight(bool) 的 MonoBehaviour
        var any = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));
        if (any != null)
        {
            for (int i = 0; i < any.Length; i++)
            {
                var mb = any[i] as MonoBehaviour;
                if (mb == null) continue;
                var tp = mb.GetType();
                var hasSet = tp.GetMethod("SetDayMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null) != null;
                var hasApply = tp.GetMethod("ApplyDayNight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null) != null;
                if (tp.Name == "MapBackgroundController" || hasSet || hasApply)
                    return mb;
            }
        }

        // 3) 仍找不到就回傳 null
        return null;
    }
}
