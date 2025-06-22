using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapCharacterSpawner : MonoBehaviour
{
    public GameObject characterIconPrefab; // 預設是含 Animator 的按鈕

    [System.Serializable]
    public class CharacterData
    {
        public string name;
        public Vector2 position; // anchoredPosition
        public string naninovelScript; // 劇本名稱
        public RuntimeAnimatorController animatorController; // 每個角色的動畫控制器
    }

    public List<CharacterData> availableCharacters = new List<CharacterData>();

    void Start()
    {
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            // 複製一份，避免 closure 捕捉錯誤
            var c = availableCharacters[i];

            var go = Instantiate(characterIconPrefab, transform);

            // ⭐⭐⭐ 保證生成的icon在UI最上層
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            var txt = go.GetComponentInChildren<Text>(true);
            var btn = go.GetComponentInChildren<Button>(true);
            var animator = go.GetComponentInChildren<Animator>(true);

            if (rect == null) Debug.LogError("RectTransform 是 null！Prefab 名稱: " + go.name);
            if (txt == null) Debug.LogError("Text 是 null！Prefab 名稱: " + go.name);
            if (btn == null) Debug.LogError("Button 是 null！Prefab 名稱: " + go.name);
            if (animator == null) Debug.LogError("Animator 是 null！Prefab 名稱: " + go.name);

            if (rect != null)
                rect.anchoredPosition = c.position;

            if (txt != null)
                txt.text = c.name;

            // 指定角色專屬動畫控制器
            if (animator != null && c.animatorController != null)
            {
                animator.runtimeAnimatorController = c.animatorController;

                // 強制播放第一個動畫 clip
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                    animator.Play(clips[0].name, -1, 0f);
            }

            if (btn != null)
            {
                // closure 捕捉，這樣每顆按鈕按下去都對應正確的 c
                string scriptName = c.naninovelScript;
                btn.onClick.AddListener(() => 回到劇本(scriptName));
            }
        }
    }

    public void 回到劇本(string 劇本名)
    {
        // 關閉地圖畫面
        gameObject.SetActive(false);

        // 改用 SceneLoader，會自動 loading 並切到劇本
        var sceneLoader = FindObjectOfType<SceneLoader>();
        if (sceneLoader)
        {
            Debug.Log($"[地圖] 透過 SceneLoader 跳轉 Naninovel 劇本: {劇本名}");
            sceneLoader.GotoScript(劇本名);
        }
        else
        {
            Debug.LogError("[地圖] SceneLoader 不在場景裡，無法跳轉！");
        }
    }
}
