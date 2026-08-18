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

        // Title 場景的裝飾物：標題插畫的 Canvas、粒子特效（ColorWave / TitleBokeh）。
        // 開新遊戲後 Naninovel 是在同一個場景繼續演劇本，這些東西留著不但會蓋在
        // 劇本畫面上（ColorWave 的 sortingOrder 比背景演員高），在「狀態重置完
        // 到第一張背景進來」的那幾幀還會直接穿幫。
        private static readonly string[] titleOnlyObjectNames =
            { "Title", "Canvas", "ColorWave", "Bokeh", "BackgroundLayer", "Blurred" };

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

            // 同時關掉場景中所有只屬於標題畫面的物件
            foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj == null) continue;
                foreach (var titleOnlyName in titleOnlyObjectNames)
                    if (obj.name.Contains(titleOnlyName))
                    {
                        obj.SetActive(false);
                        break;
                    }
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
