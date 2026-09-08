using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>
/// 在劇本裡把一條筆記登記進筆記本。
///
/// 用法：
///   @note id:queen_question
///   @note ids:purge_first,recorder          ← 一次登記多條，逗號隔開（不含空格）
///   @note id:emberbud silent:true           ← 不跳提示（一次登記一整批時用）
///   @note id:purge_who important:true       ← 重要線索：改成在對話框裡講，玩家要點一下
///
/// 代號要對得上 Assets/Naninovel/Resources/Naninovel/Text/Notes.txt 裡的那一行。
/// 對不上不會壞，只是玩家點開會看到一條沒有內文的鎖著條目，所以這裡會警告。
///
/// ★ 為什麼不自己存 PlayerPrefs ★
/// 解鎖狀態直接用 Naninovel 的 IUnlockableManager（id = "Notes/代號"），
/// 跟 @unlock CG/cg01 同一套。存檔、跨周目保留、全域範圍那些都免費得到。
/// 實際上 @unlock Notes/queen_question 也會有一樣的效果——這支指令多做的事是
/// 驗代號、跳提示，以及讓劇本讀起來像在講「記下來」而不是「解鎖」。
/// </summary>
[CommandAlias("note")]
public class NoteCommand : Command
{
    [ParameterAlias("id")]
    public StringParameter Id;

    [ParameterAlias("ids")]
    public StringParameter Ids;

    [ParameterAlias("silent")]
    public BooleanParameter Silent;

    /// <summary>
    /// 重要線索。飄字提示很容易被忽略，所以改用對話框講一句、等玩家點一下。
    /// 一輪用個兩三次就好——每條都停一下的話，那個停頓就不值錢了。
    /// </summary>
    [ParameterAlias("important")]
    public BooleanParameter Important;

    public override async UniTask ExecuteAsync (AsyncToken token = default)
    {
        var raw = Assigned(Id) ? Id.Value : (Assigned(Ids) ? Ids.Value : null);
        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.LogWarning("[note] 沒有給 id 或 ids。");
            return;
        }

        var unlockables = Engine.GetService<IUnlockableManager>();
        var textManager = Engine.GetService<ITextManager>();
        if (unlockables == null)
        {
            Debug.LogWarning("[note] 拿不到 IUnlockableManager，這次不登記。");
            return;
        }

        var quiet = Assigned(Silent) && Silent.Value;
        var important = Assigned(Important) && Important.Value;

        foreach (var part in raw.Split(','))
        {
            var noteId = part.Trim();
            if (noteId.Length == 0) continue;

            var unlockableId = NotebookPanel.UnlockPrefix + noteId;

            if (unlockables.ItemUnlocked(unlockableId))
            {
                Debug.Log($"[note] {noteId} 已經記過了，跳過。");
                continue;
            }

            // 內文找不到不擋——劇本可能先寫好、Notes.txt 還沒補，
            // 那種情況讓它照樣解鎖，但要看得到警告。
            var value = textManager?.GetRecordValue(noteId, NotebookPanel.Category);
            if (string.IsNullOrEmpty(value))
                Debug.LogWarning($"[note] Notes.txt 裡沒有「{noteId}」這一條，玩家會看到空白頁。");

            unlockables.UnlockItem(unlockableId);
            Debug.Log($"[note] 登記 {noteId}");

            if (quiet) continue;

            var title = value?.Split('|').FirstOrDefault();
            var shown = string.IsNullOrWhiteSpace(title) ? noteId : title.Trim();

            if (important)
                await SystemNotice.InDialogue($"（這件事記進筆記本了：{shown}）", token);
            else
                NoteToast.Show(shown);
        }
    }
}
