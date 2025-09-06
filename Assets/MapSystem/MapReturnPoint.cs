using UnityEngine;

public static class MapReturnPoint
{
    public static string ScriptName { get; private set; }
    public static string Label      { get; private set; }

    const string KeyScript = "MapReturnPoint.Script";
    const string KeyLabel  = "MapReturnPoint.Label";

    static MapReturnPoint()
    {
        ScriptName = PlayerPrefs.GetString(KeyScript, "");
        Label      = PlayerPrefs.GetString(KeyLabel, "");
        Debug.Log($"[MRP] static ctor -> PlayerPrefs load Script='{ScriptName}', Label='{Label}'");
    }

    public static void Set(string script, string label)
    {
        ScriptName = script ?? "";
        Label      = label ?? "";
        PlayerPrefs.SetString(KeyScript, ScriptName);
        PlayerPrefs.SetString(KeyLabel,  Label);
        PlayerPrefs.Save();
        Debug.Log($"[MRP] Set -> Script='{ScriptName}', Label='{Label}'");
    }

    public static bool HasValid()
    {
        var ok = !string.IsNullOrEmpty(ScriptName);
        Debug.Log($"[MRP] HasValid? {ok} (Script='{ScriptName}', Label='{Label}')");
        return ok;
    }

    public static void Clear()
    {
        ScriptName = "";
        Label      = "";
        PlayerPrefs.DeleteKey(KeyScript);
        PlayerPrefs.DeleteKey(KeyLabel);
        Debug.Log("[MRP] Cleared.");
    }
}
