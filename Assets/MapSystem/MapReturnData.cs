using Naninovel;

public static class MapReturnData
{
    public static string ScriptName;
    public static int LineIndex;

    public static void Remember ()
    {
        var player = Engine.GetService<IScriptPlayer>();
        ScriptName = player.PlayedScript?.Name;   // 改成 PlayedScript
        LineIndex  = player.PlaybackSpot.LineIndex;
    }
}
