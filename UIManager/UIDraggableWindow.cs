using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float dragAreaHeight = 56f;

    private RectTransform windowRect;
    private RectTransform parentRect;
    private Vector2 pointerOffset;
    private bool isDragging;

    private void Awake()
    {
        windowRect = transform as RectTransform;
        parentRect = windowRect != null ? windowRect.parent as RectTransform : null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (windowRect == null || parentRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                windowRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 windowPoint) ||
            windowPoint.y < windowRect.rect.yMax - dragAreaHeight ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 parentPoint))
        {
            isDragging = false;
            return;
        }

        pointerOffset = (Vector2)windowRect.localPosition - parentPoint;
        isDragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || windowRect == null || parentRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 parentPoint))
        {
            return;
        }

        Vector2 position = parentPoint + pointerOffset;
        Rect bounds = parentRect.rect;
        Rect windowBounds = windowRect.rect;

        float minX = bounds.xMin - windowBounds.xMin;
        float maxX = bounds.xMax - windowBounds.xMax;
        float minY = bounds.yMin - windowBounds.yMin;
        float maxY = bounds.yMax - windowBounds.yMax;

        if (minX <= maxX)
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY <= maxY)
            position.y = Mathf.Clamp(position.y, minY, maxY);

        windowRect.localPosition = new Vector3(
            position.x,
            position.y,
            windowRect.localPosition.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}
