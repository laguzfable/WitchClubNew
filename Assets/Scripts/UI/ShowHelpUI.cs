using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ShowHelpUI : MonoBehaviour
{
    [SerializeField]
    Sprite[] infoSpr;
    Image img;
    int currentPage = 0;
    [SerializeField]
    Button[] btns;
    AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
    }

    public void ChangeInfoSpr(int addNum)
    {
        currentPage += addNum;
        if (currentPage <= 0)
        {
            currentPage = 0;
            btns[1].gameObject.SetActive(false);
        }
        else
        {
            btns[1].gameObject.SetActive(true);
            btns[0].gameObject.SetActive(true);
        } 
        if (currentPage >= infoSpr.Length - 1)
        {
            currentPage = infoSpr.Length - 1;
            btns[0].gameObject.SetActive(false);
            btns[2].gameObject.SetActive(true);
        }
        img.sprite = infoSpr[currentPage];
        audioSource.Play();
    }
}
