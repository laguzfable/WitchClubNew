using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>怪物圖鑑的一格：立繪、名字、NEW 角標。內容由 <see cref="MonsterCodexView"/> 塞。</summary>
    public class MonsterCodexSlot : MonoBehaviour
    {
        /// <summary>沒遇過的怪物用這個顏色乘上去，變成剪影。</summary>
        static readonly Color LockedTint = new Color(0f, 0f, 0f, 0.8f);

        [SerializeField] private Button button = default;
        [Tooltip("外框、立繪、名字都放在這底下。最後一頁不夠填滿時只關這個，格子本身留著。")]
        [SerializeField] private GameObject content = default;
        [SerializeField] private Image portrait = default;
        [SerializeField] private Text nameLabel = default;
        [SerializeField] private GameObject newBadge = default;
        [SerializeField] private Color nameColor = new Color32(74, 52, 48, 255);
        [SerializeField] private Color lockedNameColor = new Color32(120, 95, 80, 255);

        public Button Button => button;
        public MobData Mob { get; private set; }

        public void Bind (MobData mob, bool unlocked, bool isNew, string displayName)
        {
            Mob = mob;
            content.SetActive(true);

            portrait.sprite = mob.sprite;
            portrait.enabled = mob.sprite != null;
            portrait.color = unlocked ? Color.white : LockedTint;

            nameLabel.text = unlocked ? displayName : "???";
            nameLabel.color = unlocked ? nameColor : lockedNameColor;

            if (newBadge) newBadge.SetActive(unlocked && isNew);
        }

        /// <summary>
        /// 最後一頁不夠填滿時用。整格 SetActive(false) 的話 GridLayoutGroup
        /// 會把後面的格子往前擠，所以只關內容。
        /// </summary>
        public void Clear ()
        {
            Mob = null;
            content.SetActive(false);
        }
    }
}
