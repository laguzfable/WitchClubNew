// Assets/Scripts/UI/UnlockNotice.cs
//
// 拿到符文或新卡片時，在對話框插一句有顏色的提示，順便播一聲提示音。
//
// ★ 為什麼要 await ★
// 底下走 SystemNotice（＝Naninovel 的 @print），呼叫端必須是被劇本 await 的指令，
// 不然這句會跟劇本自己的台詞搶同一個印字機。詳見 SystemNotice 的說明。
//
// ★ 符文為什麼要排隊 ★
// 卡片是劇本用 @unlockCardVariant 給的，當下就在對話框裡，直接印。
// 符文是戰鬥場景給的（CombatSystem.TryUnlockRune），那裡沒有對話框，
// 所以先排進佇列，等回到劇本、跑到 @runeNotice 的時候再印。

using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

public static class UnlockNotice
{
    /// <summary>提示音。名字要對得上 Naninovel 資源清單裡的 Audio（Assets/Sound/SE/success）。</summary>
    const string Sfx = "success";

    /// <summary>還沒印出來的符文（戰鬥場景寫進來的）。存 PlayerPrefs 而不是靜態變數——
    /// 玩家可能在戰鬥結束後直接存檔離開，下次進來還是要看到這則提示。</summary>
    const string PendingKey = "PendingRuneNotice";

    // 對話框底色偏深，所以四色都挑亮一階的，讓字浮得出來。
    static string ColorOf (ECardElement element)
    {
        switch (element)
        {
            case ECardElement.Blue:   return "#6EC6FF";
            case ECardElement.Red:    return "#FF7A8A";
            case ECardElement.Yellow: return "#FFD45E";
            case ECardElement.Green:  return "#8CE099";
            default:                  return "#C7B3FF";   // 無屬性（mon 系列）
        }
    }

    static string Tint (string text, ECardElement element) =>
        $"<color={ColorOf(element)}>{text}</color>";

    // ── 符文 ────────────────────────────────────────────────

    /// <summary>戰鬥場景拿到符文時呼叫。只排隊，不印。</summary>
    public static void QueueRune (string runeId)
    {
        if (string.IsNullOrWhiteSpace(runeId)) return;

        var queued = Pending();
        if (queued.Contains(runeId)) return;

        queued.Add(runeId);
        PlayerPrefs.SetString(PendingKey, string.Join(",", queued));
        PlayerPrefs.Save();
        Debug.Log($"[UnlockNotice] 符文提示排進佇列：{runeId}（目前 {queued.Count} 則）");
    }

    /// <summary>把排隊中的符文提示一則一則印出來。@runeNotice 呼叫。</summary>
    public static async UniTask FlushRunesAsync (AsyncToken token = default)
    {
        var queued = Pending();
        if (queued.Count == 0) return;

        // 先清掉再印：印到一半被玩家跳過或關掉遊戲的話，寧可少印一次，
        // 也不要下次進來又重印一遍同樣的東西。
        PlayerPrefs.DeleteKey(PendingKey);
        PlayerPrefs.Save();

        foreach (var id in queued)
        {
            var ability = DataService.Instance != null
                ? DataService.Instance.GetAbilityById(id)
                : new Ability();

            var name = string.IsNullOrWhiteSpace(ability.name) ? id : ability.name;
            var element = string.IsNullOrWhiteSpace(ability.name) ? ElementFromId(id) : ability.element;

            await PrintAsync("獲得符文", RuneEnTranslation.TranslateName(name), element, token);
        }
    }

    static List<string> Pending () =>
        PlayerPrefs.GetString(PendingKey, "")
            .Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    /// <summary>DataService 不在場（劇本場景沒有它）時的退路：從 id 前綴判色。</summary>
    static ECardElement ElementFromId (string id)
    {
        var lower = id.ToLowerInvariant();
        if (lower.StartsWith("blue"))   return ECardElement.Blue;
        if (lower.StartsWith("red"))    return ECardElement.Red;
        if (lower.StartsWith("yellow")) return ECardElement.Yellow;
        if (lower.StartsWith("green"))  return ECardElement.Green;
        return ECardElement.None;
    }

    // ── 卡片變體 ────────────────────────────────────────────

    /// <summary>劇本用 @unlockCardVariant 解鎖新卡時呼叫，當下就印。</summary>
    public static async UniTask CardVariantAsync (ECardElement element, string variant,
                                                  AsyncToken token = default)
    {
        await PrintAsync("獲得新卡片", CardName(element, variant), element, token);
    }

    /// <summary>卡名跟休息頁的按鈕讀同一份資料（見 SelectCardVariant），才不會兩邊叫法不一樣。</summary>
    static string CardName (ECardElement element, string variant)
    {
        var baseId = BaseIdForElement(element);
        if (baseId < 0) return element + variant;

        var id = variant == "A" ? baseId * 10 + 1
               : variant == "B" ? baseId * 10 + 2
               : baseId;

        var card = Resources.Load<CardData>("DataCollections/CardData_" + id);
        return card != null && !string.IsNullOrWhiteSpace(card.displayName)
            ? RuneEnTranslation.TranslateName(card.displayName)
            : element + variant;
    }

    static int BaseIdForElement (ECardElement element)
    {
        switch (element)
        {
            case ECardElement.Red:    return 101;
            case ECardElement.Blue:   return 102;
            case ECardElement.Green:  return 103;
            case ECardElement.Yellow: return 104;
            default: return -1;
        }
    }

    // ── 共用 ────────────────────────────────────────────────

    static async UniTask PrintAsync (string headingZh, string name, ECardElement element,
                                     AsyncToken token)
    {
        PlaySfx();
        var heading = RuneEnTranslation.TranslateName(headingZh);
        await SystemNotice.InDialogue($"{heading}　{Tint(name, element)}", token);
    }

    /// <summary>提示音不 await：等它播完才讓字出來的話，玩家會先聽到聲音再看到字。</summary>
    static void PlaySfx ()
    {
        if (!Engine.Initialized) return;

        try
        {
            var audio = Engine.GetService<IAudioManager>();
            audio?.PlaySfxAsync(Sfx).Forget();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UnlockNotice] 提示音播不出來：{ex.Message}");
        }
    }
}
