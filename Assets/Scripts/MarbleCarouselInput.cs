using UnityEngine;
using UnityEngine.EventSystems;

public class MarbleCarouselInput : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private LockerController locker;

    public void Configure(LockerController owner)
    {
        locker = owner;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        locker.BeginMarbleCarouselDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        locker.DragMarbleCarousel(eventData.delta.x);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        locker.EndMarbleCarouselDrag();
    }

    public void OnScroll(PointerEventData eventData)
    {
        locker.ScrollMarbleCarousel(eventData.scrollDelta.y);
    }
}
