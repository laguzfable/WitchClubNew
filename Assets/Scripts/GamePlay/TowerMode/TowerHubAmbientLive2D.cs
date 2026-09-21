using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hexe.TowerMode
{
    /// <summary>
    /// 在女巫競技場的選單頁放一隻 Live2D 角色當氣氛。
    ///
    /// 為什麼要繞這麼一圈（別把它改成「直接把 prefab 放進場景」）：
    /// Naninovel 在每個場景都會啟動（EngineConfiguration 的 InitializeOnApplicationLoad +
    /// SceneIndependent），而且它會自己生兩台相機——MainCamera(depth 0) 的 cullingMask 是
    /// 「除了 UI 層以外的全部」，UICamera(depth 1) 則是「只有 UI 層」。兩台加起來涵蓋所有
    /// layer，所以場景裡任何世界空間物件都會被 Naninovel 多畫一次（畫面上會看到兩個錯開的
    /// 疊影），而且不管放哪一層都躲不掉。
    ///
    /// 解法是把角色停在畫面外很遠的地方，用一台專屬相機拍進 RenderTexture，再用 UI 顯示。
    /// 這樣前後順序純粹是 Canvas 的 sortingOrder 問題，跟 Naninovel 的相機堆疊完全脫鉤。
    ///
    /// 場景裡的 sortingOrder 分配：BGCanvas = -2、角色 = -1、按鈕的 Canvas = 0。
    /// </summary>
    public class TowerHubAmbientLive2D : MonoBehaviour
    {
        [Header("角色")]
        [Tooltip("要出現的 Live2D prefab。放多隻的話每次進來會隨機挑一隻。")]
        [SerializeField] List<GameObject> characterPrefabs = new List<GameObject>();

        [Tooltip("要擺的表情，填 Animator 裡的 trigger 名字。留空＝用預設（通常是 idle）。" +
                 "填多個的話每次進來隨機挑一個。sophia 可用：idle / talking / happy / sad / angry / beer。")]
        [SerializeField] List<string> appearances = new List<string>();

        [Header("隨機台詞")]
        [Tooltip("顯示台詞的 Text，放在角色旁邊。留空＝不顯示台詞。")]
        [SerializeField] Text lineLabel;

        [Tooltip("台詞候選，進入時隨機挑一句。用中文寫；英日文到 RuneEnTranslation 的對照表補，" +
                 "沒補的話會原樣顯示中文（跟畫面上其他還沒翻譯的文字一樣）。")]
        [SerializeField] List<string> lines = new List<string>();

        [Tooltip("每隔幾秒換一句。0 = 只在進入這一頁時挑一次。")]
        [SerializeField] float lineInterval = 0f;

        [Tooltip("走到「不再補血」那一層時，改講這一段（不隨機、也不會被換掉）。留空＝照常隨機。" +
                 "打贏會直接接下一場，只有這一層會特地停回休息頁，就是為了讓她講完這段。")]
        [TextArea(2, 4)]
        [SerializeField] string milestoneLine =
            "到第五十層啦。從這裡開始我不再幫妳補傷了——" + "\n" +
            "帶著傷繼續，還是收手，妳自己決定。";

        [Header("顯示位置（建議用這個）")]
        [Tooltip("在 Canvas 裡自己放一個 RawImage 拖進來，位置和大小就用 Scene 視圖直接拖拉調，" +
                 "不用進 Play 模式、調完也不會跑掉。留空的話腳本會自己生一個，改用下面那組數值控制。")]
        [SerializeField] RawImage targetImage;

        [Header("鏡頭")]
        [Tooltip("遠近。1 = 剛好框住整隻；越大越近。半身約 2、臉部特寫約 4~6。")]
        [Range(1f, 8f)] [SerializeField] float zoom = 1f;

        [Tooltip("對準高度。0 = 身體中央，1 = 頭頂，-1 = 腳底。拉近之後用這個對準要拍的部位（臉約 0.75）。")]
        [Range(-1f, 1f)] [SerializeField] float verticalAim = 0f;

        [Tooltip("對準左右。0 = 正中央。")]
        [Range(-1f, 1f)] [SerializeField] float horizontalAim = 0f;

        [SerializeField, Range(0f, 1f)] float opacity = 1f;

        [Tooltip("動作太寬（例如張開手臂、拿東西）時自動把顯示框加寬，避免被切到。" +
                 "只會動寬度，高度和大小完全不變。")]
        [SerializeField] bool autoFitWidth = true;

        [Header("沒指定 RawImage 時才會用到")]
        [Tooltip("角色「腳底」在畫面上的位置。x：0=最左、1=最右。")]
        [Range(0f, 1f)] [SerializeField] float footX = 0.3f;
        [Range(-0.5f, 1f)] [SerializeField] float footY = 0.02f;

        [Tooltip("角色高度佔畫面高度的比例。")]
        [Range(0.2f, 2.5f)] [SerializeField] float heightRatio = 1.1f;

        // 停在這裡，遠到不可能進到任何一台場景相機的視野
        static readonly Vector3 RigPosition = new Vector3(10000f, 10000f, 0f);

        const int CharacterSortingOrder = -1; // BGCanvas 是 -2、按鈕的 Canvas 是 0
        const int MaxTextureSize = 2048;
        const float EdgeMargin = 1.06f;       // 模型會動，四周留一點才不會在動作幅度大的時候被切到

        GameObject model;
        Camera captureCamera;
        RenderTexture renderTexture;
        RawImage display;
        bool ownsDisplay;     // display 是不是腳本自己生的（別人給的就不要亂動它的 RectTransform）

        bool framed;          // 是否已經量到模型大小
        Bounds fitBounds;     // 第一次量到的邊界。垂直取景一律用它，這樣大小和構圖不會隨動作跳動
        float modelAspect = 1f;
        int texW, texH;

        Renderer[] cachedRenderers;
        float maxHalfWidth;   // 動作過程中出現過的最大半寬（相對於取景中心）
        float widthWatchUntil; // 追到這個時間為止；動畫循環跑過一輪就夠了

        void Start()
        {
            var prefab = PickPrefab();
            if (prefab == null)
            {
                enabled = false;
                return;
            }

            BuildRig(prefab);
            SetupLine();
        }

        // ── 隨機台詞 ────────────────────────────────────────────────

        string currentLine;
        float nextLineAt;

        void SetupLine()
        {
            if (lineLabel == null) return;

            if (!IsMilestone && (lines == null || lines.FindAll(l => !string.IsNullOrWhiteSpace(l)).Count == 0))
            {
                lineLabel.gameObject.SetActive(false);
                return;
            }

            // 玩家在設定裡切語言時要跟著換。註冊一次就好，之後換句子只動 currentLine，
            // 這個 lambda 每次重繪都會讀到最新的那句。
            LocaleRefresher.For(gameObject).OnRefresh(RedrawLine);

            PickLine();
        }

        /// <summary>規則要變的那一層：固定講那一段，不隨機。</summary>
        bool IsMilestone => !string.IsNullOrWhiteSpace(milestoneLine)
                            && TowerModeManager.CurrentFloor == TowerModeManager.NoHealFromFloor;

        void PickLine()
        {
            if (IsMilestone)
            {
                currentLine = milestoneLine;
                nextLineAt = float.MaxValue;   // 不要被 lineInterval 換掉
                RedrawLine();
                return;
            }

            var candidates = lines.FindAll(l => !string.IsNullOrWhiteSpace(l));
            if (candidates.Count == 0) return;

            // 只有一句的時候就別挑了，不然連續挑到同一句看起來像壞掉
            var picked = candidates[Random.Range(0, candidates.Count)];
            if (candidates.Count > 1 && picked == currentLine)
                picked = candidates[(candidates.IndexOf(picked) + 1) % candidates.Count];

            currentLine = picked;
            nextLineAt = Time.time + Mathf.Max(1f, lineInterval);
            RedrawLine();
        }

        void RedrawLine()
        {
            if (lineLabel == null || string.IsNullOrEmpty(currentLine)) return;

            lineLabel.text = RuneEnTranslation.TranslateName(currentLine);
        }

        GameObject PickPrefab()
        {
            var candidates = characterPrefabs?.FindAll(p => p != null);
            if (candidates == null || candidates.Count == 0)
            {
                Debug.LogWarning("[TowerHubAmbientLive2D] 沒有指定任何角色 prefab，略過。");
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        string PickAppearance()
        {
            var candidates = appearances?.FindAll(a => !string.IsNullOrWhiteSpace(a));
            if (candidates == null || candidates.Count == 0) return null;

            return candidates[Random.Range(0, candidates.Count)];
        }

        void BuildRig(GameObject prefab)
        {
            var rig = new GameObject("~AmbientLive2DRig");
            rig.transform.position = RigPosition;

            // 專屬圖層：讓拍攝相機只看得到角色。沒有可用的自訂圖層時退回 Default，
            // 反正角色停在一萬單位外，別的相機照樣拍不到。
            var layer = LayerMask.NameToLayer("Live2D");
            if (layer < 0) layer = 0;

            model = Instantiate(prefab, rig.transform);
            model.transform.localPosition = Vector3.zero;
            SetLayerRecursively(model, layer);

            var camGo = new GameObject("CaptureCamera");
            camGo.transform.SetParent(rig.transform, false);

            captureCamera = camGo.AddComponent<Camera>();
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = 1f; // 量到模型大小後會覆寫
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明背景
            captureCamera.cullingMask = 1 << layer;
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 100f;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.depth = -100; // 只畫進 RenderTexture，不參與螢幕上的排序

            // Cubism 的 Z 排序模式需要一個參考相機，不指定的話部件前後可能會亂掉
            var live2D = model.GetComponent<Naninovel.Live2DController>();
            if (live2D != null)
            {
                live2D.SetRenderCamera(captureCamera);

                // 表情是靠 Animator 的 trigger 切的。名字打錯或這隻角色沒有這個表情時，
                // Live2DController 自己會擋掉並印警告，不會壞掉。
                var appearance = PickAppearance();
                if (!string.IsNullOrEmpty(appearance)) live2D.SetAppearance(appearance);
            }

            ResolveDisplay();
        }

        void ResolveDisplay()
        {
            if (targetImage != null)
            {
                display = targetImage;
                ownsDisplay = false;
            }
            else
            {
                var canvasGo = new GameObject("~AmbientLive2DCanvas");
                canvasGo.transform.SetParent(transform, false);

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = CharacterSortingOrder;

                var imageGo = new GameObject("Character");
                imageGo.transform.SetParent(canvasGo.transform, false);

                display = imageGo.AddComponent<RawImage>();
                ownsDisplay = true;
            }

            display.raycastTarget = false; // 不要擋住底下按鈕的點擊
            display.color = new Color(1f, 1f, 1f, opacity);
            display.enabled = false; // 對好框之前先不要顯示，免得閃一格歪掉的畫面
        }

        void LateUpdate()
        {
            if (lineInterval > 0f && currentLine != null && Time.time >= nextLineAt) PickLine();

            // Cubism 的網格要等模型跑過一次更新才有正確的邊界，所以量測不能放在 Start
            if (!framed)
            {
                if (!TryMeasureModel()) return;

                framed = true;
                display.enabled = true;
            }

            TrackWidth();
            ApplyLayout();
        }

        /// <summary>
        /// 量出模型實際佔多大。量到之後就不再重量，之後拉鏡頭參數都是拿這組邊界重算。
        /// </summary>
        bool TryMeasureModel()
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            var bounds = new Bounds();
            var any = false;
            foreach (var r in renderers)
            {
                // Cubism 在部件不可見時會把 MeshRenderer 關掉，所以這行同時濾掉了看不見的部件
                if (!r.enabled) continue;

                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            // 還沒建好網格的話 size 會是 0，這一格先跳過，下一格再試
            if (!any || bounds.size.y <= 0.0001f || bounds.size.x <= 0.0001f) return false;

            fitBounds = bounds;
            modelAspect = bounds.size.x / bounds.size.y;
            cachedRenderers = renderers;
            maxHalfWidth = bounds.extents.x;
            widthWatchUntil = Time.time + 6f; // 表情動畫循環一輪通常幾秒，追這麼久夠了
            return true;
        }

        /// <summary>
        /// 動作會讓角色變寬（張手、拿東西），第一幀量到的寬度不夠用。
        /// 這裡持續追最大半寬，只增不減——垂直取景不動，所以大小和構圖不受影響。
        /// </summary>
        void TrackWidth()
        {
            if (Time.time > widthWatchUntil || cachedRenderers == null) return;

            var centerX = captureCamera.transform.position.x;
            foreach (var r in cachedRenderers)
            {
                if (r == null || !r.enabled) continue;

                var b = r.bounds;
                var reach = Mathf.Max(b.max.x - centerX, centerX - b.min.x);
                if (reach > maxHalfWidth) maxHalfWidth = reach;
            }
        }

        void ApplyLayout()
        {
            var rect = display.rectTransform;
            float w, h;

            if (ownsDisplay)
            {
                h = Mathf.Max(64f, Screen.height * heightRatio);
                w = h * modelAspect;

                // pivot 放在底部中央，這樣 footY 就真的是「腳底站在哪」，很直覺
                rect.anchorMin = rect.anchorMax = new Vector2(footX, footY);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(w, h);
            }
            else
            {
                // 別人給的 RawImage：高度和位置完全尊重他們自己拉的
                h = Mathf.Max(1f, rect.rect.height);
                w = Mathf.Max(1f, rect.rect.width);

                // 唯一會動的是寬度：動作張太開的時候把框加寬，不然手臂會被切掉。
                // 高度沒動，所以角色大小和垂直構圖完全不變。
                if (autoFitWidth)
                {
                    var needAspect = (maxHalfWidth * EdgeMargin) / captureCamera.orthographicSize;
                    var needW = h * needAspect;
                    if (needW > w + 0.5f)
                    {
                        w = needW;
                        rect.sizeDelta = new Vector2(w, rect.sizeDelta.y);
                    }
                }
            }

            // 取景框的長寬比一律跟著顯示框走，所以畫面永遠不會被壓扁。
            // 想要臉部特寫就把 RawImage 拉成接近正方形，再用 zoom / verticalAim 對準。
            ApplyFraming(w / h);
            EnsureTexture(w, h);

            display.color = new Color(1f, 1f, 1f, opacity);
        }

        void ApplyFraming(float aspect)
        {
            var halfHeight = Mathf.Max(0.0001f, fitBounds.extents.y * EdgeMargin / zoom);

            captureCamera.orthographicSize = halfHeight;
            captureCamera.aspect = aspect;
            captureCamera.transform.position = new Vector3(
                fitBounds.center.x + fitBounds.extents.x * horizontalAim,
                fitBounds.center.y + fitBounds.extents.y * verticalAim,
                fitBounds.center.z - 10f);
        }

        void EnsureTexture(float displayWidth, float displayHeight)
        {
            // 超過上限要「等比」縮，不然貼圖的長寬比會跟顯示框對不上，畫面會被拉扁
            var shrink = Mathf.Min(1f, MaxTextureSize / Mathf.Max(displayWidth, displayHeight));
            var wantW = Mathf.Clamp(Mathf.RoundToInt(displayWidth * shrink), 64, MaxTextureSize);
            var wantH = Mathf.Clamp(Mathf.RoundToInt(displayHeight * shrink), 64, MaxTextureSize);

            // 差幾個 pixel 就重建太浪費，拉滑桿會卡；差超過 8 才重來
            if (renderTexture != null && Mathf.Abs(wantW - texW) < 8 && Mathf.Abs(wantH - texH) < 8)
                return;

            ReleaseTexture();

            texW = wantW;
            texH = wantH;

            renderTexture = new RenderTexture(texW, texH, 16, RenderTextureFormat.ARGB32)
            {
                name = "AmbientLive2D_RT",
                antiAliasing = 2,
            };

            captureCamera.targetTexture = renderTexture;
            display.texture = renderTexture;

            // 指定 targetTexture 會把相機的 aspect 覆寫成貼圖的比例，所以要在這之後再設一次
            captureCamera.aspect = displayWidth / displayHeight;
        }

        void ReleaseTexture()
        {
            if (renderTexture == null) return;

            if (captureCamera != null) captureCamera.targetTexture = null;
            if (display != null) display.texture = null;

            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        void OnDestroy() => ReleaseTexture();
    }
}
