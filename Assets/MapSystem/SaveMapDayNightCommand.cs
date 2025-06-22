using Naninovel;

[CommandAlias("saveMapDayNight")]
public class SaveMapDayNightCommand : Command
{
    [ParameterAlias("isDay"), RequiredParameter]
    public BooleanParameter isDay;
    public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
    {
        UnityEngine.PlayerPrefs.SetInt("MapIsDay", isDay.Value ? 1 : 0);
        UnityEngine.PlayerPrefs.Save();
        return UniTask.CompletedTask;
    }
}
