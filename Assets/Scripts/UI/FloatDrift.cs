using UnityEngine;

namespace Hexe.UI
{
    /// <summary>
    /// 讓 UI 物件在原地做不規則的漂浮（上下左右都會動，而且不會重複）。
    /// 標題畫面的立繪用，取代原本那段所有角色共用、只有上下晃的 hao 動畫。
    ///
    /// ★ 為什麼用 Perlin 雜訊而不是 sin 波 ★
    /// sin 波是規律的來回，兩個角色只要頻率一樣就會同步擺動，看起來很假。
    /// Perlin 雜訊是平滑但沒有週期的隨機值，配上不同的 seed，每個角色的軌跡都不一樣。
    ///
    /// ★ 掛在哪 ★
    /// 掛在立繪本身（blue / red），不是掛在定位用的父物件（blue pos / red pos）。
    /// 腳本只負責「相對於原本位置的偏移」，所以父物件照樣拿來擺位置，兩者不衝突。
    ///
    /// ★ 記得先關掉 Animator ★
    /// hao.anim 直接覆寫 anchoredPosition，跟這個腳本會打架（而且動畫會贏）。
    /// 掛這個之前先把 Animator 元件取消勾選或移除。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatDrift : MonoBehaviour
    {
        [Header("漂浮幅度（像素）")]
        [Tooltip("左右擺動的最大距離")]
        [SerializeField] float amplitudeX = 25f;
        [Tooltip("上下擺動的最大距離。想維持以前那種以上下為主的感覺，就把 Y 設得比 X 大")]
        [SerializeField] float amplitudeY = 40f;

        [Header("速度")]
        [Tooltip("數字越大晃得越快。0.1～0.3 之間是自然的漂浮感")]
        [SerializeField] float speed = 0.15f;

        [Header("傾斜（選填）")]
        [Tooltip("輕微的左右擺頭，單位是度。設 0 就完全不轉。2～5 度就很明顯了")]
        [SerializeField] float tiltAngle = 0f;

        [Header("隨機種子")]
        [Tooltip("留 -1 = 每次執行隨機。想讓兩個角色固定有不同軌跡，就各自填不同的數字（例如 0 和 500）")]
        [SerializeField] float seed = -1f;

        RectTransform rect;
        Vector2 origin;
        float originZ;
        float seedX, seedY, seedT;

        void Awake ()
        {
            rect = GetComponent<RectTransform>();

            // 記住起點，之後所有位移都是相對它算的 ——
            // 這樣你在 Inspector 調位置、或用父物件擺位，這裡都不會覆蓋掉。
            origin = rect.anchoredPosition;
            originZ = rect.localEulerAngles.z;

            var s = seed >= 0f ? seed : Random.Range(0f, 1000f);

            // 三個維度各自取樣雜訊的不同區段，不然 X 和 Y 會同步，
            // 變成沿著一條斜線來回，反而更假。
            seedX = s;
            seedY = s + 137.13f;
            seedT = s + 421.77f;
        }

        void Update ()
        {
            var t = Time.time * speed;

            // PerlinNoise 回傳 0～1，減 0.5 再乘 2 得到 -1～1
            var dx = (Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f * amplitudeX;
            var dy = (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f * amplitudeY;

            rect.anchoredPosition = origin + new Vector2(dx, dy);

            if (Mathf.Approximately(tiltAngle, 0f)) return;

            var dz = (Mathf.PerlinNoise(seedT, t) - 0.5f) * 2f * tiltAngle;
            rect.localEulerAngles = new Vector3(0f, 0f, originZ + dz);
        }
    }
}
