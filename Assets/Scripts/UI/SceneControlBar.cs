using System;
using System.Linq;
using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 「沒有對話框」的頁面用的共用控制列（休息室、換裝頁、競技場選單、地圖…）。
    /// 在場景的 Start() 裡呼叫 <see cref="Show"/> 即可，作法比照 AffinityDebugOverlay：
    /// 執行時直接生出來，不用每個場景各自做一份 UI。
    ///
    /// ★ 為什麼只有這三顆 ★
    /// SETTINGS / LOG / TITLE 都不依賴「玩家現在在哪個 Unity 場景」，所以在任何頁面按都安全。
    /// **存讀檔刻意不放**：Naninovel 的存檔不會記錄 Unity 場景，在這些頁面存檔的話，
    /// 讀回來時劇本位置對了、人卻會出現在讀檔當下的場景。要開放存讀檔，得先補一個
    /// 把場景名寫進存檔、讀檔後載回去的 IStatefulService&lt;GameStateMap&gt;。
    /// 女巫競技場更要注意：它的樓層/護身符存在 PlayerPrefs，跟 Naninovel 存檔是兩套系統。
    ///
    /// AUTO / SKIP 也不放——那是對話專用的，在換裝頁或地圖上沒有意義。
    /// </summary>
    public static class SceneControlBar
    {
        const string RootName = "SceneControlBar";

        // 樣式跟對話框那排對齊：白字 + 黑描邊（描邊參數與 ControlPanel 的標籤相同）。
        // 位置則刻意放右上角，不跟下緣的「點擊推進」區域打架。
        static readonly Color LabelColor = Color.white;
        static readonly Color OutlineColor = new Color(0f, 0f, 0f, 0.5f);
        static readonly Vector2 OutlineDistance = new Vector2(2.69f, -2.69f);

        const int FontSize = 26;
        const float ButtonWidth = 150f;
        const float ButtonHeight = 46f;
        const float Margin = 24f;

        /// <summary>
        /// 生出控制列。重複呼叫是安全的（已經有就不重做）。
        /// 想少放某顆就把對應參數設成 false——例如競技場選單頁本來就有自己的「回標題」。
        /// </summary>
        public static void Show (bool settings = true, bool log = true, bool title = true, bool notes = false)
        {
            if (GameObject.Find(RootName) != null) return;

            // 沒有 EventSystem 的場景（例如地圖）按鈕會完全沒反應，順手補一個
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // 不用 DontDestroyOnLoad：跟著場景走，換場景自動消失
            var rootGO = new GameObject(RootName);
            var canvas = rootGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // 蓋在場景 UI 上，但低於好感度除錯（999）
            rootGO.AddComponent<CanvasScaler>();
            rootGO.AddComponent<GraphicRaycaster>();

            var font = ResolveFont();
            var x = -Margin;

            if (title) x = AddButton(rootGO, font, x, "TITLE", ExitToTitle);
            // 筆記本預設不放這裡——它現在長在蝕之聖典裡（見 NotebookTabInjector）。
            // 之後若想讓地圖或休息室也開得到，呼叫時傳 notes:true 就會多出這一顆。
            if (notes) x = AddButton(rootGO, font, x, "NOTES", NotebookPanel.Open);
            if (log) x = AddButton(rootGO, font, x, "LOG", () => ShowUI<IBacklogUI>());
            if (settings) AddButton(rootGO, font, x, "SETTINGS", () => ShowUI<ISettingsUI>());
        }

        /// <summary>從右上角往左排，回傳下一顆的 x（都是負值，靠右錨點）。</summary>
        static float AddButton (GameObject root, Font font, float x, string label, Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(root.transform, false);

            // 透明底圖：Button 需要一個 Graphic 才收得到點擊，但視覺上只留文字，
            // 跟對話框那排一致（那排也是純文字沒有底）。
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);

            var rect = go.GetComponent<RectTransform>();
            // 錨在右上：畫面下緣是「點擊推進劇情」的區域（對話框的控制列也在那），
            // 功能鍵放下面很容易誤按。競技場選單頁把「放棄本次挑戰」放右上也是同樣的道理。
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rect.anchoredPosition = new Vector2(x, -Margin);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);

            var text = textGO.AddComponent<Text>();
            text.font = font;
            text.fontSize = FontSize;
            text.color = LabelColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = label;

            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            // 白字壓在亮背景上會看不見，跟對話框那排一樣加黑描邊
            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = OutlineColor;
            outline.effectDistance = OutlineDistance;

            return x - ButtonWidth;
        }

        static void ShowUI<T> () where T : class, IManagedUI
        {
            var ui = Engine.Initialized ? Engine.GetService<IUIManager>()?.GetUI<T>() : null;
            if (ui == null)
            {
                Debug.LogWarning($"[SceneControlBar] 拿不到 {typeof(T).Name}，這次不開。");
                return;
            }
            ui.Show();
        }

        static async void ExitToTitle ()
        {
            // 確認訊息沿用對話框那顆 TITLE 的同一段文字（同一個 ManagedText key，三語共用）
            var confirmationUI = Engine.Initialized
                ? Engine.GetService<IUIManager>()?.GetUI<IConfirmationUI>()
                : null;

            if (confirmationUI != null &&
                !await confirmationUI.ConfirmAsync(ControlPanelExitToTitleButton.ConfirmationMessage))
                return;

            await ExitToTitleCommand.RunAsync();
        }

        /// <summary>
        /// 標籤目前是純英文，內建 Arial 就夠。但之後若改成中文，內建 Arial 沒有中日文字會變方框，
        /// 所以優先找場景裡現成的字型（專案已統一成 NotoSansCJKtc）。
        /// </summary>
        static Font ResolveFont ()
        {
            var fromScene = UnityEngine.Object.FindObjectsOfType<Text>()
                .Select(t => t.font)
                .FirstOrDefault(f => f != null && f.name.Contains("Noto"));
            if (fromScene != null) return fromScene;

            // Unity 2019 沒有 LegacyRuntime.ttf（那是 2021.2+ 的名字），這裡用 Arial.ttf
            return Resources.GetBuiltinResource<Font>("Arial.ttf")
                   ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
        }
    }
}
