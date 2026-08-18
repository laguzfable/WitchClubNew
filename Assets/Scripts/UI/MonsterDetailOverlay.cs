using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 點圖鑑裡的怪物之後蓋滿全螢幕的放大檢視：一張立繪 + 名字，點畫面任一處關掉。
    /// 沒有沿用 Naninovel 的 CGViewerPanel，因為那支是綁 CG 解鎖流程的 RawImage 檢視器，
    /// 這裡只要單純把 Sprite 放大而已。
    /// </summary>
    public class MonsterDetailOverlay : MonoBehaviour
    {
        Image portrait;
        Text nameLabel;

        public bool IsShown => gameObject.activeSelf;

        public static MonsterDetailOverlay Create (Transform uiRoot, Font font)
        {
            var go = MonsterCodexPanel.NewUIObject("MonsterDetailOverlay", uiRoot);
            MonsterCodexPanel.Stretch(go.GetComponent<RectTransform>());
            go.transform.SetAsLastSibling();

            var overlay = go.AddComponent<MonsterDetailOverlay>();

            var back = go.AddComponent<Image>();
            back.color = new Color(0f, 0f, 0f, 0.92f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(overlay.Hide);

            var portraitGo = MonsterCodexPanel.NewUIObject("Portrait", go.transform);
            overlay.portrait = portraitGo.AddComponent<Image>();
            overlay.portrait.preserveAspect = true;
            overlay.portrait.raycastTarget = false;
            var rect = overlay.portrait.rectTransform;
            rect.anchorMin = new Vector2(0.12f, 0.16f);
            rect.anchorMax = new Vector2(0.88f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlay.nameLabel = MonsterCodexPanel.CreateText(go.transform, "Name", string.Empty,
                34, FontStyle.Bold, TextAnchor.MiddleCenter, font);
            var nameRect = overlay.nameLabel.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.07f);
            nameRect.anchorMax = new Vector2(1f, 0.14f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var hint = MonsterCodexPanel.CreateText(go.transform, "Hint", "點擊任意處關閉",
                18, FontStyle.Normal, TextAnchor.MiddleCenter, font);
            hint.color = new Color(1f, 1f, 1f, 0.45f);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0.02f);
            hintRect.anchorMax = new Vector2(1f, 0.06f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;

            go.SetActive(false);
            return overlay;
        }

        public void Show (Sprite sprite, string displayName)
        {
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
            nameLabel.text = displayName;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide ()
        {
            gameObject.SetActive(false);
        }
    }
}
