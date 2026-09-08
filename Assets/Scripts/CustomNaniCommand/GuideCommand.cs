using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 個人線分歧前的夜晚倒數，由星塵來講。left 包含即將進入的今晚，不計白天地圖。
/// 第三章四夜、第四章分歧前兩夜；改動行程時須同步更新呼叫處與倒數檢查。
///
/// ★ 台詞不在這裡 ★
/// 全部在 Resources/Guide/GuideHints.json，用劇本檢視器的「星塵」頁編（或直接改檔）。
/// 每一則可以自己指定背景、立繪、站位、音樂，留空就是不動那一項。
/// </summary>
[CommandAlias("guide")]
public class GuideCommand : Command
{
    /// <summary>台詞資料放哪（Resources 底下，不含副檔名）。</summary>
    public const string HintsPath = "Guide/GuideHints";

    [ParameterAlias("left"), RequiredParameter]
    public IntegerParameter Left;

    [Serializable]
    public class Hint
    {
        public int left;            // 算上今晚還剩幾個夜晚
        public string bg;           // 背景，例如 star1；留空＝不動
        public string character;    // 立繪的角色 ID，例如 星塵；留空＝不出立繪
        public string pose;         // 表情／動作，留空＝用預設
        public float pos = 50f;     // 立繪站位，跟劇本的 pos: 一樣是 0～100
        public string bgm;          // 音樂，留空＝不動
        public string[] lines;      // 講的話，一行一句
    }

    [Serializable]
    class HintFile { public List<Hint> hints; }

    static List<Hint> cache;

    /// <summary>讀台詞檔。讀不到就回空清單（並且大聲抱怨——
    /// 這種東西一旦靜悄悄地不見，玩家那邊只會看到「什麼都沒發生」）。</summary>
    public static List<Hint> LoadHints ()
    {
        if (cache != null) return cache;

        var asset = Resources.Load<TextAsset>(HintsPath);
        if (asset == null)
        {
            Debug.LogError($"[guide] 找不到 Resources/{HintsPath}.json，星塵不會講話。");
            return cache = new List<Hint>();
        }

        try
        {
            var file = JsonUtility.FromJson<HintFile>(asset.text);
            cache = file?.hints ?? new List<Hint>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[guide] {HintsPath}.json 解析失敗，星塵不會講話：{ex.Message}");
            cache = new List<Hint>();
        }
        return cache;
    }

    /// <summary>編輯器改完檔案要重讀時用。</summary>
    public static void ClearCache () => cache = null;

    public override async UniTask ExecuteAsync (AsyncToken token = default)
    {
        if (!Assigned(Left) || Left.Value < 1 || Left.Value > 6)
        {
            Debug.LogError("[guide] 分歧前的剩餘夜晚需介於 1～6，並包含今晚。");
            return;
        }

        var left = Left.Value;
        var hints = LoadHints().FindAll(h => h != null && h.left == left);
        if (hints.Count == 0)
        {
            Debug.LogWarning($"[guide] {HintsPath}.json 裡沒有 left={left} 的台詞，這個夜晚星塵不會講話。");
            return;
        }

        foreach (var hint in hints)
        {
            await ApplyStageAsync(hint, token);

            if (hint.lines == null) continue;
            foreach (var line in hint.lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                await SystemNotice.InDialogue(line.Replace("{left}", left.ToString()), token);
            }

            await HideCharacterAsync(hint, token);
        }
    }

    /// <summary>把這一則指定的背景／立繪／音樂擺好。每一項留空就跳過。</summary>
    static async UniTask ApplyStageAsync (Hint hint, AsyncToken token)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hint.bg))
            {
                var backgrounds = Engine.GetService<IBackgroundManager>();
                var back = backgrounds?.GetActor(BackgroundsConfiguration.MainActorId);
                if (back != null)
                {
                    await back.ChangeAppearanceAsync(hint.bg.Trim(), 0.3f, asyncToken: token);
                    if (!back.Visible) await back.ChangeVisibilityAsync(true, 0.3f, asyncToken: token);
                }
            }

            if (!string.IsNullOrWhiteSpace(hint.character))
            {
                var actor = await Engine.GetService<ICharacterManager>()
                    .GetOrAddActorAsync(hint.character.Trim());
                if (actor != null)
                {
                    if (!string.IsNullOrWhiteSpace(hint.pose))
                        await actor.ChangeAppearanceAsync(hint.pose.Trim(), 0.3f, asyncToken: token);

                    // 劇本的 pos: 是畫面百分比，實際位置要換算成世界座標
                    // （跟 Naninovel 的 ModifyOrthoActor 同一套算法）。
                    var camera = Engine.GetService<ICameraManager>();
                    if (camera != null)
                    {
                        var x = camera.Configuration.SceneToWorldSpace(new Vector2(hint.pos / 100f, 0)).x;
                        actor.Position = new Vector3(x, actor.Position.y, actor.Position.z);
                    }

                    await actor.ChangeVisibilityAsync(true, 0.3f, asyncToken: token);
                }
            }

            if (!string.IsNullOrWhiteSpace(hint.bgm))
            {
                var audio = Engine.GetService<IAudioManager>();
                if (audio != null)
                {
                    // 先停再放：Naninovel 的 BGM 是疊著放的。
                    await audio.StopAllBgmAsync(0.3f);
                    await audio.PlayBgmAsync(hint.bgm.Trim(), volume: 1f, fadeTime: 0.4f, loop: true);
                }
            }
        }
        catch (Exception ex)
        {
            // 背景打錯字不該讓整段提醒消失——台詞照講，錯誤留在 log 裡。
            Debug.LogWarning($"[guide] 佈景設定失敗（bg={hint.bg} char={hint.character} bgm={hint.bgm}）：{ex.Message}");
        }
    }

    /// <summary>講完把立繪收掉。下一個指令通常是切到地圖場景，留著會跟著閃一下。</summary>
    static async UniTask HideCharacterAsync (Hint hint, AsyncToken token)
    {
        if (string.IsNullOrWhiteSpace(hint.character)) return;

        try
        {
            var characters = Engine.GetService<ICharacterManager>();
            var id = hint.character.Trim();
            if (characters == null || !characters.ActorExists(id)) return;

            var actor = characters.GetActor(id);
            if (actor != null && actor.Visible)
                await actor.ChangeVisibilityAsync(false, 0.3f, asyncToken: token);
        }
        catch (Exception ex) { Debug.LogWarning($"[guide] 收立繪失敗：{ex.Message}"); }
    }
}
