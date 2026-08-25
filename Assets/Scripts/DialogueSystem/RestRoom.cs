using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestRoom : MonoBehaviour
{
    [SerializeField] private GameObject selectPanel;
    [Tooltip("「更換卡片」按鈕。留空的話會自動找 onClick 接到 ChangeCardType() 的那顆。")]
    [SerializeField] private Button changeCardBtn;
    [SerializeField] private Button chatBtn;

    private void Awake()
    {
        // 停用 Naninovel UI 輸入
        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = false;

        // 暫停劇情播放
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 啟用 RestRoom 攝影機，關閉 Naninovel 攝影機
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = true;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = false;
    }

    private void Start()
    {
        AffinityDebugOverlay.Show();

        // 根據變數決定是否可聊天
        bool canChat = false;
        Engine.GetService<ICustomVariableManager>().TryGetVariableValue("CanChat", out canChat);
        selectPanel.SetActive(canChat);
        chatBtn.interactable = canChat;

        ApplyCardVariantLock();

        // 沒有對話框的頁面，共用控制列（設定／回顧／回標題）
        Hexe.UI.SceneControlBar.Show();
    }

    /// <summary>
    /// 還沒跑到解鎖卡片型態的劇情（@unlockCardVariant）之前，「更換卡片」整顆藏起來。
    /// 那之前進去也只有四張原版可選，等於是一個沒有內容的頁面。
    /// </summary>
    private void ApplyCardVariantLock()
    {
        var btn = changeCardBtn != null ? changeCardBtn : FindChangeCardButton();
        if (btn == null)
        {
            Debug.LogWarning("[RestRoom] 找不到「更換卡片」按鈕，鎖定狀態沒套用。" +
                             "請把它拖進 changeCardBtn，或確認它的 onClick 有接 ChangeCardType()。");
            return;
        }

        var unlocked = CardVariantUnlock.AnyUnlocked;
        if (btn.gameObject.activeSelf != unlocked)
            btn.gameObject.SetActive(unlocked);
    }

    /// <summary>
    /// 沒指定的話，靠 onClick 的靜態接線反查——場景裡那顆叫 ChangeSkillButton (1)，
    /// 名字看不出用途，用方法名找比較不會挑錯。
    /// </summary>
    private Button FindChangeCardButton()
    {
        foreach (var btn in FindObjectsOfType<Button>())
            for (var i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                if (btn.onClick.GetPersistentMethodName(i) == nameof(ChangeCardType) &&
                    ReferenceEquals(btn.onClick.GetPersistentTarget(i), this))
                    return btn;
        return null;
    }

    public void ChangeSkill()
    {
        SceneManager.LoadScene("ChangeRuneScene");
    }

    public void ChangeCardType()
    {
        SceneManager.LoadScene("ChangeCardTypeScene");
    }

    public void Sleep()
    {
        // 關閉休息室攝影機，啟用 Naninovel 攝影機，並顯示 ContinueInputUI
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = true;

        // 回到暫存劇情段落
        NaniBridgeUtility.GoBackToSavedStory();
    }

    public void Chat()
    {
        selectPanel.SetActive(true);
    }

    public void SelectCharacter(string scriptName, string label)
    {
        var scriptParameter = new ScriptParameter();
        scriptParameter.scriptName = scriptName;

        if (!string.IsNullOrEmpty(label))
            scriptParameter.scriptLabel = label;

        DataService.Instance.scriptParameter = scriptParameter;

        // 設定不能再重複聊天
        Engine.GetService<ICustomVariableManager>().SetVariableValue("CanChat", "false");

        GotoNani();
    }

    private void GotoNani()
    {
        var advCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (advCamera != null) advCamera.enabled = false;

        var naniCamera = Engine.GetService<ICameraManager>().Camera;
        naniCamera.enabled = true;

        if (GameObject.FindObjectOfType<ContinueInputUI>() is ContinueInputUI continueUI)
            continueUI.Visible = true;

        SceneManager.LoadSceneAsync("NaniDialogTest");
    }

    public void BackToRoom()
    {
        selectPanel.SetActive(false);
    }
}
