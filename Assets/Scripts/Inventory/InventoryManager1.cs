using UnityEngine;
using UnityEngine.UI;

using Mirror;
using System.Collections.Generic;


public class InventoryManager1 : NetworkBehaviour
{
    public GameObject inventorySlotPrefab;
    public Transform inventoryPanel;
    public GameObject itemPrefab;  // Reference to the item prefab
    public int inventorySize = 10;
    public bool inventoryFull = false;
    //public bool isPickingUp = false;
    //--------------slotSprites-----------

    public Sprite[] SlotSprites;
    
    [SerializeField]
    private List<GameObject> slots = new List<GameObject>();
    
    public Dictionary<string, ItemStack> inventory = new Dictionary<string, ItemStack>(); // Key: item name, Value: ItemStack
    

    void Start()
    {
        if (!isLocalPlayer ) return;
        
        foreach(Transform child in inventoryPanel){
            if(child.CompareTag("Slot")){
                Debug.Log("Prepared slots");
                slots.Add(child.gameObject);
            }
        }

        // for (int i = 0; i < inventorySize; i++)
        // {
        //     // GameObject slot = Instantiate(inventorySlotPrefab, inventoryPanel);
        //     // Image tempSprite = slot.GetComponent<Image>();
        //     // if(SlotSprites[i] != null){
        //     //     tempSprite.sprite = SlotSprites[i];
        //     // }else{
        //     //     tempSprite.sprite= SlotSprites[0];
        //     // }
        //     // slots.Add(slot);

        // }
    }

    [Command]
    public void CmdAddItem(string itemName, int amount , bool isStackable)
    {
        //if(!isPickingUp)
         RpcAddItem(connectionToClient, itemName, amount, isStackable);
    }

    [TargetRpc]
    private void RpcAddItem(NetworkConnectionToClient target, string itemName, int amount, bool isStackable)
    {
        CheckInventoryFull();
        if(!inventoryFull){
            //isPickingUp = true;
            if (isStackable)
            {
                // Stackable item logic: Increase quantity if already in inventory
                ItemStack existingStack = CheckIfItemExists(itemName);
                ItemData itemData = FindItemData(itemName);
                if (existingStack.itemName != null)
                {
                    // Get the existing stack, modify it, and put it back in the dictionary
                    Debug.Log("stacked!!");
                    existingStack.quantity += amount;
                    inventory[existingStack.uniqueKey] = existingStack; // Update the dictionary with modified stack
                    CmdUpdateStack(existingStack, null);  // Notify client about the updated stack
                }
                else if (inventory.Count < inventorySize)
                {
                    string uniqueKey = itemName + System.Guid.NewGuid().ToString();
                    ItemStack newStack = new ItemStack(itemName, amount, itemData.quantityPerItem, uniqueKey);
                    inventory.Add(uniqueKey, newStack);  // Add stackable item to inventory
                    CmdUpdateInventory(newStack, uniqueKey,isStackable);  // Notify client about the new stack
                }
            }
            else
            {
                // Unstackable item logic: Each item is treated as a unique instance
                for (int i = 0; i < amount; i++)
                {
                    if (inventory.Count < inventorySize)
                    {
                        Debug.Log("not stackable");
                        // Use a GUID to ensure uniqueness
                        string uniqueKey = itemName + System.Guid.NewGuid().ToString();
                        ItemStack newItem = new ItemStack(itemName, 1, 1, uniqueKey);  // Quantity of 1 for unstackable items
                        inventory.Add(uniqueKey, newItem);  // Add item to inventory with a unique key
                        CmdUpdateInventory(newItem,uniqueKey,isStackable);  // Notify client about the new unstackable item
                    }
                }
            }
        }
    }
    public ItemStack CheckIfItemExists(string itemName)
    {
        // Iterate through the inventory dictionary
        foreach (var entry in inventory)
        {
            // Compare the itemName of the current ItemStack with the passed itemName
            if (entry.Value.itemName == itemName)
            {
                // Item found with matching itemName
                return entry.Value;
            }
        }
        // No matching itemName found
        return new ItemStack(null, 0, 0, null);  // Or return null if you prefer
    }



    [Command(requiresAuthority = false)]
    public void CmdUpdateStack(ItemStack existingStack, InventorySlot currSlot)
    {
        RpcUpdateStack(connectionToClient,existingStack, currSlot);
    }
    [Command]
    public void CmdUpdateInventory(ItemStack newStack, string uniqueKey, bool stackable)
    {
        RpcUpdateInventory(connectionToClient, newStack, uniqueKey, stackable);
    }

    // [TargetRpc]
    // private void RpcUpdateInventory(NetworkConnectionToClient target, ItemStack newStack)
    // {
    //     foreach (GameObject slot in slots)
    //     {
    //         if (slot.transform.childCount == 0)
    //         {
    //             GameObject newItemGO = Instantiate(itemPrefab, slot.transform);
    //             NetworkIdentity networkIdentity ;
    //             if(newItemGO.GetComponent<NetworkIdentity>() == null){

    //                 networkIdentity = newItemGO.AddComponent<NetworkIdentity>();
    //             }else{
    //                 networkIdentity = newItemGO.GetComponent<NetworkIdentity>();
    //             }
    //             CmdSpawnItem(networkIdentity);
                

    //             InventoryItem item = newItemGO.GetComponent<InventoryItem>();
    //             item.nameOfItem = newStack.itemName;
                

    //             // Lookup item data on the client (avoid sending heavy data)
    //             ItemData itemData = FindItemData(newStack.itemName);
    //             if (itemData != null)
    //             {
    //                 Debug.Log("yey it found the itemdata" + itemData + newStack.itemName + "the icon: " + itemData.icon);
    //                 Image image = newItemGO.AddComponent<Image>();
    //                 image.sprite = itemData.icon;

    //                 item.Prefab = itemData.prefab;

    //                 TextMeshProUGUI quantityText = newItemGO.GetComponentInChildren<TextMeshProUGUI>();
    //                 quantityText.text = newStack.quantity.ToString();
    //             }
                
                
    //             if (networkIdentity != null)
    //             {
    //                 CmdSetAuth(networkIdentity);
    //             }

    //             newItemGO.transform.SetParent(slot.transform);
                
    //             return;
    //         }
    //     }
    // }
    // [Command]
    // private void CmdSpawnItem(NetworkIdentity networkIdentity)
    // {
        
    //     NetworkServer.Spawn(networkIdentity.gameObject);
    // }

    [TargetRpc]
private void RpcUpdateInventory(NetworkConnectionToClient target, ItemStack newStack, string uniqueKey, bool stackable)
{
    for (int i = 0; i < slots.Count; i++)
    {
        GameObject slot = slots[i];
        if (slot != null && slot.transform.childCount == 3)
        {
            InventorySlot invSlot = slots[i].GetComponent<InventorySlot>();
            // Send the index of the slot to the server
            CmdRequestItemSpawn(i, newStack, uniqueKey, invSlot, stackable);
            break;
        } 
    }
}
void CheckInventoryFull(){
    int count = 0;
    for (int i = 0; i < slots.Count; i++)
    {
        GameObject slot = slots[i];
        if (slot != null && slot.transform.childCount > 3)
        {
            count++;
            
        }
    }
    if(count == slots.Count){
        inventoryFull = true;
    } else{
        inventoryFull = false;
    }
}



public bool CheckInventorySlotAvailable(InventorySlot slot){
    if(slot.transform.childCount == 3 ){
        return true;
    }
    return false;
}

[Command]
private void CmdRequestItemSpawn(int slotIndex, ItemStack newStack, string uniqueKey, InventorySlot slot, bool stackable)
{
    Debug.Log(slotIndex);
    Debug.Log(slots.Count);
    
    if(CheckInventorySlotAvailable(slot)){
        GameObject newItemGO = Instantiate(itemPrefab, transform);
        
        NetworkIdentity networkIdentity = newItemGO.GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
        {
            networkIdentity = newItemGO.AddComponent<NetworkIdentity>();
        }
        NetworkServer.Spawn(newItemGO, connectionToClient);
        Debug.Log("connectionToClient auth" + connectionToClient);
            
        uint netId = networkIdentity.netId; // Get the network instance ID     

            // Send the item data to the client for updating visuals (TargetRpc)
        RpcUpdateItemOnClient(connectionToClient, slotIndex, newStack.itemName, netId, newStack.quantity, uniqueKey, stackable);

    } else {
        //try again
        RpcUpdateInventory(connectionToClient, newStack, uniqueKey, stackable);
    }
            
        
        
    
    
}


[TargetRpc]
private void RpcUpdateItemOnClient(NetworkConnectionToClient target, int indexSlot, string itemName, uint netId, int quantity, string uniqueKey, bool stackable)
{
    if (!NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity))
    {
        Debug.LogError($"Failed to find spawned object with Network ID {netId} on client!");
        return;
    }
    Debug.Log("networkidentity " + networkIdentity);
    GameObject newItemGO = networkIdentity.gameObject;
    InventoryItem item = newItemGO.GetComponent<InventoryItem>();
    item.nameOfItem = itemName;
    item.uniqueKey = uniqueKey;
    item.amount = quantity;
    item.isStackable = stackable;

    // Lookup item data
    ItemData itemData = FindItemData(itemName);
    Debug.Log("Found item data: " + itemData);
    InventorySlot slot = slots[indexSlot].GetComponent<InventorySlot>();
    if (itemData != null)
    {
        Debug.Log("Found item data: " + itemData.itemName);
        Debug.Log("Found item data: " + itemData.prefab);
        Debug.Log("Found item data: " + item);
        Image image = newItemGO.AddComponent<Image>();
        image.sprite = itemData.icon;
        if(item == null || itemData.prefab == null)
        item.Prefab = itemData.prefab;

        // TextMeshProUGUI quantityText = newItemGO.GetComponentInChildren<TextMeshProUGUI>();
        // quantityText.text = quantity.ToString();
        if(slot != null){

            slot.amount.text = quantity.ToString();
            slot.nameOfItem.text = itemName.ToString();
        }
    }

    // Set parent and position
    newItemGO.transform.SetParent(slot.transform);
    newItemGO.transform.localPosition = slot.Center.localPosition;
    newItemGO.transform.localScale = new Vector3(1f,1f,1f);
    newItemGO.transform.localRotation = Quaternion.identity;
    //isPickingUp = false;
}


    // Method to find item data on client side
    public ItemData FindItemData(string itemName)
    {
        return Resources.Load<ItemData>($"Items/{itemName}");
    }


    [TargetRpc]
    private void RpcUpdateStack(NetworkConnectionToClient target, ItemStack updatedStack, InventorySlot currSlot)
    {
       
        
        if(currSlot != null){
            Debug.Log("removed stack" + currSlot);
            currSlot.amount.text = "";
            currSlot.nameOfItem.text = "";
        }
        foreach (GameObject slot in slots)
        {
            InventoryItem item = slot.GetComponentInChildren<InventoryItem>();
                
            InventorySlot slotInv = slot.GetComponent<InventorySlot>();
            if (slot.transform.childCount > 3)
            {

                if (item != null && item.uniqueKey == updatedStack.uniqueKey ) // Match item by name
                {
                    // TextMeshProUGUI quantityText = itemGO.GetComponentInChildren<TextMeshProUGUI>(); // Update quantity text
                    // quantityText.text = updatedStack.quantity.ToString();
                    Debug.Log("updated stack" + updatedStack + updatedStack.quantity.ToString() + updatedStack.itemName + updatedStack.uniqueKey);
                    item.amount = updatedStack.quantity;
                    slotInv.amount.text = updatedStack.quantity.ToString();
                    slotInv.nameOfItem.text = updatedStack.itemName;
        
                    //isPickingUp = false;
                    return;
                }
            }
        }
    }

    


    [Command(requiresAuthority = false)]
    public void CmdSetAuth(NetworkIdentity networkIdentity)
    {
        if (networkIdentity == null)
        {
            Debug.LogError("CmdSetAuth was called with a null NetworkIdentity!");
            return;
        }
        networkIdentity.AssignClientAuthority(connectionToClient);
    }

    [Command(requiresAuthority = false)]
    public void CmdRemoveItem(string uniqueKey, int amount)
    {
        RpcBeforeRemoveItem(connectionToClient, uniqueKey, amount);
   }

    [TargetRpc]
    private void RpcBeforeRemoveItem(NetworkConnectionToClient target, string uniqueKey, int amount ){
        Debug.Log("command remove");
        if (inventory.TryGetValue(uniqueKey, out ItemStack existingStack))
        {
            Debug.Log("Found item name for removal: " + uniqueKey);
            Debug.Log("quantity: " +existingStack.quantity + amount);
            existingStack.quantity -= amount;

            if (existingStack.quantity <= 0)
            {
                Debug.Log("remove whole item");
                inventory.Remove(uniqueKey);
                CmdBeforeRemoveItem(uniqueKey);  // Notify the client to remove the item
            }
            else
            {
                Debug.Log("remove Quantity");
                inventory[uniqueKey] = existingStack;  // Update the inventory dictionary
                CmdBeforeUpdateStack(existingStack);  // Update the stack on the client
            }
        }
    }
    [Command]
    public void CmdBeforeRemoveItem(string uniqueKey)
    {
        RpcRemoveItem(connectionToClient,uniqueKey);
    }
    [Command]
    public void CmdBeforeUpdateStack(ItemStack existingStack)
    {
        RpcUpdateStack(connectionToClient, existingStack, null);
    }

    [TargetRpc]
    private void RpcRemoveItem(NetworkConnectionToClient target, string uniqueKey)
    {
        foreach (GameObject slot in slots)
        {
            if (slot.transform.childCount > 3) // Consider making this more robust
            {
                Debug.Log("Searching slot for item to remove...");

                GameObject itemGO = FindMatchingChild(slot, uniqueKey);
                if (itemGO != null)
                {
                    InventoryItem item = itemGO.GetComponent<InventoryItem>();
                    Debug.Log("Removing item from slot " + itemGO + " " + item);

                    NetworkIdentity networkIdentity = itemGO.GetComponent<NetworkIdentity>();
                    if (networkIdentity != null)
                    {
                        CmdDestroyItem(networkIdentity);
                    }
                    InventorySlot invSlot = slot.GetComponent<InventorySlot>();
                    if(invSlot != null){
                        invSlot.nameOfItem.text = "";
                        invSlot.amount.text = "";
                    }
                    return;
                }
            }
        }
    }
    GameObject FindMatchingChild(GameObject slot, string uniqueKey) {
        foreach (Transform child in slot.transform) {
            if (child.CompareTag("InventoryItem")) {
                InventoryItem item = child.GetComponent<InventoryItem>();
                if (item != null && item.uniqueKey == uniqueKey) {
                    return child.gameObject;
                }
            }
        }
        return null;
    }

    [Command]
    public void CmdDestroyItem(NetworkIdentity itemGO)
    {
        NetworkServer.Destroy(itemGO.gameObject);  // Ensure it's destroyed across the network
    }

    public ItemStack GetItem(string uniqueKey)
    {
        if (inventory.TryGetValue(uniqueKey, out ItemStack itemStack))
        {
            return itemStack;
        }
        return new ItemStack(null, 0, 0, uniqueKey); // Default: No bullets
    }
    public ItemStack FindItemByName(string name){
        foreach(var item in inventory){
            if (item.Value.itemName == name){
                return item.Value;
            }
        }
        return new ItemStack(null, 0, 0, null); // Default: No bullets
    }
    

}


// Class to store stackable item data
[System.Serializable]
public struct ItemStack
{
    public string uniqueKey;
    public string itemName;  // Use string instead of full ItemData
    public int quantity;
    public int quantityPerItem;

    public ItemStack(string itemName, int quantity, int quantityPerItem, string uniqueKey)
    {
        this.uniqueKey = uniqueKey;
        this.itemName = itemName;
        this.quantity = quantity;
        this.quantityPerItem = quantityPerItem;
    }
}
