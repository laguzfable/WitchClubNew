using Naninovel;
using Naninovel.UI;
using Hexe.TowerMode;

namespace Hexe.UI
{
    public class TitleTowerModeButton : ScriptableButton
    {
        TitleMenu titleMenu;

        protected override void Awake()
        {
            base.Awake();
            titleMenu = GetComponentInParent<TitleMenu>();
        }

        protected override void OnButtonClick()
        {
            titleMenu?.Hide();
            TowerModeManager.StartRun();
        }
    }
}
