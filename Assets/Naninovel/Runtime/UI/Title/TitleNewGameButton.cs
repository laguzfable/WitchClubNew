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
            if (string.IsNullOrEmpty(startScriptName))
            {
                Debug.LogError("Can't start new game: specify start script name in the settings.");
                return;
            }

            // 立刻強制隱藏 TitleMenu，不等任何 async 流程
            titleMenu.gameObject.SetActive(false);

            // 同時關掉場景中所有帶有 "Title" 或 "Canvas" 名稱的物件
            foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj == null) continue;
                if (obj.name.Contains("Title") || obj.name.Contains("Canvas"))
                    obj.SetActive(false);
            }

            if (!string.IsNullOrEmpty(titleScriptName))
            {
                var titleScript = await scriptManager.LoadScriptAsync(titleScriptName);
                if (titleScript != null && titleScript.LabelExists(titleLabel))
                {
                    scriptPlayer.ResetService();
                    scriptPlayer.PreloadAndPlayAsync(titleScript, label: titleLabel);
                }
            }

            stateManager.ResetStateAsync(excludeFromReset, () =>
            {
                // Reset 後再確認一次，防止 Naninovel 的 title state restore 重新啟用
                titleMenu.gameObject.SetActive(false);
                return scriptPlayer.PreloadAndPlayAsync(startScriptName);
            });
        }
    }
}
