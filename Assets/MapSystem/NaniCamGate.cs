using UnityEngine;
using Naninovel;

[DefaultExecutionOrder(-1000)]
public class NaniCamGate : MonoBehaviour
{
    public enum Mode { Disable, Mask, MoveToDisplay2 }
    [Tooltip("Disable：關掉 UICamera 的渲染；Mask：不渲染任何層(已修正不清畫面)；MoveToDisplay2：丟到 Display2。")]
    public Mode mode = Mode.Disable;

    // —— UICamera 狀態備份 ——
    Camera naniCam;
    bool prevEnabled; int prevMask; int prevDisp; CameraClearFlags prevClear;

    // —— UICamera 的 AudioListener（回小說要還給它）——
    AudioListener naniListener;             // UICamera 上的 AL（原有或我們暫時加的）
    bool naniListenerExisted;               // 進地圖前是否本來就有
    bool naniListenerPrevEnabled;

    // —— 地圖期間的 AudioListener（掛在地圖主相機或臨時物件）——
    AudioListener mapListenerAdded;         // 只有「我們新增」時才會記住，回小說時移除
    Camera mapCamUsed;                      // 我們選作聲音主控的相機（如有）

    void Awake()
    {
        // 1) 找到 Naninovel UICamera
        var camMgr = Engine.GetService<ICameraManager>();
        naniCam = camMgr != null ? camMgr.Camera : null;

        if (naniCam != null)
        {
            // 備份 UICamera 狀態
            prevEnabled = naniCam.enabled;
            prevMask    = naniCam.cullingMask;
            prevDisp    = naniCam.targetDisplay;
            prevClear   = naniCam.clearFlags;

            // 取/建 UICamera 的 AudioListener（之後回小說要交還給它）
            naniListener = naniCam.GetComponent<AudioListener>();
            if (naniListener != null) { naniListenerExisted = true; naniListenerPrevEnabled = naniListener.enabled; }
            else
            {
                // 若本來沒有，先加一顆但暫時關閉；回小說時再打開
                naniListener = naniCam.gameObject.AddComponent<AudioListener>();
                naniListener.enabled = false;
                naniListenerExisted = false;
                naniListenerPrevEnabled = false;
            }

            // 2) 視模式處理 UICamera 的「畫面」行為
            switch (mode)
            {
                case Mode.Disable:
                    // 完全不渲染，避免蓋畫面；聲音交給地圖主相機
                    naniCam.enabled = false;
                    break;

                case Mode.Mask:
                    // 不渲染任何層 + 不清畫面 → 不會黑屏
                    naniCam.cullingMask = 0;
                    naniCam.clearFlags  = CameraClearFlags.Nothing;
                    break;

                case Mode.MoveToDisplay2:
                    naniCam.targetDisplay = 1; // Display2（0 是 Display1）
                    break;
            }

            Debug.Log("[NaniCamGate] UICamera handled: " + mode);
        }
        else
        {
            Debug.Log("[NaniCamGate] 沒找到 UICamera（可忽略）。");
        }

        // 3) 進地圖時，保證「當前場景」有 1 顆可用的 AudioListener（交給地圖主相機）
        EnsureMapAudioListener();
        // 並且把 UICamera 上的 listener 關掉，避免同時兩顆
        if (naniListener != null) naniListener.enabled = false;
    }

    void OnDestroy()
    {
        // —— 還原 UICamera 的渲染設定 ——
        if (naniCam != null)
        {
            naniCam.enabled       = prevEnabled;
            naniCam.cullingMask   = prevMask;
            naniCam.targetDisplay = prevDisp;
            naniCam.clearFlags    = prevClear;
            Debug.Log("[NaniCamGate] UICamera restored.");
        }

        // —— 把聲音主控「交還」給 UICamera —— 
        if (naniListener != null)
        {
            // 回小說時，至少讓 UICamera 上有 1 顆啟用的 AL，避免 0 顆刷警告
            // 若原本就有且原本是開的 → 開；若原本沒或是關的 → 開（保底）
            naniListener.enabled = true;
        }

        // —— 移除我們在地圖加的那顆 listener（若有加） —— 
        if (mapListenerAdded != null)
        {
            Destroy(mapListenerAdded);
            mapListenerAdded = null;
            mapCamUsed = null;
        }
        // 若地圖主相機本來就有 listener（我們沒加），就不主動動它；通常 Single 切場會把地圖相機銷毀，不會造成兩顆。
    }

    // ========== Helpers ==========

    void EnsureMapAudioListener()
    {
        // 優先選擇地圖主相機（Main or 任一啟用相機）
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        mapCamUsed = cam;

        if (cam != null)
        {
            var al = cam.GetComponent<AudioListener>();
            if (al == null)
            {
                // 地圖主相機沒有 → 我們加一顆並啟用（回小說時會移除）
                mapListenerAdded = cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[NaniCamGate] Attached AudioListener to map camera: " + cam.name);
            }
            else
            {
                // 已經有 → 確認啟用；我們不記錄（代表不是我們加的）
                if (!al.enabled) al.enabled = true;
                mapListenerAdded = null;
                Debug.Log("[NaniCamGate] Using existing AudioListener on map camera: " + cam.name);
            }
        }
        else
        {
            // 真的找不到相機：建立一個臨時物件掛 AL，避免刷屏
            var go = new GameObject("MapTempAudioListener");
            go.transform.SetParent(this.transform, false);
            mapListenerAdded = go.AddComponent<AudioListener>();
            Debug.LogWarning("[NaniCamGate] No camera found in map; created temporary AudioListener.");
        }
    }
}
