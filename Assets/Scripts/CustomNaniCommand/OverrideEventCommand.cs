using Naninovel;
using UnityEngine;

[CommandAlias("overrideEvent")]
public class OverrideEventCommand : Command
{
    [ParameterAlias("char"), RequiredParameter]
    public StringParameter character;

    [ParameterAlias("evt"), RequiredParameter]
    public StringParameter evtName;

    [ParameterAlias("script")]
    public StringParameter scriptName;

    // 舊版 Naninovel 沒有 FloatParameter → 改成字串
    // 格式： "10,-20"
    [ParameterAlias("offset")]
    public StringParameter offsetString;

    // 舊版只支援字串路徑 → 用 Resources.Load
    [ParameterAlias("anim")]
    public StringParameter animatorPath;

    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var data = new MapSpecialOverrideData();
        data.characterName = character.Value;
        data.eventName = evtName.Value;
        data.naninovelScript = scriptName?.Value ?? "";

        // --- 解析 offset ---
        Vector2 offset = Vector2.zero;
        if (!string.IsNullOrEmpty(offsetString?.Value))
        {
            // 格式: "x,y"
            var parts = offsetString.Value.Split(',');
            if (parts.Length == 2)
            {
                float.TryParse(parts[0], out offset.x);
                float.TryParse(parts[1], out offset.y);
            }
        }
        data.offset = offset;

        // --- 動畫 ---
        if (!string.IsNullOrEmpty(animatorPath?.Value))
        {
            var anim = Resources.Load<RuntimeAnimatorController>(animatorPath.Value);
            if (anim != null)
                data.animator = anim;
            else
                Debug.LogWarning($"[overrideEvent] 無法在 Resources 載入動畫：{animatorPath.Value}");
        }

        MapSpecialOverride.SetOverride(data);
        return UniTask.CompletedTask;
    }
}
