using UnityEngine;

// 從地圖進入節點時要先設定好的變數（通常是好感度）
[System.Serializable]
public class VariablePreset
{
    public string name;   // Naninovel 自訂變數名稱，例如 affinity_Ved
    public int value;
}

// 「這個角色的好感度，要玩家曾經走過哪條線才算數」
[System.Serializable]
public class AffinityPreset
{
    [Tooltip("好感度變數名稱，例如 affinity_Ved")]
    public string variableName;

    [Tooltip("要曾經走過哪個節點，才算跟這個角色好過。\n" +
             "寫法同 parentId：沒有 label 就寫 nodeId，有的話寫 nodeId#label。\n" +
             "留空＝一律視為達標。\n" +
             "沒走過的話，從地圖進場時這個變數會被設成 Locked Affinity Value。")]
    public string requireVisited;
}

// 這是小型的資料類別
[System.Serializable]
public class BranchNode
{
    public string nodeId;       // @visitNode 對應ID
    public string displayName;  // UI上顯示文字
    public string scriptName;   // Naninovel 腳本名稱

    public string label;        // ⭐ 可選擇跳到腳本內的某個 label（不填＝從頭開始）

    // ⭐ 這個節點在劇情地圖上顯示的圖。不填＝沿用 NodeButton prefab 原本的底圖。
    //    未解鎖時顯示的是 BranchMapUI.lockedIcon，不是這張。
    public Sprite icon;

    // ⭐ 想讓同一個 label 在地圖上開好幾格時填這個（例如「好感深厚／疏離」兩種入口）。
    //    只影響地圖上的識別，解鎖判定仍然看 VisitKey，所以幾格會一起亮。
    public string variantId;

    // ⭐ 滑鼠移到這格時，右頁三角形裡要顯示的圖。留空＝三角形保持空白。
    //    跟 icon 是兩張不同的圖：icon 是書頁上那顆小寶石，preview 是右頁的大圖。
    public Sprite preview;

    // ⭐ 三角形周圍的關鍵字，順序對應 BranchMapUI.keywordTexts。
    //    給的比欄位少沒關係，多出來的欄位會清空。
    public string[] keywords;

    // ⭐ 不看解鎖紀錄，一律當成已解鎖。
    //    主線必經的那幾格（第一～四章、分歧點前）勾這個。它們跟其他格是同生共死的：
    //    有任何一格亮著就代表它們必定也走過了，所以「鎖住」的狀態只會出現在
    //    全新存檔、而那時整張地圖本來就是暗的，鎖了也沒有意義。
    public bool alwaysUnlocked;

    // ⭐ 這個節點要用哪個按鈕 prefab。留空＝用 BranchMapUI.nodeButtonPrefab。
    //    章節那幾格（羅馬數字）和路線那幾格（寶石）長相不同、行為一樣，
    //    所以是換 prefab 而不是換元件——點擊邏輯只留一份在 NodeButton。
    public GameObject buttonPrefab;

    // ⭐ 書頁版面用：這個節點要放在左頁的哪個插槽上。
    //    填 BranchMapUI.nodeSlotRoot 底下某個子物件的名字，位置直接在 Scene 裡拖。
    //    留空的話，書頁模式下這個節點不會生成（Console 會提醒）。
    //    舊的樹狀自動排版不看這個欄位。
    public string slotName;

    // ⭐ 從地圖進入這個節點時要覆寫的變數。留空＝全部沿用 BranchMapUI 的預設好感度。
    //    ★ 不要拿它偷改好感 ★ 那是在玩家看不到的情況下改數值。舊制度的分歧看好感
    //    （Ved>=50、Nel>=50），節點才需要先壓值；現在全部改看符文數，15 格都清空了。
    public VariablePreset[] variableOverrides;

    // ⭐ 這一格通往哪些結局（ACH_END_XX）。右頁會顯示：已收集的顯示名字、還沒拿到的顯示 ???。
    //    只填「分歧點落在這一格段落裡」的結局——更深的格子有自己的，重複掛上去會讓玩家
    //    以為在這一格就拿得到。名字對照在 EndingCatalog。
    public string[] endingIds;

    // ⭐ 進這一格之前，要不要先讓星塵出來問「這一輪你陪的是誰」。
    //    判準是「這一格之後還會走到地圖日」：夜晚儀式要好感（RitualGate），
    //    儀式贏了才給符文，符文數才決定分歧（RunesGreen()>=3 那些）。
    //    走不到地圖日的格子（結局段）問了也沒用；chapter3 開頭會把好感全部歸零，
    //    所以第一～三章那幾格問了也會被清掉，一樣不要勾。
    public bool askAffinity;

    // ⭐ 這一格專屬的星塵台詞，接在 BranchMapUI.stardustLines 後面。留空＝只講共通的那幾句。
    public string stardustLine;

    // ⭐ 樹狀排版用：父節點的 Key，留空代表這是樹根。
    //    Key 的寫法跟 VisitedNodeManager 一致：沒有 label 就寫 nodeId，
    //    有 label 就寫 "nodeId#label"（例如 chapter5blue#beforefinal_blue）。
    public string parentId;

    /// <summary>解鎖判定用的 Key，對應 @visitNode 記錄的內容。同一個 label 的所有變體共用。</summary>
    public string VisitKey
    {
        get { return string.IsNullOrEmpty(label) ? nodeId : nodeId + "#" + label; }
    }

    /// <summary>地圖上的唯一 Key，用於父子連線。有 variantId 的話會接在後面以免撞名。</summary>
    public string Key
    {
        get { return string.IsNullOrEmpty(variantId) ? VisitKey : VisitKey + "@" + variantId; }
    }
}


// 這是 ScriptableObject
[CreateAssetMenu(menuName = "BranchMap/BranchDatabase")]
public class BranchDatabase : ScriptableObject
{
    public BranchNode[] nodes;
}
