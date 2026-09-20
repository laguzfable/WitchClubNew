using System.Collections.Generic;
using Naninovel;
using UnityEngine;

namespace Hexe.UI
{
    /// <summary>
    /// 設定畫面的「畫質」：只給兩個選項，一般跟省效能。取代 Naninovel 原本列出 6 個 Unity 畫質等級的選單。
    ///
    /// ★ 為什麼不直接把 Unity 的畫質等級砍成兩個 ★
    /// 玩家的設定存的是等級編號（0～5，預設 5＝Ultra）。砍掉的話舊設定會指到不存在的等級。
    /// 所以等級照舊，這裡只挑其中兩個用，舊設定的其他編號也照樣認得（見 IsSaver）。
    ///
    /// ★ 兩個選項實際差在哪 ★
    /// 這款是 2D 立繪＋Live2D，Unity 畫質裡光源、陰影、反射那些都用不到，真正有差的只有：
    /// - 一般：Ultra。2 倍反鋸齒，垂直同步（跟著螢幕更新率跑）。
    /// - 省效能：Medium（反鋸齒關、貼圖一樣是原尺寸，畫面不會糊），再把幀率鎖在 30。
    ///   Medium 本身是垂直同步，144Hz 螢幕上還是會跑到 144，所以要另外鎖。
    /// </summary>
    public class GraphicsPresetDropdown : ScriptableDropdown
    {
        [ManagedText("DefaultUI")]
        protected static string NormalOption = "一般";
        [ManagedText("DefaultUI")]
        protected static string SaverOption = "省效能";

        protected override void OnEnable ()
        {
            base.OnEnable();
            InitializeOptions();
            if (Engine.TryGetService<ILocalizationManager>(out var locale))
                locale.OnLocaleChanged += HandleLocaleChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            if (Engine.TryGetService<ILocalizationManager>(out var locale))
                locale.OnLocaleChanged -= HandleLocaleChanged;
        }

        // 選單順序：0＝一般、1＝省效能
        protected override void OnValueChanged (int value)
        {
            GraphicsPreset.Set(value == 1);
        }

        void InitializeOptions ()
        {
            UIComponent.ClearOptions();
            UIComponent.AddOptions(new List<string> { NormalOption, SaverOption });
            UIComponent.SetValueWithoutNotify(GraphicsPreset.IsSaver ? 1 : 0);
            UIComponent.RefreshShownValue();
        }

        void HandleLocaleChanged (string _) => InitializeOptions();
    }

    /// <summary>兩種畫質的實際設定。選單和開機時都走這裡。</summary>
    public static class GraphicsPreset
    {
        public const int NormalLevel = 5;   // Ultra
        public const int SaverLevel = 2;    // Medium
        const int SaverFrameRate = 30;

        /// <summary>
        /// 目前是不是省效能。Medium 以下都算——舊版選單選過 Very Low / Low 的玩家，
        /// 本來就是想省，歸到省效能。
        /// </summary>
        public static bool IsSaver => QualitySettings.GetQualityLevel() <= SaverLevel;

        public static void Set (bool saver)
        {
            var cameras = Engine.GetService<ICameraManager>();
            var level = saver ? SaverLevel : NormalLevel;
            // 走 CameraManager 才會記進 Naninovel 的設定存檔。
            if (cameras != null) cameras.QualityLevel = level;
            else QualitySettings.SetQualityLevel(level, true);
            ApplyFrameRate();
        }

        /// <summary>
        /// 幀率上限不在 Unity 的畫質等級裡，每次開遊戲要自己補上。
        /// 引擎初始化完（設定存檔已經讀進來、畫質等級已經套好）之後跑。
        /// </summary>
        public static void ApplyFrameRate ()
        {
            if (IsSaver)
            {
                QualitySettings.vSyncCount = 0;   // 垂直同步開著時 targetFrameRate 沒作用
                Application.targetFrameRate = SaverFrameRate;
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            Engine.OnInitializationFinished -= ApplyFrameRate;
            Engine.OnInitializationFinished += ApplyFrameRate;
            if (Engine.Initialized) ApplyFrameRate();
        }
    }
}
