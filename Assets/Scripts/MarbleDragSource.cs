using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MarbleDragSource : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [SerializeField] private string marbleId;
    [SerializeField] private LockerController locker;
    [SerializeField] private Image portrait;

    private RectTransform ghost;
    private RectTransform canvasRect;
    private bool equippingDrag;
    private bool carouselDrag;

    public string MarbleId => marbleId;
    public bool IsEquippingDrag => equippingDrag;

    public void Configure(string id, LockerController owner, Image marblePortrait)
    {
        marbleId = id;
        locker = owner;
        portrait = marblePortrait;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        equippingDrag = false;
        carouselDrag = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!equippingDrag && !carouselDrag)
        {
            Vector2 movement = eventData.position - eventData.pressPosition;
            if (movement.sqrMagnitude < 64f) return;
            if (Mathf.Abs(movement.x) >= Mathf.Abs(movement.y) ||
                !locker.IsMarbleOwned(marbleId))
            {
                carouselDrag = true;
                locker.BeginMarbleCarouselDrag();
            }
            else
            {
                equippingDrag = true;
                CreateGhost();
            }
        }

        if (carouselDrag)
        {
            locker.DragMarbleCarousel(eventData.delta.x);
            return;
        }
        if (ghost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            eventData.position, eventData.pressEventCamera, out Vector2 position);
        ghost.anchoredPosition = position;
    }

    private void CreateGhost()
    {
        canvasRect = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        GameObject imageObject = new GameObject("Marble Preview",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasRect, false);
        ghost = (RectTransform)imageObject.transform;
        ghost.sizeDelta = new Vector2(86f, 86f);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = portrait.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (carouselDrag) locker.EndMarbleCarouselDrag();
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        carouselDrag = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        locker.ScrollMarbleCarousel(eventData.scrollDelta.y);
    }
}
