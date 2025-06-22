using UnityEngine;

public class MapPanZoom : MonoBehaviour
{
    public float panSpeed = 1f;
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2f;

    public Vector2 limitX = new Vector2(-1000, 1000);
    public Vector2 limitY = new Vector2(-1000, 1000);

    private Vector3 lastMousePos;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // 左鍵拖曳平移
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            transform.localPosition += new Vector3(delta.x, delta.y, 0) * panSpeed;
            transform.localPosition = ClampPosition(transform.localPosition);
            lastMousePos = Input.mousePosition;
        }

        // 滾輪縮放（以滑鼠為中心）
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 beforeZoomPos = ScreenToLocalPosition(Input.mousePosition);

            float newScale = Mathf.Clamp(transform.localScale.x + scroll * zoomSpeed, minZoom, maxZoom);
            transform.localScale = new Vector3(newScale, newScale, 1);

            Vector3 afterZoomPos = ScreenToLocalPosition(Input.mousePosition);
            Vector3 zoomOffset = afterZoomPos - beforeZoomPos;

            transform.localPosition -= zoomOffset;
            transform.localPosition = ClampPosition(transform.localPosition);
        }
    }

    Vector3 ScreenToLocalPosition(Vector3 screenPos)
    {
        Vector3 worldPoint = cam.ScreenToWorldPoint(screenPos);
        Vector3 localPoint = transform.parent.InverseTransformPoint(worldPoint);
        return localPoint;
    }

    Vector3 ClampPosition(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, limitX.x, limitX.y);
        pos.y = Mathf.Clamp(pos.y, limitY.x, limitY.y);
        return pos;
    }
}
