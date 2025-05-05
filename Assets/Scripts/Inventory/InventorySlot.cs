using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Mirror;
using TMPro;

public class InventorySlot : NetworkBehaviour, IDropHandler
{
    public Transform Center;
    public TextMeshProUGUI nameOfItem;
    public TextMeshProUGUI amount;
    public void OnDrop(PointerEventData eventData)
    {
        InventoryItem item = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (item != null && item.isOwned)
        {
            item.transform.SetParent(transform);
        }
    }
}