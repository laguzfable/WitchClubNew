using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 把「筆記」分頁按鈕塞進蝕之聖典（BranchMapUI）。
    ///
    /// ★ 為什麼用注入而不是改 prefab ★
    /// 跟 <see cref="MonsterCodexInjector"/> 同一個理由：BranchMapUI.prefab 的節點插槽
    /// 是照書頁美術排好的，多加一顆按鈕就得重新對位。用注入的話版面資料留在程式裡，
    /// 而且 prefab 之後怎麼改都不會把這顆按鈕弄丟。
    ///
    /// ★ 位置 ★
    /// 貼著既有的「回上一頁」按鈕擺（同一組錨點、同樣大小，往上讓開一個按鈕的高度），
    /// 這樣不管書頁美術怎麼縮放，兩顆都會待在一起。
    /// </summary>
    public static class NotebookTabInjector
    {
        const string ButtonName = "NotesTabButton";
        const string Label = "筆記";
        const int FontSize = 30;
        const float GapAbove = 12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            InjectRetryLoop().Forget();
        }

        static void OnSceneLoaded (Scene scene, LoadSceneMode mode) => InjectRetryLoop().Forget();

        static async UniTaskVoid InjectRetryLoop ()
        {
            // 聖典是從標題選單開的，玩家可能過很久才點進去，所以等的時間放寬。
            const float timeout = 30f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                if (Inject()) return;
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));
                elapsed += 0.25f;
            }
        }

        /// <returns>true 表示已經處理完（含「本來就注入過了」），不用再重試。</returns>
        static bool Inject ()
        {
            var map = FindBranchMap();
            if (map == null) return false;

            if (map.transform.Find(ButtonName) != null) return true;

            var back = FindBackButton(map.transform);
            if (back == null)
            {
                Debug.LogError("[NotebookTabInjector] 聖典底下找不到「回上一頁」按鈕，筆記分頁沒加上去。");
                return true; // 結構跟預期不一樣，再重試也沒用
            }

            var go = new GameObject(ButtonName, typeof(RectTransform));
            go.transform.SetParent(map.transform, false);

            // 錨點與大小完全照回上一頁那顆，只往上挪一個按鈕高——
            // 兩顆長得一樣、待在一起，玩家一看就知道是同一層的東西。
            var rect = (RectTransform)go.transform;
            rect.anchorMin = back.anchorMin;
            rect.anchorMax = back.anchorMax;
            rect.pivot = back.pivot;
            rect.sizeDelta = back.sizeDelta;
            rect.localScale = back.localScale;
            rect.anchoredPosition = back.anchoredPosition
                                    + new Vector2(0f, back.rect.height + GapAbove);

            // 透明底圖：Button 要有 Graphic 才收得到點擊，視覺上只留文字。
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);

            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<Text>();
            text.font = ResolveFont(back);
            text.fontSize = FontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = Label;

            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            outline.effectDistance = new Vector2(2.69f, -2.69f);

            go.AddComponent<Button>().onClick.AddListener(NotebookPanel.Open);

            Debug.Log("[NotebookTabInjector] ✅ 筆記分頁注入完成");
            return true;
        }

        /// <summary>
        /// 聖典沒打開時面板是隱藏的，FindObjectOfType 抓不到，所以用 FindObjectsOfTypeAll；
        /// 但它連專案裡的 prefab 資產也會撈進來，要靠 scene.IsValid() 過濾掉。
        /// （作法跟 MonsterCodexInjector.FindGalleryPanel 一致。）
        /// </summary>
        static BranchMapUI FindBranchMap ()
        {
            foreach (var ui in Resources.FindObjectsOfTypeAll<BranchMapUI>())
                if (ui != null && ui.gameObject.scene.IsValid())
                    return ui;
            return null;
        }

        static RectTransform FindBackButton (Transform root)
        {
            var back = root.GetComponentsInChildren<BranchMapBackButton>(true).FirstOrDefault();
            return back != null ? (RectTransform)back.transform : null;
        }

        /// <summary>沿用回上一頁那顆的字型，這樣兩顆看起來是同一組。</summary>
        static Font ResolveFont (RectTransform back)
        {
            var sibling = back.GetComponentInChildren<Text>(true);
            if (sibling != null && sibling.font != null) return sibling.font;

            var fromScene = Object.FindObjectsOfType<Text>()
                .Select(t => t.font)
                .FirstOrDefault(f => f != null && f.name.Contains("Noto"));
            if (fromScene != null) return fromScene;

            return Resources.GetBuiltinResource<Font>("Arial.ttf")
                   ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
        }
    }
}
