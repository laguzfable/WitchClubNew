// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using UnityEngine;

namespace Naninovel.UI
{
    public class TitleNewGameButton : ScriptableButton
    {
        private const string titleLabel = "OnNewGame";

        [Tooltip("Services to exclude from state reset when starting a new game.")]
        [SerializeField] private string[] excludeFromReset = new string[0];

        private string startScriptName;
        private string titleScriptName;
        private TitleMenu titleMenu;
        private IScriptPlayer scriptPlayer;
        private IStateManager stateManager;
        private IScriptManager scriptManager;

        protected override void Awake ()
        {
            base.Awake();

            scriptManager = Engine.GetService<IScriptManager>();
            startScriptName = scriptManager.StartGameScriptName;
            titleScriptName = scriptManager.Configuration.TitleScript;
            titleMenu = GetComponentInParent<TitleMenu>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();
            stateManager = Engine.GetService<IStateManager>();
            Debug.Assert(titleMenu && scriptPlayer != null);
        }

        protected override void Start ()
        {
            base.Start();

            if (string.IsNullOrEmpty(startScriptName))
                UIComponent.interactable = false;
        }

        protected override async void OnButtonClick ()
        {
            // --- 🔧 關掉你自訂的 Title Canvas ---
            foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj == null) continue;
                if (obj.name.Contains("Title") || obj.name.Contains("Canvas"))
                {
                    Debug.Log("[CustomTitle] Hide: " + obj.name);
                    obj.SetActive(false);
                }
            }

            // --- 原始 Naninovel 流程 ---
            if (string.IsNullOrEmpty(startScriptName))
            {
                Debug.LogError("Can't start new game: specify start script name in the settings.");
                return;
            }

            if (!string.IsNullOrEmpty(titleScriptName))
            {
                var titleScript = await scriptManager.LoadScriptAsync(titleScriptName);
                if (titleScript != null && titleScript.LabelExists(titleLabel))
                {
                    scriptPlayer.ResetService();
                    // 直接啟動，不等待
                    scriptPlayer.PreloadAndPlayAsync(titleScript, label: titleLabel);
                }
            }

            // 關掉內建 Title UI
            titleMenu.Hide();

            // 重置狀態並開始遊戲
            stateManager.ResetStateAsync(excludeFromReset,
                () => scriptPlayer.PreloadAndPlayAsync(startScriptName));
        }
    }
}
