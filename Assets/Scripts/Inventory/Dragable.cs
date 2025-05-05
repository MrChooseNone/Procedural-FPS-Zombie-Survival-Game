using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Mirror;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class Dragable : NetworkBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    
    private Transform originalParent;

    // Reference to the inventory slot that this item is assigned to
    private InventorySlot currentSlot;
    public GameObject gameObjectCanvas;
    public Canvas canvas;

    public GameObject Prefab;
    private RectTransform panel;
    public string nameOfItem;
    private Camera playerCam;
    public string uniqueKey;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
    }
    void Start(){
        gameObjectCanvas = GameObject.FindWithTag("PlayerCanvas");
        if(gameObjectCanvas != null){
            canvas = gameObjectCanvas.GetComponent<Canvas>();
            panel = gameObjectCanvas.transform.Find("InventoryPanel").GetComponent<RectTransform>();


        }
        
            playerCam = gameObjectCanvas.GetComponentInParent<Camera>();
        
        if (playerCam == null)
        {
            Debug.LogError("Player camera not found!");
            
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isOwned) return;
         
        Debug.Log("begin drag");

        //originalParent = transform.parent;
        //currentSlot = originalParent.GetComponent<InventorySlot>();
        

        // Set the item outside the normal UI hierarchy while dragging
        transform.SetParent(canvas.transform);
        
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isOwned) return;
        

            // Convert the screen position to a position in the Canvas
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out localPosition);
            
            // Update the position of the item based on the local position within the Canvas
            rectTransform.localPosition = localPosition;
        

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isOwned) return;
        
            GameObject target = eventData.pointerEnter;
            Debug.Log("Target: " + (target != null ? target.name : "None" ));

            // If dropped inside the inventory, place it into a valid slot
            
                Debug.Log("Dropped on .");
                
                // transform.localPosition = eventData.position;
                transform.SetParent(panel.transform);
            
            
        
    }


    
}
