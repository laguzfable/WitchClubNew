using Naninovel;
using UnityEngine;

/// <summary>
/// Naninovel 在 CameraManager.InitializeMainCamera() 裡自己 new 出來的相機只設了
/// backgroundColor，沒有設 clearFlags，所以吃到 Unity 的預設值 Skybox。
/// 那台相機 depth = 0、cullingMask 是「除了 UI 層以外全部」，會蓋掉場景自己的
/// Main Camera（depth = -1）——也就是說只要畫面上沒有背景演員蓋滿，
/// 玩家看到的就是 Unity 的預設天空盒（藍灰色）。
///
/// 最明顯的地方是按「新遊戲」：Title 的 UI 一關掉、狀態重置完到劇本第一句
/// @back 進場之間有幾幀什麼背景都沒有，天空盒就穿幫閃一下。
/// 這裡把 clearFlags 改成純黑，那段空窗看到的是黑畫面（VN 本來就該是黑的），
/// 順便也修掉背景切換／@back 空檔的同一個問題。
/// </summary>
public static class NaniCameraClearFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install ()
    {
        Engine.OnInitializationFinished -= Apply;
        Engine.OnInitializationFinished += Apply;
        // 編輯器裡重跑（或引擎已經先初始化完）時補一次。
        if (Engine.Initialized) Apply();
    }

    private static void Apply ()
    {
        var camera = Engine.GetService<ICameraManager>()?.Camera;
        if (camera == null)
        {
            Debug.LogWarning("[NaniCameraClearFix] 拿不到 Naninovel 相機，天空盒穿幫的修正沒生效。");
            return;
        }

        // CameraManager.ResetService() 只會還原相機上的 MonoBehaviour 特效，
        // 不會動 clearFlags，所以設一次就會一直有效（含 ResetStateAsync 之後）。
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }
}
