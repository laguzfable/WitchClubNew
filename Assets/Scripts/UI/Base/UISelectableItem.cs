using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UISelectableItem : MonoBehaviour {

    public UISelectableItemGroup group;

    [SerializeField]
    protected bool isSelect = false;

    Button btn;

    void Awake()
    {
        if(group == null)
        {
            group = GetComponentInParent<UISelectableItemGroup>();
        }
        if(group != null)
        {
            group.Add(this);
        }

        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
        Init();
    }

    protected virtual void Init()
    {

    }

    public virtual void OnClick()
    {
        group.OnSelect(this);
    }

    public virtual void Select(bool newSelectState)
    {
        isSelect = newSelectState;
    }

    public virtual bool IsSelect()
    {
        return isSelect;
    }
}
