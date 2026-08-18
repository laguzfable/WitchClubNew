using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 更換卡片頁右側詳情用：把 CardData 組成一段「效果說明」。
///
/// 卡片資料本身沒有說明欄位（只有 1~5 級的 ATK/DEF/HEAL/EN 數值表），
/// 所以說明分兩段：上面一句人寫的特色描述，下面是資料表直接讀出來的數值。
/// 數值是從 asset 現讀的，之後調平衡改了資料，說明會自己跟著變。
/// </summary>
public static class CardEffectText
{
    // 卡片資料表設計上就是 5 級；不要用陣列實際長度來判斷，
    // 有些資產在 Inspector 裡不小心多存了一筆全 0 的尾巴（跟 ElementCard 同樣的理由）。
    const int MaxLevel = 5;

    /// <summary>每種型態的特色描述（中文原文，英日文走 RuneEnTranslation.TranslateDesc）。</summary>
    static readonly Dictionary<string, string> Flavors = new Dictionary<string, string>
    {
        // ── 血系（紅）──────────────────────────────
        { "101",  "純攻擊：只有攻擊力，沒有其他附加效果。" },
        { "1011", "攻擊特化：攻擊力比原版更高，其餘一樣是零。" },
        { "1012", "攻守兼備：攻擊力比原版低，但每張同時附帶治療。" },

        // ── 學院（藍）──────────────────────────────
        { "102",  "防禦為主：以防禦為主，附帶少量攻擊。" },
        { "1021", "防禦特化：防禦更高，攻擊更低。" },
        { "1022", "充能型：防禦略降，改成每張提供 1 點符文能量。" },

        // ── 自然（綠）──────────────────────────────
        { "103",  "治療為主：固定防禦搭配高治療。" },
        { "1031", "治療特化：治療更高，防禦更低。" },
        { "1032", "守護型：治療降低，換取更高的固定防禦。" },

        // ── 惡魔（黃）──────────────────────────────
        { "104",  "均衡型：攻防兼具，每張還提供 1 點符文能量。" },
        { "1041", "充能特化：攻防較低，但每張提供 2 點符文能量。" },
        { "1042", "戰鬥型：攻防都更高，但不提供符文能量。" },
    };

    public static string Build (CardData card)
    {
        if (card == null) return "";

        var sb = new StringBuilder();

        if (Flavors.TryGetValue(card.ID ?? "", out var flavor))
            sb.AppendLine(RuneEnTranslation.TranslateDesc(flavor)).AppendLine();

        if (card.cardAttr != null && card.cardAttr.Length > 0)
        {
            sb.AppendLine("Lv1 → Lv" + MaxLevel);
            AppendStat(sb, card, "攻擊", a => a.ATK);
            AppendStat(sb, card, "防禦", a => a.DEF);
            AppendStat(sb, card, "治療", a => a.HEAL);
            AppendStat(sb, card, "能量", a => a.EN);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>某個屬性從 1 級到 5 級的數值。整條都是 0 的（例如不會治療的卡）就整行不顯示。</summary>
    static void AppendStat (StringBuilder sb, CardData card, string labelZh, Func<CardAttribute, int> pick)
    {
        var topIndex = Mathf.Min(MaxLevel, card.cardAttr.Length) - 1;
        var first = pick(card.cardAttr[0]);
        var top = pick(card.cardAttr[topIndex]);

        if (first == 0 && top == 0) return;

        var label = RuneEnTranslation.TranslateName(labelZh);
        sb.AppendLine(first == top ? $"{label}　{first}" : $"{label}　{first} → {top}");
    }
}
