using Naninovel;

[CommandAlias("saveMapDayNight")]
public class SaveMapDayNightCommand : Command
{
    [ParameterAlias("isDay"), RequiredParameter]
    public BooleanParameter isDay;
    public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        int val = isDay.Value ? 1 : 0;
        UnityEngine.PlayerPrefs.SetInt("MapIsDay", val);
        UnityEngine.PlayerPrefs.SetInt("IsDay", val);   // MapEventManager 讀這個
        UnityEngine.PlayerPrefs.Save();
        return UniTask.CompletedTask;
    }
}
