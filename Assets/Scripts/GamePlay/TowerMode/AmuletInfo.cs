using System.Collections.Generic;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 護身符頁右側詳情用的名稱／分類／效果說明表。
    /// 效果敘述要跟 TowerModeManager 裡真正的實作對得上，改規則時兩邊一起改。
    /// 中文是原文，英日文走 RuneEnTranslation 查表。
    /// </summary>
    public static class AmuletInfo
    {
        public struct Info
        {
            public string name;
            public string category;
            public string description;
        }

        // 分類（也是查表 key，翻譯放在 RuneEnTranslation.Names*）
        const string CardBoost = "卡片強化";
        const string SlotCategory = "護符欄位";
        const string BattleStart = "戰鬥起手";

        // key 必須跟 TowerModeManager 裡的護身符代號一字不差（場景按鈕填的也是同一組字串）
        static readonly Dictionary<string, Info> Table = new Dictionary<string, Info>
        {
            { "Blue", new Info {
                name = "藍護身符", category = CardBoost,
                description = "挑戰中，學院（藍）卡片的攻擊、防禦、治療、能量全部變成 2 倍。" } },

            { "Red", new Info {
                name = "紅護身符", category = CardBoost,
                description = "挑戰中，血系（紅）卡片的攻擊、防禦、治療、能量全部變成 2 倍。" } },

            { "Yellow", new Info {
                name = "黃護身符", category = CardBoost,
                description = "挑戰中，惡魔（黃）卡片的攻擊、防禦、治療、能量全部變成 2 倍。" } },

            { "Green", new Info {
                name = "綠護身符", category = CardBoost,
                description = "挑戰中，自然（綠）卡片的攻擊、防禦、治療、能量全部變成 2 倍。" } },

            { "Dimension", new Info {
                name = "次元護符", category = CardBoost,
                description = "手牌的等級上限從 5 級提高到 7 級；第 6、7 級照第 4→5 級的成長幅度往上推算。" } },

            { "Adapter", new Info {
                name = "護符轉接頭", category = SlotCategory,
                description = "護身符欄位從 1 格變成 3 格（轉接頭本身也佔 1 格）。卸下時，多出來裝不下的護身符會自動取下。" } },

            { "FullEnergy", new Info {
                name = "深淵護符", category = BattleStart,
                description = "每場戰鬥一開始，符文能量就是全滿的。" } },

            { "WitchRune", new Info {
                name = "女巫符文", category = BattleStart,
                description = "每場戰鬥開場，手上保證有 1 張角色卡。" } },

            { "LilyRune", new Info {
                name = "百合符文", category = BattleStart,
                description = "上一場滿血過關的話，這場開場保證有 2 張角色卡；可以和女巫符文疊加。" } },
        };

        public static bool TryGet (string amuletId, out Info info)
        {
            info = default;
            return !string.IsNullOrEmpty(amuletId) && Table.TryGetValue(amuletId, out info);
        }
    }
}
