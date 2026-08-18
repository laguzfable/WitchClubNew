using UnityEngine;
using UnityEngine.UI;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 掛在 CombatScene 裡的「退出」按鈕上。
    /// 只有在高塔模式戰鬥中才會顯示；一般劇情戰鬥會自動隱藏，不用另外分場景管理。
    /// 點下去會記錄目前樓層（若破紀錄）並回到高塔選單頁，放棄這場戰鬥（本次挑戰不會結束）。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TowerQuitButton : MonoBehaviour
    {
        void Start()
        {
            if (!TowerModeManager.IsActive)
            {
                gameObject.SetActive(false);
                return;
            }

            // 交給 refresher 記住原文，玩家中途切語言時這顆才跟得上
            var label = GetComponentInChildren<Text>();
            if (label != null) LocaleRefresher.For(gameObject).Track(label);

            GetComponent<Button>().onClick.AddListener(TowerModeManager.QuitRun);
        }
    }
}
