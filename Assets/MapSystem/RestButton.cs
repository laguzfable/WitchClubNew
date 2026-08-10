using UnityEngine;

public class RestButton : MonoBehaviour
{
    // 除錯熱鍵：不管日夜，數字鍵盤的 . 直接跳過這次地圖，等同按了「回去」按鈕
    void Update ()
    {
        if (Input.GetKeyUp(KeyCode.KeypadPeriod))
            OnRestButtonClicked();
    }

    public void OnRestButtonClicked ()
    {
        if (!MapReturnPoint.HasValid()) return;

        var loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("[RestButton] 找不到 SceneLoader！");
            return;
        }

        loader.GotoScript(MapReturnPoint.ScriptName, MapReturnPoint.Label);
        // 返回點已交給 GotoScript（走 ExplicitGotoPending 路徑），用掉就清
        MapReturnPoint.Clear();
    }
}
