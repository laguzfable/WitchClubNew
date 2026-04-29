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
    }

    void Start()
    {
        TutorialObject[] steps = null;
        if      (_target == "mobMei")    steps = meiSteps;
        else if (_target == "mobVivia")  steps = viviaSteps;
        else if (_target == "mobEuphie") steps = euphieSteps;

        if (steps == null || steps.Length == 0 || tutorialController == null) return;

        // 至少一個步驟有對話文字才啟動，防止 Inspector 留空元素導致白屏
        bool hasContent = false;
        foreach (var s in steps) if (s != null && !string.IsNullOrEmpty(s.dialog)) { hasContent = true; break; }
        if (!hasContent) return;

        // 有步驟才設旗標並注入，避免空步驟時卡住 PrepareBeginTurn
        TutorialController.isDemoMode = true;
        var cs = FindObjectOfType<CombatSystem>();
        if (cs != null) cs.SetTutorController(tutorialController);

        tutorialController.BeginWithSteps(steps);
    }

    void OnDestroy()
    {
        TutorialController.isDemoMode = false;
    }
}
