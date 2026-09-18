using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 回憶模式的兩個分頁：「CG」跟「怪物圖鑑」，掛在 CGGalleryUI 的 BrowserPanel 上。
    ///
    /// 以前整個怪物圖鑑是 MonsterCodexPanel 在執行時用程式畫出來的，Editor 裡看不到也調不了。
    /// 現在物件都在 CGGalleryUI.prefab 裡（由 GalleryLayoutBuilder 建），這支只管行為：
    /// 切分頁、塞格子內容、翻頁、點怪物放大、NEW 角標、收集成就。
    /// </summary>
    public class MonsterCodexView : MonoBehaviour, IPageSource
    {
        [Header("分頁")]
        [SerializeField] private Toggle cgTab = default;
        [SerializeField] private Toggle codexTab = default;
        [Tooltip("怪物圖鑑分頁上的字，會顯示收集數。")]
        [SerializeField] private Text codexTabLabel = default;
        [Tooltip("CG 分頁才顯示的東西：CG 格子、CG 的翻頁列。")]
        [SerializeField] private GameObject[] cgObjects = default;
        [Tooltip("怪物分頁才顯示的東西：怪物格子、怪物的翻頁列。")]
        [SerializeField] private GameObject[] codexObjects = default;

        [Header("怪物圖鑑")]
        [SerializeField] private MonsterCodexSlot[] slots = default;
        [SerializeField] private Button previousPageButton = default;
        [SerializeField] private Button nextPageButton = default;
        [SerializeField] private MonsterDetailOverlay overlay = default;

        private int currentPage = 1;
        private bool codexActive;
        private CGGalleryPanel gallery;
        private bool wasVisible;

        public int CurrentPage => currentPage;
        // 注意：這裡會讀 MonsterCodex.AllMobs（會載入全部立繪），所以只在怪物分頁開著時才被問。
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(MonsterCodex.AllMobs.Count / (float)ItemsPerPage));
        int ItemsPerPage => Mathf.Max(1, slots.Length);

        void Awake ()
        {
            cgTab.onValueChanged.AddListener(on => { if (on) SelectTab(false); });
            codexTab.onValueChanged.AddListener(on => { if (on) SelectTab(true); });
            previousPageButton.onClick.AddListener(() => SelectPage(currentPage - 1));
            nextPageButton.onClick.AddListener(() => SelectPage(currentPage + 1));
            foreach (var slot in slots)
            {
                var s = slot;
                s.Button.onClick.AddListener(() => OnSlotClicked(s));
            }
        }

        void OnEnable ()
        {
            // NEW 角標：每次打開都算一次新的瀏覽。這裡不能寫在 Awake——
            // 面板一直活著，只是被開開關關，Awake 整場遊戲只跑一次。
            NewItemTracker.BeginVisit(NewItemTracker.Monsters);

            // 每次重新打開回憶模式都回到 CG 分頁
            ShowCGTab();

            // 「CG 全收集」成就：CG 是 Naninovel 在管的，沒有解鎖當下的 hook 可以掛，
            // 所以在玩家打開回憶模式時補檢查一次。
            CGGalleryProgress.CheckAchievement(GetComponentInParent<CGGalleryPanel>());
        }

        void Update ()
        {
            if (overlay && overlay.IsShown && Input.GetKeyDown(KeyCode.Escape))
                overlay.Hide();

            WatchVisibility();
        }

        // Naninovel 的 UI 是靠透明度隱藏的，不是 SetActive，關掉再打開不會觸發 OnEnable。
        // 要自己盯著可見狀態的變化，否則「一次瀏覽」整場遊戲只會開始一次，角標要重開遊戲才會消。
        void WatchVisibility ()
        {
            if (!gallery) gallery = GetComponentInParent<CGGalleryPanel>();

            var visibleNow = gallery && gallery.Visible;
            if (visibleNow && !wasVisible)
            {
                NewItemTracker.BeginVisit(NewItemTracker.Monsters);
                ShowCGTab();
                CGGalleryProgress.CheckAchievement(gallery);
            }
            wasVisible = visibleNow;
        }

        void ShowCGTab ()
        {
            currentPage = 1;
            if (cgTab.isOn) SelectTab(false);
            else cgTab.isOn = true;   // 會觸發 onValueChanged → SelectTab(false)
        }

        void SelectTab (bool codex)
        {
            codexActive = codex;

            foreach (var go in cgObjects)
                if (go) go.SetActive(!codex);
            foreach (var go in codexObjects)
                if (go) go.SetActive(codex);

            // 這裡不去碰 MonsterCodex.AllMobs：CGGalleryUI 是遊戲一開機就建好的，
            // 先讀清單等於把全部怪物立繪常駐在記憶體裡。所以留到玩家真的點進怪物分頁才載。
            if (codex)
            {
                currentPage = Mathf.Clamp(currentPage, 1, PageCount);
                Refresh();
            }
            else
            {
                if (overlay) overlay.Hide();
                if (codexTabLabel) codexTabLabel.text = "怪物圖鑑";
            }
        }

        public void SelectPage (int page)
        {
            var clamped = Mathf.Clamp(page, 1, PageCount);
            if (clamped == currentPage) return;
            currentPage = clamped;
            Refresh();
        }

        void Refresh ()
        {
            if (!codexActive) return;

            var mobs = MonsterCodex.AllMobs;
            var offset = (currentPage - 1) * ItemsPerPage;

            for (int i = 0; i < slots.Length; i++)
            {
                var index = offset + i;
                if (index >= mobs.Count)
                {
                    slots[i].Clear();
                    continue;
                }

                var mob = mobs[index];
                var unlocked = MonsterCodex.IsSeen(mob);
                var isNew = NewItemTracker.IsNewThisVisit(NewItemTracker.Monsters, mob.name);
                slots[i].Bind(mob, unlocked, isNew, DisplayNameOf(mob));
            }

            previousPageButton.interactable = currentPage > 1;
            nextPageButton.interactable = currentPage < PageCount;

            if (codexTabLabel) codexTabLabel.text = $"怪物圖鑑  {MonsterCodex.SeenCount} / {mobs.Count}";
        }

        void OnSlotClicked (MonsterCodexSlot slot)
        {
            if (!codexActive || slot.Mob == null) return;
            if (!MonsterCodex.IsSeen(slot.Mob)) return;
            if (overlay) overlay.Show(slot.Mob.sprite, DisplayNameOf(slot.Mob), QuoteOf(slot.Mob));
        }

        /// <summary>Naninovel 文字檔的分類名＝Resources/Naninovel/Text 底下的檔名。</summary>
        public const string QuoteCategory = "MonsterQuotes";

        /// <summary>
        /// 圖鑑台詞，代號是 MobData 的檔名。跟筆記本（Notes.txt）同一套：原文只寫中文，
        /// 翻譯放 Localization/語言/Text/MonsterQuotes.txt，Naninovel 會照目前語言挑。
        /// </summary>
        static string QuoteOf (MobData mob)
        {
            var text = Engine.GetService<ITextManager>()?.GetRecordValue(mob.name, QuoteCategory);
            return string.IsNullOrWhiteSpace(text) ? null : text.Replace("<br>", "\n");
        }

        static string DisplayNameOf (MobData mob)
        {
            return string.IsNullOrEmpty(mob.displayName) ? mob.name : mob.displayName;
        }
    }
}
