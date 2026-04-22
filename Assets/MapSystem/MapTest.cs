using UnityEngine;
using Naninovel;
using Naninovel.UI;

public class MapTest : MonoBehaviour
{
    [SerializeField] private Camera mapCamera;

    void Start()
    {
        // 直接從 PlayerPrefs 讀，預設 1=白天
        bool isDay = PlayerPrefs.GetInt("MapIsDay", 1) == 1;

        Debug.Log("[MapTest] 啟動 MapTest，isDay=" + isDay);

        Engine.GetService<IUIManager>().SetUIVisibleWithToggle(false);

        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        var continueUI = Object.FindObjectOfType<ContinueInputUI>();
        if (continueUI != null)
            continueUI.Visible = false;

        var printerMgr = Engine.GetService<ITextPrinterManager>();
        try
        {
            var dialoguePrinter = printerMgr?.GetActor(printerMgr.DefaultPrinterId);
            if (dialoguePrinter != null)
                dialoguePrinter.Visible = false;
        }
        catch { }

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;

        if (mapCamera != null)
            mapCamera.enabled = true;

        var bg = FindObjectOfType<MapBackgroundController>();
        if (bg != null)
            bg.SetDayMode(isDay);
        else
            Debug.LogWarning("找不到 MapBackgroundController！");
    }
}
