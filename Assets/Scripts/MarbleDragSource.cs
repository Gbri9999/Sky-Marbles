using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MarbleDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string marbleId;
    [SerializeField] private LockerController locker;
    [SerializeField] private Image portrait;

    private RectTransform ghost;
    private RectTransform canvasRect;

    public string MarbleId => marbleId;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!locker.IsMarbleOwned(marbleId)) return;

        canvasRect = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        GameObject imageObject = new GameObject("Marble Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasRect, false);
        ghost = (RectTransform)imageObject.transform;
        ghost.sizeDelta = new Vector2(86, 86);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = portrait.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            eventData.position, eventData.pressEventCamera, out Vector2 position);
        ghost.anchoredPosition = position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost != null) Destroy(ghost.gameObject);
    }
}
