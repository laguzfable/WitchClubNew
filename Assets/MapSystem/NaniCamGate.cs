using UnityEngine;
using Naninovel;

public class NaniCamGate : MonoBehaviour
{
    public enum Mode { Disable, Mask, MoveToDisplay2 }
    [Tooltip("Disable: 關閉 UICamera；Mask: 不渲染任何層；MoveToDisplay2: 丟到 Display2。")]
    public Mode mode = Mode.Disable;

    Camera naniCam; bool prevEnabled; int prevMask; int prevDisp; CameraClearFlags prevClear;

    void Awake()
    {
        try
        {
            var camMgr = Engine.GetService<ICameraManager>();
            naniCam = camMgr != null ? camMgr.Camera : null;
            if (naniCam == null) { Debug.Log("[NaniCamGate] 沒找到 UICamera（可忽略）。"); return; }

            // 記錄舊值
            prevEnabled = naniCam.enabled;
            prevMask    = naniCam.cullingMask;
            prevDisp    = naniCam.targetDisplay;
            prevClear   = naniCam.clearFlags;

            switch (mode)
            {
                case Mode.Disable:
                    naniCam.enabled = false;
                    Debug.Log("[NaniCamGate] UICamera disabled.");
                    break;
                case Mode.Mask:
                    naniCam.cullingMask = 0;
                    Debug.Log("[NaniCamGate] UICamera cullingMask=0。");
                    break;
                case Mode.MoveToDisplay2:
                    naniCam.targetDisplay = 1; // Display 2 （Display 1 是 0）
                    Debug.Log("[NaniCamGate] UICamera moved to Display2.");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[NaniCamGate] 例外：" + ex.Message);
        }
    }

    void OnDestroy()
    {
        if (naniCam == null) return;
        naniCam.enabled       = prevEnabled;
        naniCam.cullingMask   = prevMask;
        naniCam.targetDisplay = prevDisp;
        naniCam.clearFlags    = prevClear;
        Debug.Log("[NaniCamGate] UICamera restored.");
    }
}
