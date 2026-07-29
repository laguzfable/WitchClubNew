using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 一行指令解鎖所有符文。
/// Nani 腳本用法：@unlockAllRunes
/// 不需要任何參數，最可靠。
/// </summary>
[CommandAlias("unlockAllRunes")]
public class UnlockAllRunesCommand : Command
{
    public override UniTask ExecuteAsync(AsyncToken token = default)
    {
        PlayerPrefs.SetString("UnlockedRunes_Blue",   "blue01,blue02,blue03,blue04,blue05");
        PlayerPrefs.SetString("UnlockedRunes_Red",    "red01,red02,red03,red04,red05");
        PlayerPrefs.SetString("UnlockedRunes_Yellow", "yellow01,yellow02,yellow03,yellow04,yellow05");
        PlayerPrefs.SetString("UnlockedRunes_Green",  "green01,green02,green03,green04,green05");
        PlayerPrefs.SetString("UnlockedRunes_None",   "mon02,mon04,mon08,mon09,mon10,mon12");
        PlayerPrefs.Save();
        Debug.Log("[unlockAllRunes] ✅ 全部符文已解鎖");
        AchievementManager.Instance.Unlock(AchievementManager.ACH_ALL_RUNES);
        return UniTask.CompletedTask;
    }
}
