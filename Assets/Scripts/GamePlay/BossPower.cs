using Naninovel;
using UnityEngine;

/// <summary>
/// 第二章賣書的選擇（chapter2 的 @choice set:bossPower=N）對結局戰的影響。
///
/// 賣掉越核心的書，流出去的知識越危險，結局王就越強：
///   0 = 學院中的禁忌藥草  1 = 奧秘之書《無窮魔源》  2 = 蝕之聖典
///
/// 目前只反映在血量上。要改成也影響攻擊力或行動次數的話，加在這裡，
/// 不要散到 EnemyUnit 裡面去。
/// </summary>
public static class BossPower
{
    /// <summary>Naninovel 自訂變數名稱，跟 chapter2.nani 的 @choice set: 一致。</summary>
    public const string VariableName = "bossPower";

    /// <summary>會吃這個加成的敵人。目前只有結局戰的王。</summary>
    public const string FinalBossMobName = "micha_final";

    /// <summary>index 對應 bossPower 的值，超出範圍就用最後一個。</summary>
    static readonly float[] hpMultipliers = { 1f, 1.25f, 1.5f };

    /// <summary>
    /// 讀出目前的 bossPower。讀不到（沒跑過第二章、舊存檔、Naninovel 還沒起來）
    /// 一律當 0，也就是不加成——寧可王太弱，也不要因為一個沒設定的變數把玩家卡死。
    /// </summary>
    public static int Current
    {
        get
        {
            try
            {
                var vars = Engine.GetService<ICustomVariableManager>();
                if (vars == null) return 0;

                var raw = vars.GetVariableValue(VariableName);
                if (string.IsNullOrEmpty(raw)) return 0;

                return int.TryParse(raw, out var value) ? Mathf.Max(0, value) : 0;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[BossPower] 讀不到 " + VariableName + "，當作 0：" + ex.Message);
                return 0;
            }
        }
    }

    /// <summary>把加成套到基礎血量上。不是結局王就原樣回傳。</summary>
    public static int ApplyHP(string mobName, int baseHP)
    {
        if (string.IsNullOrEmpty(mobName) ||
            !mobName.Equals(FinalBossMobName, System.StringComparison.OrdinalIgnoreCase))
            return baseHP;

        var power = Current;
        var mult = hpMultipliers[Mathf.Min(power, hpMultipliers.Length - 1)];
        var hp = Mathf.RoundToInt(baseHP * mult);

        Debug.Log($"[BossPower] 結局王血量 {baseHP} → {hp}（bossPower={power}, ×{mult}）");
        return hp;
    }
}
