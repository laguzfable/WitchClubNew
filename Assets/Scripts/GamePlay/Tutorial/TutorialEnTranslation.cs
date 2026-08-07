using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chinese (zh-TW) → English / Japanese translation for tutorial dialogue bubbles.
/// Automatically reads PlayerPrefs "Language" to pick the right language.
/// Matching is substring-based to tolerate minor character differences.
/// </summary>
public static class TutorialEnTranslation
{
    struct Entry
    {
        public string key;  // unique Chinese substring to match on
        public string en;
        public string ja;
        public Entry(string k, string e, string j) { key = k; en = e; ja = j; }
    }

    static readonly List<Entry> table = new List<Entry>
    {
        // ── Direction / pointer prompts ──────────────────────────────
        new Entry("看到右邊這個區塊了嗎",
            "See that panel on the right? That's where your turn results appear!",
            "右側のパネルが見える？そこにターンの結果が表示されるよ！"),

        new Entry("看到左邊這個區塊了嗎",
            "See that panel on the left? That shows your hand of cards!",
            "左側のパネルが見える？そこに手札が表示されるよ！"),

        new Entry("看到這個區塊了嗎",
            "See this panel? Pay attention to what appears here each turn.",
            "このパネルが見える？毎ターン、ここに注目して。"),

        // ── Core tutorial entries ────────────────────────────────────
        new Entry("巫術世界有四大屬性",
            "The world of magic has four schools — Academy (Blue), Blood (Red), Nature (Green), and Devil (Gold).",
            "魔法の世界には四つの属性がある——アカデミー（青）、血系（赤）、自然（緑）、悪魔（金）。"),

        new Entry("每張牌都有最多2種資訊",
            "Each card shows up to two stats: Attack and Defense.",
            "各カードには最大2種類のステータスがある：攻撃と防御。"),

        new Entry("唯一的特例是自然",
            "The exception is Nature (green) cards — instead of Attack they carry a Healing value.",
            "例外は自然（緑）カード——攻撃の代わりに回復値を持っている。"),

        new Entry("每個回合，您將會隨機獲得5張牌",
            "Each turn you draw 5 random cards. You can play any number of cards of the SAME color.",
            "各ターン、ランダムに5枚のカードを引く。同じ色のカードなら何枚でも出せる。"),

        new Entry("這邊會出現結算後的數字",
            "Your turn's results are tallied up here.",
            "このターンの結果がここに表示される。"),

        new Entry("選好要出的牌後，要按下結束回合",
            "Once you've chosen your cards, press End Turn to play them. Take your time before deciding!",
            "カードを選んだら、ターン終了を押して出す。焦らず考えて！"),

        new Entry("試試看吧，先打出鮮血",
            "Give it a try — select the Blood (red) card(s) and press End Turn.",
            "やってみよう——血系（赤）のカードを選んでターン終了を押して。"),

        new Entry("挺會的嘛，現在要來真的",
            "Nice! Now let's do this for real.",
            "なかなかやるね！さあ、本番だ。"),

        new Entry("這邊是",
            "This is your health bar. Drain it to zero and you lose!",
            "これは自分のHPバー。ゼロになったら負け！"),

        new Entry("至於這上面，則是對手的血量",
            "Above is your opponent's health, and a preview of their next move.",
            "上は相手のHPと、次の行動のプレビューだ。"),

        new Entry("再試一次吧，用學院",
            "Let's try again — block the attack with Academy (blue) cards.",
            "もう一度——アカデミー（青）カードで攻撃を防いで。"),

        new Entry("很好，就是這樣",
            "Good. You're getting the hang of it!",
            "よし、その調子！"),

        new Entry("沒打出的牌數值上升了",
            "Cards left in your hand get upgraded! Each card can upgrade up to five times.",
            "手札に残ったカードはパワーアップ！各カードは最大5回まで強化できる。"),

        new Entry("把升級過的自然",
            "Play the upgraded Nature (green) cards to top off your health!",
            "強化された自然（緑）カードを出してHPを回復しよう！"),

        new Entry("沒錯，就是這樣",
            "That's it. Just like that!",
            "そう、まさにその通り！"),

        new Entry("能量開始浮動了呢",
            "The energy around here is shifting. This shows the environmental effect active this turn.",
            "周囲のエネルギーが揺らいでいる。このターンのアクティブな環境効果だ。"),

        new Entry("下回合是攻擊加倍的絳紅之夜",
            "A Crimson Night is coming — all attacks are doubled next turn. Let's test it!",
            "次のターンは「深紅の夜」——全攻撃が2倍になる。試してみよう！"),

        new Entry("來吧，打出鮮血",
            "Play some Blood (red) cards and see the doubled damage in action.",
            "血系（赤）カードを出して、2倍ダメージを確認しよう。"),

        new Entry("這比想像得痛",
            "Wow — that hurt more than expected! How exciting!",
            "うわ——思ったより痛かった！なかなか刺激的だね！"),

        new Entry("戰鬥技巧應該是沒問題",
            "That covers the basics of combat!",
            "これで戦闘の基本はバッチリ！"),

        // ── Rune tutorial (isTutorial2) ──────────────────────────────
        new Entry("每場戰鬥開始，符文都屬於休眠的狀態",
            "At the start of every battle, all runes are dormant.",
            "戦闘開始時、全てのルーンは休眠状態にある。"),

        new Entry("要怎麼啟用符文呢？就得靠惡魔的契約之力",
            "How do you activate a rune? You need the power of a Devil contract!",
            "ルーンを起動するには？悪魔の契約の力が必要だ！"),

        new Entry("我們來看看惡魔卡",
            "Let's take a look at Devil cards.",
            "悪魔カードを見てみよう。"),

        new Entry("普通的惡魔卡每張可以儲1星",
            "A normal Devil card stores 1 star per play. Special ones — those with Lilina on them — store 3 stars.",
            "通常の悪魔カードは1枚で1スター蓄積。リリナが描かれた特別なカードは3スター蓄積！"),

        new Entry("要集幾顆星才能啟用符文呢",
            "How many stars does it take to activate a rune? Each rune is different!",
            "ルーンを起動するのに何スター必要か？ルーンによって異なる！"),

        new Entry("已經集滿的符文，會發光提示可以使用",
            "A fully charged rune will glow — that means it's ready to use!",
            "スターが溜まったルーンは光って使用可能を知らせる！"),

        new Entry("星星是共用的，也就是說用了其中一個符文之後",
            "Stars are shared across all runes. Using one rune means you'll need to recharge before using another.",
            "スターは全ルーン共有。1つ使ったら再チャージが必要だ。"),

        new Entry("然後，使用符文並不會結束回合",
            "Also — using a rune does NOT end your turn. Feel free to use it anytime!",
            "ルーン使用はターン終了にならない。いつでも自由に使える！"),

        new Entry("講了這麼多",
            "That's enough talk — give it a try!",
            "説明はこのくらい——実際にやってみよう！"),

        new Entry("先打出金色的惡魔牌",
            "Start by playing the gold Devil cards.",
            "まず金色の悪魔カードを出して。"),

        new Entry("看起來已經啟用洗掉所有手牌的技能",
            "Looks like you triggered the ability that clears your whole hand — let's see it in action!",
            "手札を全部捨てるスキルが発動したようだ——実際に見てみよう！"),

        new Entry("按下右邊最下面，惡魔系的符文",
            "Press the Devil-faction rune at the bottom right.",
            "右下の悪魔系ルーンを押して。"),

        new Entry("非常好，看起來",
            "Excellent — looks like you've got this down!",
            "すばらしい——もう完璧だね！"),
    };

    public static string Translate(string zh)
    {
        if (string.IsNullOrEmpty(zh)) return zh;

        var lang = PlayerPrefs.GetString("Language", "zh-TW").ToLower();
        bool isJa = lang.StartsWith("ja");
        bool isZh = lang.StartsWith("zh");

        // zh-TW: return original
        if (isZh) return zh;

        foreach (var e in table)
        {
            if (zh.Contains(e.key))
                return isJa ? e.ja : e.en;
        }
        return zh;
    }
}
