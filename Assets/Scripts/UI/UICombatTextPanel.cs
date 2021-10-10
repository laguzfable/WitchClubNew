using System.Collections;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;

//Use Respawn tag
public class UICombatTextPanel : MonoBehaviour {

    [SerializeField]
    int totalCount = 30;

    int curIndex = 0;

    UICombatText[] txtArr;

    [SerializeField]
    UICombatText combatTxtPrefab;

    [SerializeField]
    Transform playerPos, enemyPos, systemPos;

    //List<UICombatText> displayTxtList = new List<UICombatText>();


    class TextContent
    {
        public string content;
        public ECombatTextType type;
        public bool isPlayer;
    }

    Queue<TextContent> contentQueue = new Queue<TextContent>();
    

    // Use this for initialization
    void Start () {

        txtArr = new UICombatText[totalCount];

    }

    public void DisplaySystemText(string content)
    {
        UICombatText txt = GetText();
        txt.Display(content, ECombatTextType.System, systemPos.position).Forget();
    }
    public void EnqueueText(string content, ECombatTextType type, bool isPlayer)
    {
        contentQueue.Enqueue(new TextContent() {content = content, type = type, isPlayer = isPlayer });
        DisplayAllContent().Forget();
    }

    public float popDelayTime = 0.25f;
    async UniTaskVoid DisplayAllContent()
    {
        while(contentQueue.Count > 0)
        {
            PopUpText(contentQueue.Dequeue());
            await UniTask.Delay(System.TimeSpan.FromSeconds(popDelayTime), cancellationToken:this.GetCancellationTokenOnDestroy());
        }
    }

    void PopUpText(TextContent txtContent)
    {
        UICombatText txt = GetText();
        txt.Display(txtContent.content, txtContent.type, txtContent.isPlayer ? playerPos.position : enemyPos.position).Forget();
    }

    UICombatText GetText()
    {
        var curTxt = txtArr[curIndex];

        if (curTxt == null)
        {
            curTxt = Instantiate(combatTxtPrefab, transform);
        }
        curIndex++;
        if (curIndex >= totalCount)
        {
            curIndex = 0;
        }
        return curTxt;
    }
}
