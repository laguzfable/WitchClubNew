// Assets/Scripts/UI/CGNewBadge.cs
//
// 回憶模式（CG）格子上的 NEW 角標。
//
// ★ 為什麼不直接改 Naninovel 的 CGGalleryGridSlot ★
// 那是套件自帶的檔案，升級版本會被蓋掉。改成掛一個元件在 CG 格子外面，
// 定時掃一遍現場的格子、該標的標上去——作法比照 MonsterCodexInjector。
//
// ★ 為什麼要定時掃而不是只在 OnEnable 掃一次 ★
// CG 格子是分頁的，Naninovel 換頁時會把同一批 slot 重新 Bind 到別的 CG 上，
// 但不會通知外面。只在開啟時掃一次的話，翻到第二頁角標就對錯人了。

using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    public class CGNewBadge : MonoBehaviour
    {
        const float SyncInterval = 0.3f;
        const string BadgeName = "NewBadge";
        static readonly Color BadgeColor = new Color(1f, 0.86f, 0.45f);

        float nextSync;
        CGGalleryPanel gallery;
        bool wasVisible;

        void OnEnable ()
        {
            NewItemTracker.BeginVisit(NewItemTracker.CGs);
            nextSync = 0f;
        }

        void Update ()
        {
            // 進頁面就算看過，所以每次打開回憶模式都要開一輪新的瀏覽。
            // 不能只寫在 OnEnable——Naninovel 的 UI 是靠透明度隱藏的，不是 SetActive，
            // 關掉再開不會觸發 OnEnable，角標會一直停在第一次的狀態。
            if (gallery == null) gallery = GetComponentInParent<CGGalleryPanel>();
            var visibleNow = gallery != null && gallery.Visible;
            if (visibleNow && !wasVisible)
            {
                NewItemTracker.BeginVisit(NewItemTracker.CGs);
                nextSync = 0f;
            }
            wasVisible = visibleNow;

            if (Time.unscaledTime < nextSync) return;
            nextSync = Time.unscaledTime + SyncInterval;
            Sync();
        }

        void Sync ()
        {
            var unlockables = Engine.Initialized ? Engine.GetService<IUnlockableManager>() : null;
            if (unlockables == null) return;

            foreach (var slot in GetComponentsInChildren<CGGalleryGridSlot>(true))
            {
                if (slot == null || string.IsNullOrEmpty(slot.Id)) continue;

                // 這一格現在綁的是哪張 CG 會隨換頁改變，所以每次都重新判斷
                var unlocked = unlockables.ItemUnlocked(slot.Id)
                               || unlockables.ItemUnlocked("CG/" + slot.Id);
                var show = unlocked && NewItemTracker.IsNewThisVisit(NewItemTracker.CGs, slot.Id);

                SetBadge(slot.transform, show);
            }
        }

        void SetBadge (Transform slotRoot, bool show)
        {
            var existing = slotRoot.Find(BadgeName);

            if (!show)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var go = new GameObject(BadgeName, typeof(RectTransform));
            go.transform.SetParent(slotRoot, false);

            var text = go.AddComponent<Text>();
            text.text = "NEW";
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.color = BadgeColor;
            text.alignment = TextAnchor.UpperRight;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.font = slotRoot.GetComponentInChildren<Text>(true)?.font
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(-6f, -4f);
        }
    }
}
