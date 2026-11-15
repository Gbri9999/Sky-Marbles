using UnityEngine;
using UnityEngine.EventSystems;

public class MarbleDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private LockerController locker;

    public void OnDrop(PointerEventData eventData)
    {
        MarbleDragSource marble = eventData.pointerDrag?.GetComponent<MarbleDragSource>();
        if (marble != null) locker.EquipMarble(marble.MarbleId);
    }
}
