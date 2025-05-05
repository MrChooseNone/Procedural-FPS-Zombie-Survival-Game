using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Mirror;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using NUnit.Framework.Constraints;
using Mono.Cecil;

public class InventoryItem : NetworkBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    private CanvasGroup canvasGroup;
    private bool slotAvailable;
    private bool isHandled = false;
    public int amount;
    public PlayerWallInteraction player;
    public bool StartExternal = false;
    public bool EndExternal = false;
    public bool isStackable = false;


    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>(); // Make sure the CanvasGroup exists!
        isHandled = false;
        
    }
    void Start(){
        gameObjectCanvas = GameObject.FindWithTag("PlayerCanvas");
        if(gameObjectCanvas != null){
            canvas = gameObjectCanvas.GetComponent<Canvas>();
            panel = gameObjectCanvas.transform.Find("InventoryPanel").GetComponent<RectTransform>();
            
            playerCam = gameObjectCanvas.GetComponentInParent<Camera>();
            player = gameObjectCanvas.GetComponentInParent<PlayerWallInteraction>();

        }
        
        
        if (playerCam == null)
        {
            Debug.LogError("Player camera not found!");
            
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("begin drag");
        //if (!isOwned) return;
         
        if(rectTransform == null){
            rectTransform = GetComponent<RectTransform>();
        }
        originalParent = transform.parent;
        currentSlot = originalParent.GetComponent<InventorySlot>();
        if(currentSlot.transform.parent.CompareTag("ExternalInventory")){
            StartExternal = true;
        }else{
            StartExternal = false;
        }
        

        // Set the item outside the normal UI hierarchy while dragging
        transform.SetParent(canvas.transform);
        canvasGroup.blocksRaycasts = false; // Allows dragging over UI elements
        isHandled = false;
        //Invoke("ReturnItem", 80f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //if (!isOwned) return;
        

            // Convert the screen position to a position in the Canvas
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out localPosition);
            
            // Update the position of the item based on the local position within the Canvas
            rectTransform.localPosition = localPosition;
        

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //if (!isOwned) return;
            
            GameObject target = eventData.pointerEnter;
            Debug.Log("Target: " + (target != null ? target.name : "None" ));
            if(target == null){
                Debug.Log("Dropped inside inventory but not on a slot. Returning to original position.");
                transform.SetParent(originalParent);
                transform.localPosition = currentSlot.Center.localPosition;
                canvasGroup.blocksRaycasts = true; // Enable raycasts again after dropping
                isHandled = true;
                return;
            }
            // If dropped inside the inventory, place it into a valid slot
            InventorySlot slot = target.GetComponent<InventorySlot>();
            InventoryManager1 inventoryManager = GetComponentInParent<InventoryManager1>();
            PlayerWallInteraction playerWallInteract = GetComponentInParent<PlayerWallInteraction>();
            ItemStack item = new ItemStack(null, 0, 0 ,null);
            if(StartExternal){
                item = playerWallInteract.externalInventory.GetItem(uniqueKey);
            }else {
                item = inventoryManager.GetItem(uniqueKey);
            }
            ItemData itemData = inventoryManager.FindItemData(item.itemName);
            if (target != null && slot != null)
            {  
                if(CheckInventorySlotAvailable(slot)){
                    if(slot.transform.parent.CompareTag("ExternalInventory") && playerWallInteract != null && item.itemName != null && !StartExternal){
                        if(playerWallInteract.externalInventory != null){
                            Debug.Log("EXTERNAL INVENTORY FOUND");
                            playerWallInteract.externalInventory.UpdateInventory(item, new ItemStack(null, 0, 0 ,null));
                            inventoryManager.CmdRemoveItem(uniqueKey, item.quantity);
                            transform.SetParent(target.transform);
                            transform.localPosition = slot.Center.localPosition;
                            transform.localScale = Vector3.one;
                            CmdMoveItem(target.transform.GetSiblingIndex(), inventoryManager, nameOfItem, currentSlot,new ItemStack(null, 0, 0 ,null));
                            currentSlot = slot;
                            Destroy(gameObject);
                            isHandled = true;
                        }
                    }else if (!slot.transform.parent.CompareTag("ExternalInventory")){

                        Debug.Log("Dropped on a valid inventory slot.");
                        transform.SetParent(target.transform);
                        transform.localPosition = slot.Center.localPosition;
                        transform.localScale = Vector3.one;
                        CmdMoveItem(target.transform.GetSiblingIndex(), inventoryManager, nameOfItem, currentSlot, item);
                        currentSlot = slot;
                        if(StartExternal ){
                            playerWallInteract.externalInventory.UpdateInventory(new ItemStack(null, 0, 0 ,null), item);
                            inventoryManager.CmdAddItem(nameOfItem, item.quantity, itemData.isStackable);
                            Destroy(gameObject);
                        }
                        isHandled = true;
                    }else{
                        Debug.Log("Dropped inside inventory but not on a slot. Returning to original position.");
                        transform.SetParent(originalParent);
                        transform.localPosition = currentSlot.Center.localPosition;
                        isHandled = true;
                    }
                }else {
                    Debug.Log("Dropped inside inventory but not on a slot. Returning to original position.");
                    transform.SetParent(originalParent);
                    transform.localPosition = currentSlot.Center.localPosition;
                    isHandled = true;
                }
            }
            else
            {
                // Check if dropped outside inventory
                if (!RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition))
                {
                    
                    
                    
                    Vector3 dropPosition = playerCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f));
                    Debug.Log("item amount" + item.quantity + item.quantityPerItem);
                    
                    if(Keyboard.current.leftShiftKey.isPressed){
                        Debug.Log("Dropped half outside inventory. Dropping into world.");
                        int dropAmount = (int)Mathf.Floor(item.quantity/2);
                        if(StartExternal) CmdDropItem(dropAmount, item.quantityPerItem, inventoryManager,nameOfItem, dropPosition, item.quantity, playerWallInteract.externalInventory);
                        else CmdDropItem(dropAmount, item.quantityPerItem, inventoryManager,nameOfItem, dropPosition, item.quantity, null); //drop half
                        isHandled = true;
                    }else {
                        if(StartExternal) CmdDropItem(item.quantityPerItem, item.quantityPerItem, inventoryManager, nameOfItem, dropPosition, item.quantity, playerWallInteract.externalInventory);
                        else CmdDropItem(item.quantityPerItem, item.quantityPerItem, inventoryManager, nameOfItem, dropPosition, item.quantity, null);
                        isHandled = true;
                    }
                    
                }
                else
                {
                    // Return to original slot if still inside the inventory
                    Debug.Log("Dropped inside inventory but not on a slot. Returning to original position.");
                    transform.SetParent(originalParent);
                    transform.localPosition = currentSlot.Center.localPosition;
                    isHandled = true;
                }
            }
            canvasGroup.blocksRaycasts = true; // Enable raycasts again after dropping
        
    }
    private void ReturnItem(){
        Debug.Log(isHandled);
        if(isHandled) return;
        Debug.Log("Dropped inside inventory but not on a slot. Returning to original position.");
        transform.SetParent(originalParent);
        transform.localPosition = currentSlot.Center.localPosition;
        canvasGroup.blocksRaycasts = true; // Enable raycasts again after dropping
    }

    public bool CheckInventorySlotAvailable(InventorySlot slot){
        Debug.Log("slot check" + slot.transform.childCount);
        if(slot.transform.childCount <= 4 ){
            return true;
        }
        return false;
    }


    [Command(requiresAuthority = false)]
    private void CmdDropItem(int amount, int quantityPerItem,InventoryManager1 inventoryManager, string nameOfItem, Vector3 dropPosition, int quantity, InventoryContainer inventoryContainer)
    {
        Debug.Log("inventorymanager " + inventoryManager);
        ItemData itemData = null;
        if(inventoryContainer != null){
            itemData = inventoryContainer.FindItemData(nameOfItem);
        }    
        else {
            itemData = inventoryManager.FindItemData(nameOfItem);
        }
        Debug.Log("item data in command " + itemData + itemData.prefab);
        
        float terrainHeight = Terrain.activeTerrain.SampleHeight(dropPosition);
        if(dropPosition.y <= terrainHeight){
            dropPosition.y = terrainHeight;
        }
        int iterations = Mathf.CeilToInt((float)amount / quantityPerItem);
        
            for(int i = 0; i < iterations; i++){

                GameObject droppedItem = Instantiate(itemData.prefab, dropPosition, Quaternion.identity);
                NetworkServer.Spawn(droppedItem);
            }
        

        if(inventoryContainer != null) RpcRemoveFromInventory(player.connectionToClient, amount, quantity, inventoryContainer);
        else RpcRemoveFromInventory(player.connectionToClient, amount, quantity, null);
    }
    
    [TargetRpc]
    private void RpcRemoveFromInventory(NetworkConnectionToClient target, int amount, int quantity, InventoryContainer inventoryContainer)
    {
        Debug.Log("remove " + target);
        // Reduce stack count or remove the item
        InventoryManager1 inventoryManager = null;
        if(inventoryContainer == null){
            
            inventoryManager = GetComponentInParent<InventoryManager1>();
            Debug.Log("inventorymanager " + inventoryManager + amount);
            inventoryManager.CmdRemoveItem(uniqueKey, amount);
        }else{
            Debug.Log("inventorymanager " + inventoryContainer + amount);
            inventoryContainer.CmdRemoveItem(uniqueKey, amount);
        }
        if(quantity <= amount){
            Debug.Log("Destroid the item");
            currentSlot.amount.text = "";
            currentSlot.nameOfItem.text = "";
            Destroy(gameObject);
            
        } else{
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdMoveItem(int newSlotIndex, InventoryManager1 inventoryManager1, string uniqueKey,InventorySlot currSlotIndex, ItemStack itemStack)
    {
        // Request the client-side update to move the item in the inventory
        
        
        RpcMoveItem(player.connectionToClient ,newSlotIndex, inventoryManager1, currSlotIndex, itemStack);
    }
    

    [TargetRpc]
    private void RpcMoveItem(NetworkConnectionToClient target, int newSlotIndex, InventoryManager1 inventoryManager1,InventorySlot currSlotIndex, ItemStack itemStack)
    {
        // Move the item visually on all clients
        if(itemStack.itemName != null){
            transform.SetSiblingIndex(newSlotIndex);
            
            inventoryManager1.CmdUpdateStack(itemStack, currSlotIndex);
        }else{

            transform.SetSiblingIndex(newSlotIndex);
            ItemStack currStack = inventoryManager1.GetItem(uniqueKey);
            inventoryManager1.CmdUpdateStack(currStack, currSlotIndex);
        }
    }
}
