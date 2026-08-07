using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// @GoBackToStory
/// 回到用 @SaveReturnPoint 存下的主線劇本位置（跟休息室/符文站的「返回」按鈕走同一條路）。
/// 給地圖上點角色圖示觸發的事件（如白天寵物事件）結尾用，取代 @stop。
/// </summary>
[CommandAlias("GoBackToStory")]
public class GoBackToStory : Command
{
    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Debug.Log($"🐾🐾🐾 [PETDAY] @GoBackToStory 執行了！目前存的返回點 = '{MapReturnPoint.ScriptName}'#'{MapReturnPoint.Label}' (HasValid={MapReturnPoint.HasValid()})");

        try
        {
            NaniBridgeUtility.GoBackToSavedStory();
            Debug.Log("🐾🐾🐾 [PETDAY] GoBackToSavedStory() 呼叫完成，沒有丟例外。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🐾🐾🐾 [PETDAY] GoBackToSavedStory() 丟例外了：{e}");
        }

        return UniTask.CompletedTask;
    }
}
