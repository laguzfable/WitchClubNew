using Naninovel;
using UnityEngine;

[CommandAlias("setDayMode")] // 小寫，劇本也是 @setDayMode
public class SetDayModeCommand : Command
{
    [ParameterAlias("isDay"), RequiredParameter]
    public BooleanParameter isDay; // 小寫，呼叫時用 isDay:false

    public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        Debug.Log($"[Nani] SetDayMode 執行！參數 isDay={isDay.Value}");
        var controller = Object.FindObjectOfType<MapBackgroundController>();
        if (controller != null)
        {
            controller.SetDayMode(isDay.Value);
            Debug.Log($"[Nani] MapBackgroundController.SetDayMode({isDay.Value}) 執行完成。");
        }
        else
        {
            Debug.LogWarning("[Nani] 找不到 MapBackgroundController！");
        }
        return UniTask.CompletedTask;
    }
}
