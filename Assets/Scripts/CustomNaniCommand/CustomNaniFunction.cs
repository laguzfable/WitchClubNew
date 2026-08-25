[Naninovel.ExpressionFunctions]
public static class CustomNaniFunction
{
    public static int StrLeng (string content) => content.Length;

    public static string LimitedName (string content) => content.Substring(0, 2);

    // ─── 輪次判斷 ───────────────────────────────────────────────
    // 「第幾輪」沒有專門的存檔欄位，靠 EndingRecord（本地結局紀錄，PlayerPrefs）推算：
    // 看過任一結局，就代表玩家已經回過標題重跑一次，也就是第二輪以後。
    // 這份紀錄跟 Steam 成就是分開的，離線／Editor／沒登入 Steam 都算得出來。

    /// <summary>已收集的結局數。@if 裡可以直接比大小。</summary>
    public static int EndingCount () => EndingRecord.Count;

    /// <summary>指定結局拿過沒有。例：HasEnding("ACH_END_07")</summary>
    public static bool HasEnding (string achievementId) => EndingRecord.Has(achievementId);

    /// <summary>是不是第二輪以後。想改成「拿過輪迴の鑰匙才算」就換成 HasEnding("ACH_END_07")。</summary>
    public static bool IsSecondPlaythrough () => EndingRecord.Count >= 1;

    // ─── 教學 ───────────────────────────────────────────────────
    // 劇本用：@if "SkipTutorial('basic')" → 走一句台詞帶過；@else → 照常進教學。
    // id 是 TutorialRecord.Basic / TutorialRecord.Rune 那兩個字串。

    /// <summary>這個教學該不該跳過（設定有開 + 已經完整看過一次）。</summary>
    public static bool SkipTutorial (string tutorialId) => TutorialRecord.ShouldSkip(tutorialId);

    // 劇本裡優先用這兩個無參數版本：NCalc 對引號很敏感，能不寫字串就不寫。
    public static bool SkipBasicTutorial () => TutorialRecord.ShouldSkip(TutorialRecord.Basic);
    public static bool SkipRuneTutorial () => TutorialRecord.ShouldSkip(TutorialRecord.Rune);

    // ─── 符文收集 ───────────────────────────────────────────────
    // 夜晚儀式打贏才給符文，所以符文數＝做完幾場儀式。
    // 路線分歧與結局判定改用這個，取代原本的好感門檻。
    // 用法：@if "RuneCount('yellow')>=3"

    /// <summary>指定顏色已解鎖的符文數（0～5）。劇本裡建議用下面四個無參數版本。</summary>
    public static int RuneCount (string color) => RuneCollection.Count(color);

    /// <summary>之前玩過、名字有存起來過沒有。用在序章的「我們是不是見過？」分支。</summary>
    public static bool HasSavedName () => !string.IsNullOrEmpty(PlayerNameStore.Saved);

    // ─── 儀式階段 ───────────────────────────────────────────────
    // 閒聊劇本用：@goto .chat3 if:StageNelly()>=2
    // 回傳 0-based 的「卡在第幾場儀式」，0＝還沒做過任何一場。

    public static int StageEupie () => RitualGate.Stage("Eupie");
    public static int StageMel () => RitualGate.Stage("Mel");
    public static int StageNelly () => RitualGate.Stage("Nelly");
    public static int StageVedia () => RitualGate.Stage("Vedia");
    public static int StageSybil () => RitualGate.Stage("Sybil");

    // 劇本用：@goto xxx if:RunesYellow()>=3 —— 不必寫引號，NCalc 不會出事。
    public static int RunesBlue () => RuneCollection.Count("blue");
    public static int RunesRed () => RuneCollection.Count("red");
    public static int RunesYellow () => RuneCollection.Count("yellow");
    public static int RunesGreen () => RuneCollection.Count("green");
}
