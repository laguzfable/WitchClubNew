using Naninovel;

// 讓 @battle 也能觸發既有的 GotoCombatScene，不改任何原檔
[CommandAlias("battle")]
public class BattleAliasLower : GotoCombatScene {}
