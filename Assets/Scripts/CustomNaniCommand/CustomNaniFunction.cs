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

    /// <summary>
    /// 說「妳認錯人了」之後，又打了跟上一輪一樣的名字。
    /// 太長的名字存的是 LimitedName 截過的版本，所以超過 8 字時比截過的。
    /// </summary>
    public static bool SameAsSavedName (string name)
    {
        var saved = PlayerNameStore.Saved;
        if (string.IsNullOrEmpty(saved) || string.IsNullOrEmpty(name)) return false;
        name = name.Trim();
        if (name == saved) return true;
        return StrLeng(name) > 8 && LimitedName(name) == saved;
    }

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

    // 正常遊戲須親自完成第五場與最後引導；聖典只借回已取得的五枚儀式符文。
    public static bool YellowRescueReady () => RuneCollection.IsComplete("yellow")
        && (SanctumLoan.Active || (StoryFlag("YellowFifthComplete") && StoryFlag("YellowGuidanceComplete")));

    static bool StoryFlag (string name)
    {
        if (!Naninovel.Engine.Initialized) return false;
        var raw = Naninovel.Engine.GetService<Naninovel.ICustomVariableManager>()?.GetVariableValue(name);
        return bool.TryParse(raw, out var value) && value;
    }

    // ── 誰跟玩家最親近 ──────────────────────────────────────
    //
    // 用 C# 比而不是在劇本裡寫 affinity_Ved>affinity_Eup，是因為 NCalc 碰到
    // 沒被 @set 過的變數會整條運算式求值失敗，而且失敗是靜悄悄的
    // （綠線的西碧兒就這樣被吃掉過一次）。這裡讀不到的一律當 0，不會炸。

    /// <summary>薇狄亞是不是四個人裡好感最高的。同分不算——平手就照原本的三選一走。</summary>
    public static bool VediaIsClosest ()
    {
        var ved = Affinity("affinity_Ved");
        return ved > Affinity("affinity_Eup")
            && ved > Affinity("affinity_Mel")
            && ved > Affinity("affinity_Nel");
    }

    /// <summary>讀一個好感度變數。沒設過或讀不到都算 0。</summary>
    static int Affinity (string variableName)
    {
        if (!Naninovel.Engine.Initialized) return 0;

        var vars = Naninovel.Engine.GetService<Naninovel.ICustomVariableManager>();
        var raw = vars?.GetVariableValue(variableName);
        return int.TryParse(raw, out var value) ? value : 0;
    }
}
