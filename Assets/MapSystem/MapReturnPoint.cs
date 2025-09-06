using UnityEngine;

/// <summary>
/// 主線返回點（script + label）的獨立儲存處，避免被 afterChat 或戰鬥流程覆蓋。
/// 使用靜態欄位 + PlayerPrefs 雙保險。@SaveReturnPoint 指令會寫入這裡。
/// </summary>
public static class MapReturnPoint
{
    public static string ScriptName { get; private set; }
    public static string Label      { get; private set; }

    private const string KeyScript = "MapReturnPoint.Script";
    private const string KeyLabel  = "MapReturnPoint.Label";

    static MapReturnPoint ()
    {
        ScriptName = PlayerPrefs.GetString(KeyScript, "");
        Label      = PlayerPrefs.GetString(KeyLabel, "");
        Debug.Log($"[MRP] static ctor -> PlayerPrefs load Script='{ScriptName}', Label='{Label}'");
    }

    public static void Set (string script, string label)
    {
        ScriptName = script ?? "";
        Label      = label ?? "";

        PlayerPrefs.SetString(KeyScript, ScriptName);
        PlayerPrefs.SetString(KeyLabel,  Label);
        PlayerPrefs.Save();

        Debug.Log($"[MRP] Set -> Script='{ScriptName}', Label='{Label}'");
    }

    public static bool HasValid ()
    {
        var ok = !string.IsNullOrEmpty(ScriptName);
        Debug.Log($"[MRP] HasValid? {ok} (Script='{ScriptName}', Label='{Label}')");
        return ok;
    }

    public static void Clear ()
    {
        ScriptName = "";
        Label      = "";
        PlayerPrefs.DeleteKey(KeyScript);
        PlayerPrefs.DeleteKey(KeyLabel);
        Debug.Log("[MRP] Cleared.");
    }
}
