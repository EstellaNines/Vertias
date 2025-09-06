using UnityEngine;
using UnityEngine.EventSystems;

public class MapZoomAndPan : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Header("必要引用")]
    public RectTransform mapContent;   // Map(Image) 的 RectTransform
    public RectTransform viewport;     // MapRight 的 RectTransform（即显示区域）

    [Header("缩放限制")]
    public float minZoom = 1.0f;
    public float maxZoom = 3.0f;
    public float zoomSpeed = 0.05f;

    // 缓存
    private Vector2 prevPointerLocal; // 上一帧的鼠标局部坐标
    private Vector2 contentStartLocal; // 地图初始位置

    public void OnBeginDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, e.position, e.pressEventCamera, out prevPointerLocal);

        contentStartLocal = mapContent.anchoredPosition;
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 currentPointer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, e.position, e.pressEventCamera, out currentPointer);

        Vector2 offset = currentPointer - prevPointerLocal;
        mapContent.anchoredPosition += offset;

        prevPointerLocal = currentPointer;

        ClampMapPosition();
    }

    public void OnScroll(PointerEventData e)
    {
        float delta = e.scrollDelta.y;
        float scaleFactor = 1 + delta * zoomSpeed;
        Vector3 newScale = mapContent.localScale * scaleFactor;
        float clampedScale = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale = Vector3.one * clampedScale;

        Vector2 pointerLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapContent, e.position, e.pressEventCamera, out pointerLocal);

        Vector2 pivotDelta = pointerLocal * (1 - newScale.x / mapContent.localScale.x);
        mapContent.localScale = newScale;
        mapContent.anchoredPosition += pivotDelta;

        ClampMapPosition();
    }

    private void ClampMapPosition()
    {
        Vector2 mapSize = mapContent.rect.size * mapContent.localScale.x;
        Vector2 viewportSize = viewport.rect.size;

        float halfMapWidth = mapSize.x / 2;
        float halfMapHeight = mapSize.y / 2;
        float halfViewWidth = viewportSize.x / 2;
        float halfViewHeight = viewportSize.y / 2;

        Vector2 pos = mapContent.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -halfMapWidth + halfViewWidth, halfMapWidth - halfViewWidth);
        pos.y = Mathf.Clamp(pos.y, -halfMapHeight + halfViewHeight, halfMapHeight - halfViewHeight);
        mapContent.anchoredPosition = pos;
    }
}