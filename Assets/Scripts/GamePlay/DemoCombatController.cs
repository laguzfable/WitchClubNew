using UnityEngine;

/// <summary>
/// Demo 戰鬥引導控制器。
/// 掛在 CombatScene 的任意 GameObject。
/// 拖入同場景的 TutorialController，再設定三個角色的步驟陣列。
///
/// mobMei    → meiSteps
/// mobVivia  → viviaSteps
/// mobEuphie → euphieSteps
/// mobNelly  → 自由遊玩（不啟動引導）
/// </summary>
public class DemoCombatController : MonoBehaviour
{
    [SerializeField] TutorialController tutorialController;

    [Header("各角色教學步驟（與 TutorialController 的 tutorialArr 格式相同）")]
    [SerializeField] TutorialObject[] meiSteps;
    [SerializeField] TutorialObject[] viviaSteps;
    [SerializeField] TutorialObject[] euphieSteps;

    void Start()
    {
        var target = DataService.Instance?.scriptParameter?.combatTarget?.Value;

        TutorialObject[] steps = null;
        if      (target == "mobMei")    steps = meiSteps;
        else if (target == "mobVivia")  steps = viviaSteps;
        else if (target == "mobEuphie") steps = euphieSteps;
        // mobNelly → steps 為 null → 自由遊玩

        if (steps != null && steps.Length > 0 && tutorialController != null)
            tutorialController.BeginWithSteps(steps);
    }
}
