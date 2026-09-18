using UnityEngine;
using UnityEngine.UI;

namespace Hexe.UI
{
    /// <summary>
    /// 分頁按鈕的字色：選中時是紫底淺字，沒選中是紙底深字。
    /// Toggle 本身只會切底圖，字色要另外跟著換。
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ToggleLabelColor : MonoBehaviour
    {
        [SerializeField] private Text label = default;
        [SerializeField] private Color onColor = new Color32(250, 240, 222, 255);
        [SerializeField] private Color offColor = new Color32(74, 52, 48, 255);

        private Toggle toggle;

        void Awake () => toggle = GetComponent<Toggle>();

        void OnEnable ()
        {
            toggle.onValueChanged.AddListener(Apply);
            Apply(toggle.isOn);
        }

        void OnDisable () => toggle.onValueChanged.RemoveListener(Apply);

        void Apply (bool on)
        {
            if (label) label.color = on ? onColor : offColor;
        }
    }
}
