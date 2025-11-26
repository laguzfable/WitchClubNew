// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Naninovel.Runtime.UI;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class CGGalleryGridSlot : ScriptableGridSlot
    {
        public override string Id => Data.Id;

        protected virtual CGSlotData Data { get; private set; }
        protected virtual RawImage ThumbnailImage => thumbnailImage;
        protected virtual Texture2D LockedTexture => lockedTexture;
        protected virtual Texture2D LoadingTexture => loadingTexture;
        protected virtual IReadOnlyList<Texture2D> CGTextures { get; private set; }
        protected virtual bool AnyUnlocked => CGTextures?.Any(t => t != null) ?? false;

        [SerializeField] private RawImage thumbnailImage = null;
        [SerializeField] private Texture2D lockedTexture = default;
        [SerializeField] private Texture2D loadingTexture = default;

        private IUnlockableManager unlockableManager;
        private ILocalizationManager localizationManager;
        private CGViewerPanel viewerPanel;
        
        // 🔥 新增：用於追蹤在 LoadAndHoldAsync 期間被「Hold」住的資源。
        private readonly List<string> heldResourcePaths = new List<string>();


        public void Initialize (CGViewerPanel viewerPanel)
        {
            this.viewerPanel = viewerPanel;
        }

public void Bind (CGSlotData data)
{
    Debug.Log($"[CG BIND] Bind called → Id={data.Id}, Paths={string.Join(",", data.TexturePaths)}");

    UnloadCGTextures();
    this.Data = data;
    Refresh();
}


protected virtual async UniTask LoadCGTexturesAsync ()
{
    var prevThumbnailImage = ThumbnailImage.texture;
    ThumbnailImage.texture = LoadingTexture;
    var textures = new Texture2D[Data.TexturePaths.Count];
    
    // 🔥 在載入新貼圖前，確保釋放舊的（雖然 UnloadCGTextures 在 Bind 時已呼叫，但這更保險）
    // 我們將在 UnloadCGTextures 裡處理釋放邏輯，這裡只需清空。
    heldResourcePaths.Clear(); 

    // === 我新增的 LOG：看實際收到的資料 ===
    Debug.Log($"[CG SLOT] SlotId = {Data.Id}");
    for (int i = 0; i < Data.TexturePaths.Count; i++)
        Debug.Log($"[CG SLOT] TexturePaths[{i}] = {Data.TexturePaths[i]} (raw path)");

    await UniTask.WhenAll(Data.TexturePaths.Select(LoadCGTextureAsync));
    CGTextures = textures;
    ThumbnailImage.texture = prevThumbnailImage;

    async UniTask LoadCGTextureAsync (string path)
    {
        // 🔥 關鍵：真正的 unlockableId 在這裡
        var unlockableId = PathToUnlockableId(path);

        Debug.Log($"[CG CHECK] Path = {path}");
        Debug.Log($"[CG CHECK] unlockableId = {unlockableId}");
        Debug.Log($"[CG CHECK] unlocked? {unlockableManager.ItemUnlocked(unlockableId)}");

        if (!unlockableManager.ItemUnlocked(unlockableId))
        {
            Debug.Log($"[CG CHECK] → Not unlocked: {unlockableId}");
            return;
        }

        var index = Data.TexturePaths.IndexOf(path);
        
        // 確保使用 LoadAndHoldAsync 或 Hold
        var resource = Data.TextureLoader.IsLoaded(path)
            ? Data.TextureLoader.GetLoadedOrNull(path)
            // 🔥 修正：確保貼圖被 LoadAndHoldAsync 載入，防止被釋放
            : await Data.TextureLoader.LoadAndHoldAsync(path, this);

        if (resource != null && resource.Object != null)
        {
            textures[index] = resource.Object;
            // 🔥 追蹤被持有的資源路徑，以便在 UnloadCGTextures 時正確釋放
            heldResourcePaths.Add(path); 
            Debug.Log($"[CG LOAD] Loaded AND HELD CG texture at index {index}");
        }
        else
        {
            Debug.LogWarning($"[CG LOAD] Failed to load or texture is null for path: {path}");
        }
    }
}


        public virtual void UnloadCGTextures ()
        {
            if (Data.TexturePaths is null) return;
            
            // 🔥 修正：只釋放我們追蹤到的資源，防止釋放不該釋放的
            foreach (var texturePath in heldResourcePaths)
                Data.TextureLoader?.Release(texturePath, this);
            
            heldResourcePaths.Clear(); // 清空追蹤清單
        }

        protected virtual void Refresh () => HandleItemUpdated(null);

protected override void Awake ()
{
    base.Awake();

    Debug.Log("[CG AWAKE] Awake Called");

    this.AssertRequiredObjects(ThumbnailImage, LockedTexture);

    unlockableManager = Engine.GetService<IUnlockableManager>();
    localizationManager = Engine.GetService<ILocalizationManager>();
    ThumbnailImage.texture = LoadingTexture;

    unlockableManager.OnItemUpdated += HandleItemUpdated;
    localizationManager.OnLocaleChanged += HandleLocaleChanged;
}


        protected override void OnDestroy ()
        {
            base.OnDestroy();
            
            UnloadCGTextures(); // 🔥 確保在銷毀時釋放資源

            if (unlockableManager != null)
                unlockableManager.OnItemUpdated -= HandleItemUpdated;
            if (localizationManager != null)
                localizationManager.OnLocaleChanged -= HandleLocaleChanged;
        }

protected virtual async void HandleItemUpdated (UnlockableItemUpdatedArgs _)
{
    Debug.Log($"[CG EVENT] HandleItemUpdated CALLED, Id={Id}");

    while (Id is null)
    {
        await UniTask.DelayFrame(1);
        if (!this) return;
    }

    await LoadCGTexturesAsync();

    if (!AnyUnlocked) ThumbnailImage.texture = LockedTexture;
    else ThumbnailImage.texture = CGTextures.FirstOrDefault(t => t != null);
}


        protected virtual void HandleLocaleChanged (string _) => Refresh();

protected override void OnButtonClick ()
{
    base.OnButtonClick();
    
    // 🔥 新增診斷 Log (用於最終確認)
    Debug.Log($"[CG CLICK] Slot clicked. AnyUnlocked: {AnyUnlocked}"); 
    if (AnyUnlocked)
    {
        Debug.Log($"[CG CLICK] Calling viewerPanel.Show() with {CGTextures.Count(t => t != null)} valid textures."); 
    }

    if (!AnyUnlocked)
    {
        Debug.LogWarning("[CG CLICK] Slot clicked but no unlocked CG. Ignored.");
        return; // ← 防止 viewerPanel.Show 空資料
    }

    viewerPanel.Show(CGTextures);
}


        private static string PathToUnlockableId (string path) => $"{CGGalleryPanel.CGPrefix}/{path}";
    }
}