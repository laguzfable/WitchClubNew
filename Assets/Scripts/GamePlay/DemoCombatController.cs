using UnityEngine;

/// <summary>
/// Demo 戰鬥引導控制器。
/// 掛在 CombatScene 的任意 GameObject，拖入 Tutorial2Canvas 上的 TutorialController。
///
/// mobMei    → meiSteps（教學引導）
/// mobVivia  → viviaSteps（教學引導）
/// mobEuphie → euphieSteps（教學引導）
/// mobNelly  → 自由遊玩（不啟動引導）
/// </summary>
public class DemoCombatController : MonoBehaviour
{
    [SerializeField] TutorialController tutorialController;

    [Header("各角色教學步驟（格式同 TutorialController 的 tutorialArr）")]
    [SerializeField] TutorialObject[] meiSteps;
    [SerializeField] TutorialObject[] viviaSteps;
    [SerializeField] TutorialObject[] euphieSteps;

    string _target;

    void Awake()
    {
        _target = DataService.Instance?.scriptParameter?.combatTarget?.Value ?? "";

        // 讓 CombatSystem 的 canGoNext 判斷能進入 demo 分支
        if (_target == "mobMei" || _target == "mobVivia" || _target == "mobEuphie")
            TutorialController.isDemoMode = true;
    }

    void Start()
    {
        TutorialObject[] steps = null;
        if      (_target == "mobMei")    steps = meiSteps;
        else if (_target == "mobVivia")  steps = viviaSteps;
        else if (_target == "mobEuphie") steps = euphieSteps;

        if (steps == null || steps.Length == 0 || tutorialController == null) return;

        // 把 tutorController 注入 CombatSystem，讓 canGoNext 能正確傳回來
        var cs = FindObjectOfType<CombatSystem>();
        if (cs != null) cs.SetTutorController(tutorialController);

        tutorialController.BeginWithSteps(steps);
    }

    void OnDestroy()
    {
        TutorialController.isDemoMode = false;
    }
}
