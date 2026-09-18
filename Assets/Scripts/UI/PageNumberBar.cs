using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>會翻頁的東西。怪物圖鑑自己實作；Naninovel 的格子（ScriptableGrid）靠反射接上。</summary>
    public interface IPageSource
    {
        int CurrentPage { get; }
        int PageCount { get; }
        void SelectPage (int page);
    }

    /// <summary>
    /// 翻頁列中間那排數字：‹ 1 2 3 4 5 ›，目前這頁墊紫色菱形，點數字直接跳頁。
    /// 存讀檔和回憶模式共用。
    ///
    /// Naninovel 原本只有「上一頁／下一頁」加一個頁碼字。頁數超過 <see cref="maxVisible"/> 時
    /// 只列目前這頁附近的幾個，不然 99 格會排出 11 個數字。
    ///
    /// 數字按鈕從 <see cref="template"/> 複製出來，要改樣式就改那顆（它本身不會顯示）。
    /// </summary>
    public class PageNumberBar : MonoBehaviour
    {
        [Tooltip("要翻頁的對象：Naninovel 的格子（GameStateSlotsGrid、CGGalleryGrid），或實作 IPageSource 的元件。")]
        [FormerlySerializedAs("grid")]
        [SerializeField] private MonoBehaviour source = default;
        [Tooltip("數字按鈕的樣板：Button + 當作選中底的 Image + 子物件的 Text。")]
        [SerializeField] private Button template = default;
        [Tooltip("選中底：目前這頁才顯示。")]
        [SerializeField] private string currentMarkName = "Current";
        [SerializeField] private int maxVisible = 5;
        [SerializeField] private Color currentColor = new Color32(250, 240, 222, 255);
        [SerializeField] private Color normalColor = new Color32(74, 52, 48, 255);

        private readonly List<Button> buttons = new List<Button>();
        private IPageSource pages;
        private int shownPage = -1, shownCount = -1;

        void Awake ()
        {
            if (template) template.gameObject.SetActive(false);
            pages = source as IPageSource ?? ReflectedGrid.TryWrap(source);
            if (source && pages is null)
                Debug.LogError($"[PageNumberBar] {source.GetType().Name} 不能翻頁（不是 ScriptableGrid 也沒實作 IPageSource）。");
        }

        void LateUpdate ()
        {
            if (pages is null || !template) return;

            // 格子還沒初始化時 CurrentPage 是 0。
            var page = pages.CurrentPage;
            var count = pages.PageCount;
            if (page < 1 || count < 1) return;
            if (page == shownPage && count == shownCount) return;
            shownPage = page;
            shownCount = count;
            Rebuild();
        }

        void Rebuild ()
        {
            var visible = Mathf.Min(maxVisible, shownCount);
            var first = Mathf.Clamp(shownPage - visible / 2, 1, shownCount - visible + 1);

            while (buttons.Count < visible)
                buttons.Add(CreateButton());

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                var active = i < visible;
                button.gameObject.SetActive(active);
                if (!active) continue;

                var page = first + i;
                var current = page == shownPage;

                var label = button.GetComponentInChildren<Text>(true);
                if (label)
                {
                    label.text = page.ToString();
                    label.color = current ? currentColor : normalColor;
                }

                var mark = button.transform.Find(currentMarkName);
                if (mark) mark.gameObject.SetActive(current);

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => pages.SelectPage(page));
            }
        }

        Button CreateButton ()
        {
            var button = Instantiate(template, template.transform.parent, false);
            button.name = "Page";
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + buttons.Count + 1);
            button.gameObject.SetActive(true);
            return button;
        }

        /// <summary>
        /// Naninovel 的 ScriptableGrid 是泛型類別，沒有共用的介面可以轉型，
        /// 只好用反射去拿 CurrentPage / PageCount / SelectPage。只在 Awake 查一次。
        /// </summary>
        class ReflectedGrid : IPageSource
        {
            readonly object grid;
            readonly PropertyInfo current, count;
            readonly MethodInfo select;

            ReflectedGrid (object grid, PropertyInfo current, PropertyInfo count, MethodInfo select)
            {
                this.grid = grid;
                this.current = current;
                this.count = count;
                this.select = select;
            }

            public static ReflectedGrid TryWrap (object grid)
            {
                if (grid is null) return null;
                var type = grid.GetType();
                var current = type.GetProperty("CurrentPage");
                var count = type.GetProperty("PageCount");
                var select = type.GetMethod("SelectPage", new[] { typeof(int) });
                return current != null && count != null && select != null
                    ? new ReflectedGrid(grid, current, count, select)
                    : null;
            }

            public int CurrentPage => (int)current.GetValue(grid);
            public int PageCount => (int)count.GetValue(grid);
            public void SelectPage (int page) => select.Invoke(grid, new object[] { page });
        }
    }
}
