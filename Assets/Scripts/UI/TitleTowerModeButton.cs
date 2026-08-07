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
            // titleMenu?.Hide() 預設會用 FadeTime 淡出，但接下來馬上就要換場景（高塔模式的
            // Title UI 是跨場景常駐的），淡出還沒播完畫面就已經切走了，殘留半透明的標題按鈕。
            // 這裡直接切走不用淡出。
            titleMenu?.ChangeVisibilityAsync(false, 0f).Forget();

            if (TowerModeManager.HasSavedRun)
                TowerModeManager.ResumeRun();
            else
                TowerModeManager.StartRun();
        }
    }
}
