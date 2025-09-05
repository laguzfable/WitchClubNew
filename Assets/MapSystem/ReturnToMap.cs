using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMap : MonoBehaviour
{
    // 在 Unity Inspector 中設定你的地圖場景名稱
    public string mapSceneName = "MapTest";

    // 選擇性地，設定一個 Naniscript 腳本名稱
    public string naniscriptName = "";

    /// <summary>
    /// 當按鈕被點擊時呼叫此方法。
    /// </summary>
    public void OnReturnButtonClicked()
    {
        // 啟動一個協程來處理場景載入和延遲，確保畫面不會殘留。
        StartCoroutine(LoadSceneAndPlayScript());
    }

    private IEnumerator LoadSceneAndPlayScript()
    {
        // 第一步：載入地圖場景。
        // 這個指令會立刻開始場景切換，但不會等到渲染完成。
        SceneManager.LoadScene(mapSceneName);

        // 第二步：等待一幀。
        // 這是關鍵步驟，它讓 Unity 有時間渲染新場景，
        // 確保地圖畫面完全載入後再執行後續指令，避免畫面殘留。
        yield return null;

        // 第三步：如果設定了 Naniscript 腳本名稱，則執行它。
        if (!string.IsNullOrEmpty(naniscriptName))
        {
            // IMPORTANT: Naniscript 舊版本可能沒有統一的 API。
            // 這裡你需要根據你的 Naniscript 版本來修改程式碼。
            //
            // 常見的呼叫方式可能是：
            // Naniscript.PlayScript(naniscriptName);
            //
            // 如果上述方法無效，你可能需要先找到 Naniscript 的管理器物件：
            // var naniManager = GameObject.Find("NaniManager").GetComponent<Naniscript.NaniManager>();
            // if (naniManager != null)
            // {
            //     naniManager.StartScript(naniscriptName);
            // }

            // 為了這個範例，我們只會列印一條訊息，請在這裡替換成你實際的呼叫程式碼。
            Debug.Log("成功切換至地圖，並正在嘗試執行 Naniscript 腳本：" + naniscriptName);
        }
    }
}
