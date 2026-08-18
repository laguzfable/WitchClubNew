// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.


namespace Naninovel.UI
{
    public class TitleContinueButton : ScriptableButton
    {
        private IStateManager gameState;
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            gameState = Engine.GetService<IStateManager>();
            uiManager = Engine.GetService<IUIManager>();
        }

        /// <summary>Awake 抓服務時引擎不一定已經就緒（這個專案會在中途重載場景／重置狀態，
        /// UI 有機會在服務還沒接上的空檔被 Awake），所以用的時候再補抓一次。
        /// 抓不到就回 null，呼叫端自己判斷——總比直接 NullReferenceException 好。</summary>
        private IStateManager GameState =>
            gameState ?? (gameState = Engine.Initialized ? Engine.GetService<IStateManager>() : null);

        protected override void Start ()
        {
            base.Start();

            ControlInteractability();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            var state = GameState;
            if (state != null) state.GameSlotManager.OnSaved += ControlInteractability;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            var state = GameState;
            if (state != null) state.GameSlotManager.OnSaved -= ControlInteractability;
        }

        protected override void OnButtonClick ()
        {
            var saveLoadUI = uiManager.GetUI<ISaveLoadUI>();
            if (saveLoadUI is null) return;

            var lastLoadMode = saveLoadUI.GetLastLoadMode();
            saveLoadUI.PresentationMode = lastLoadMode;
            saveLoadUI.Show();
        }

        private void ControlInteractability () => UIComponent.interactable = gameState.AnyGameSaveExists;
    }
}
