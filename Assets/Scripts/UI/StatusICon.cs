using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class StatusICon : MonoBehaviour
{
    [SerializeField]
    Sprite[] statusIcons;
    Image image;
    // Start is called before the first frame update
    private void Start()
    {
        image = gameObject.GetComponent<Image>();
    }

    public void SetStatus(int ID)
    {
        if(image == null) image = gameObject.GetComponent<Image>();
        switch (ID)
        {
            case 1:
                //暈眩,GBY
                break;
            case 2:
                //睡眠
                break;
            case 3:
                //護盾,RGB,B技能
                break;
            case 4:
                //無敵,GB
                break;
            case 5:
                //增加攻擊力,RBY
                break;
            case 6:
                //每回合回血
                break;
            case 7:
                //無限SP,RGY
                break;
            case 8:
                //反射盾,RB,Y技能
                break;
            case 9:
                //吸血盾,BY
                break;
            case 10:
                //補血打人,GY
                break;
            case 11:
                //中詛咒(每回合扣血),RY,勉強算是R技能(敵我都有)
                break; 
        }
        image.sprite = statusIcons[ID - 1];
    }
}
