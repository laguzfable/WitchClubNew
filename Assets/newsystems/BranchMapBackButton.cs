using Naninovel;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在劇情地圖（BranchMapUI）面板上的「回上一頁」按鈕。
/// 點擊後只是把 BranchMapUI 這個面板關掉，回到下面本來就開著的主選單（Title 選單）。
/// </summary>
[RequireComponent(typeof(Button))]
public class BranchMapBackButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        GetComponentInParent<BranchMapUI>()?.Hide();
    }
}
