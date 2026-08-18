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

            // Awake 抓服務時引擎不一定已經就緒（中途重載場景／重置狀態時，UI 有機會在
            // 服務還沒接上的空檔被 Awake），那樣下面的 stateManager 會是 null 而 NRE，
            // 開新遊戲就會停在這裡不動。點下去的當下補抓一次最保險。
            if (stateManager == null) stateManager = Engine.GetService<IStateManager>();
            if (scriptPlayer == null) scriptPlayer = Engine.GetService<IScriptPlayer>();
            if (scriptManager == null) scriptManager = Engine.GetService<IScriptManager>();
            if (titleMenu == null) titleMenu = GetComponentInParent<TitleMenu>();

            if (stateManager == null || scriptPlayer == null)
            {
                Debug.LogError("[TitleNewGameButton] Naninovel 服務拿不到，開新遊戲中止。" +
                               "（stateManager=" + (stateManager == null ? "null" : "ok") +
                               ", scriptPlayer=" + (scriptPlayer == null ? "null" : "ok") + "）");
                return;
            }

            // 立刻強制隱藏 TitleMenu，不等任何 async 流程
            if (titleMenu != null) titleMenu.gameObject.SetActive(false);

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
                if (titleMenu != null) titleMenu.gameObject.SetActive(false);
                return scriptPlayer.PreloadAndPlayAsync(startScriptName);
            });
        }
    }
}
