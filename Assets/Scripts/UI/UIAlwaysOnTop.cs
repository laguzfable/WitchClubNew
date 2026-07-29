using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class UIAlwaysOnTop : MonoBehaviour
{
    void OnEnable() => transform.SetAsLastSibling();

    void Update() => transform.SetAsLastSibling();
}
