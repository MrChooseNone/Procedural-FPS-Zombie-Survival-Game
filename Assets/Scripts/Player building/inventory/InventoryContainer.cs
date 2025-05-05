using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using Unity.VisualScripting;
using Mirror.BouncyCastle.Pkcs;

public class InventoryContainer : NetworkBehaviour
{
    

    [SerializeField] public List<Transform> slotParents; // UI parents for the slots
    [SerializeField] private GameObject itemPrefab;

    [SyncVar] public bool inventoryOpen = false;
    
    [SyncVar(hook = nameof(OnInventoryChanged))]
    public SyncList<ItemStack> syncedItems = new SyncList<ItemStack>();
    public PlayerWallInteraction playerWallInteraction;
    public GameObject externalInventoryPanel;

    private void Awake()
    {
        
    }

    // Call this to open inventory (only one player allowed at a time)
    [Command(requiresAuthority = false)]
    public void CmdRequestOpenInventory(NetworkConnectionToClient sender = null)
    {
        if (!inventoryOpen)
        {
            inventoryOpen = true;
            TargetOpenInventory(sender);
        }
    }

    [TargetRpc]
    private void TargetOpenInventory(NetworkConnection target)
    {
        Debug.Log("Inventory opened by player");
        SpawnItemsInUI();
        if(playerWallInteraction != null){
            playerWallInteraction.ToggleInventory();
        }
    }

    public void CloseInventory()
    {
        inventoryOpen = false;
        if(playerWallInteraction != null){
            playerWallInteraction.ToggleInventory();
        }
        ClearSlots();
    }

    public void UpdateInventory(ItemStack newItem, ItemStack removedItem)
    {
        if (!isServer) return;

        
        if(newItem.itemName != null){
            syncedItems.Add(newItem);
        }
        if(removedItem.itemName != null)
        {
            syncedItems.Remove(removedItem);
        }
        SpawnItemsInUI();
    }

    private void OnInventoryChanged(SyncList<GameObject>.Operation op, int index, GameObject oldItem, GameObject newItem)
    {
        // Refresh the inventory UI when items are updated
        SpawnItemsInUI();
    }

    private void SpawnItemsInUI()
    {
        ClearSlots();
        int i = 0;
        foreach(var stack in syncedItems)
        {
            // GameObject spawned = Instantiate(item, slotParents[i]);
            // InventoryItem inventoryItem = spawned.GetComponent<InventoryItem>();
            // InventorySlot invSlot = slotParents[i].GetComponent<InventorySlot>();
            // if(invSlot != null && inventoryItem != null){
            //     invSlot.nameOfItem.text = inventoryItem.nameOfItem;
            //     invSlot.amount.text = inventoryItem.amount.ToString();
            // }
            // // Optional: update visuals
            // spawned.name = syncedItems[i].name;
            CmdUpdateInventory(stack, stack.uniqueKey, i);
            i++;
        }
    }

    private void ClearSlots()
    {
        foreach (Transform slot in slotParents)
        {
            if (slot.childCount > 3) // Consider making this more robust
            {
                Debug.Log("Searching slot for item to remove...");

                foreach (Transform child in slot) {
                    if (child.CompareTag("InventoryItem")) {
                        Destroy(child.gameObject);
                        InventorySlot invSlot = slot.GetComponent<InventorySlot>();
                        if(invSlot != null){
                            invSlot.nameOfItem.text = "";
                            invSlot.amount.text = "";
                        }
                    }
                }
            }
        }
    }

    // public ItemStack CheckIfItemExists(string itemName)
    // {
    //     // Iterate through the inventory dictionary
    //     foreach (var entry in inventory)
    //     {
    //         // Compare the itemName of the current ItemStack with the passed itemName
    //         if (entry.Value.itemName == itemName)
    //         {
    //             // Item found with matching itemName
    //             return entry.Value;
    //         }
    //     }
    //     // No matching itemName found
    //     return new ItemStack(null, 0, 0, null);  // Or return null if you prefer
    // }



    [Command(requiresAuthority = false)]
    public void CmdUpdateStack(ItemStack existingStack, InventorySlot currSlot)
    {
        RpcUpdateStack(playerWallInteraction.connectionToClient,existingStack, currSlot);
    }
    [Command(requiresAuthority = false)]
    public void CmdUpdateInventory(ItemStack newStack, string uniqueKey, int i)
    {
        RpcUpdateInventory(playerWallInteraction.connectionToClient, newStack, uniqueKey, i);
    }


    [TargetRpc]
private void RpcUpdateInventory(NetworkConnectionToClient target, ItemStack newStack, string uniqueKey, int index)
{
    for (int i = index; i < slotParents.Count; i++)
    {
        Transform slot = slotParents[i];
        if (slot != null && slot.childCount == 3)
        {
            InventorySlot invSlot = slotParents[i].GetComponent<InventorySlot>();
            // Send the index of the slot to the server
            CmdRequestItemSpawn(i, newStack, uniqueKey, invSlot);
            break;
        } 
    }
}
void CheckInventoryFull(){
    int count = 0;
    for (int i = 0; i < slotParents.Count; i++)
    {
        Transform slot = slotParents[i];
        if (slot != null && slot.childCount > 3)
        {
            count++;
            
        }
    }
    // if(count == slotParents.Count){
    //     inventoryFull = true;
    // } else{
    //     inventoryFull = false;
    // }
}



public bool CheckInventorySlotAvailable(InventorySlot slot){
    if(slot.transform.childCount == 3 ){
        return true;
    }
    return false;
}

[Command(requiresAuthority = false)]
private void CmdRequestItemSpawn(int slotIndex, ItemStack newStack, string uniqueKey, InventorySlot slot)
{
    Debug.Log(slotIndex);
    Debug.Log(slotParents.Count);
    
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
        RpcUpdateItemOnClient(playerWallInteraction.connectionToClient, slotIndex, newStack.itemName, netId, newStack.quantity, uniqueKey);

    } else {
        //try again
        RpcUpdateInventory(playerWallInteraction.connectionToClient, newStack, uniqueKey, 0);
    }
            
        
        
    
    
}


[TargetRpc]
private void RpcUpdateItemOnClient(NetworkConnectionToClient target, int indexSlot, string itemName, uint netId, int quantity, string uniqueKey)
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

    // Lookup item data
    ItemData itemData = FindItemData(itemName);
    Debug.Log("Found item data: " + itemData);
    InventorySlot slot = slotParents[indexSlot].GetComponent<InventorySlot>();
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
        foreach (Transform slot in slotParents)
        {
            InventoryItem item = slot.GetComponentInChildren<InventoryItem>();
                
            InventorySlot slotInv = slot.GetComponent<InventorySlot>();
            if (slot.childCount > 3)
            {

                if (item != null && item.uniqueKey == updatedStack.uniqueKey ) // 
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
        RpcBeforeRemoveItem(playerWallInteraction.connectionToClient, uniqueKey, amount);
   }

    [TargetRpc]
    private void RpcBeforeRemoveItem(NetworkConnectionToClient target, string uniqueKey, int amount)
    {
        Debug.Log("command remove");
        int existingStackIndex = GetItemStackIndexByKey(uniqueKey);

        if (existingStackIndex >= 0 && syncedItems[existingStackIndex].itemName != null)
        {
            Debug.Log("Found item name for removal: " + uniqueKey);
            Debug.Log("quantity: " + syncedItems[existingStackIndex].quantity + " - " + amount);
            ItemStack updatedStack = syncedItems[existingStackIndex];
            updatedStack.quantity -= amount; 

            if (updatedStack.quantity <= 0)
            {
                Debug.Log("remove whole item");
                syncedItems.RemoveAt(existingStackIndex);
                CmdBeforeRemoveItem(uniqueKey);  // Notify the server to remove the item
            }
            else
            {
                Debug.Log("remove Quantity");
                syncedItems[existingStackIndex] = updatedStack;
                CmdBeforeUpdateStack(syncedItems[existingStackIndex]);  // Update the stack on the client
            }
        }
    }

    public int GetItemStackIndexByKey(string key)
    {
        for (int i = 0; i < syncedItems.Count; i++)
        {
            if (syncedItems[i].uniqueKey == key)
                return i;
        }

        return -1; // Not found
    }


    [Command(requiresAuthority = false)]
    public void CmdBeforeRemoveItem(string uniqueKey)
    {
        RpcRemoveItem(playerWallInteraction.connectionToClient,uniqueKey);
    }
    [Command(requiresAuthority = false)]
    public void CmdBeforeUpdateStack(ItemStack existingStack)
    {
        RpcUpdateStack(playerWallInteraction.connectionToClient, existingStack, null);
    }

    [TargetRpc]
    private void RpcRemoveItem(NetworkConnectionToClient target, string uniqueKey)
    {
        foreach (Transform slot in slotParents)
        {
            if (slot.childCount > 3) // Consider making this more robust
            {
                Debug.Log("Searching slot for item to remove...");

                GameObject itemGO = FindMatchingChild(slot.gameObject, uniqueKey);
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

    [Command(requiresAuthority = false)]
    public void CmdDestroyItem(NetworkIdentity itemGO)
    {
        NetworkServer.Destroy(itemGO.gameObject);  // Ensure it's destroyed across the network
    }

    public ItemStack GetItem(string uniqueKey)
    {
        for (int i = 0; i < syncedItems.Count; i++)
        {
            if (syncedItems[i].uniqueKey == uniqueKey) return syncedItems[i];
        }

        return new ItemStack(null, 0, 0, null);
    }
    
    // public ItemStack FindItemByName(string name){
    //     foreach(var item in inventory){
    //         if (item.Value.itemName == name){
    //             return item.Value;
    //         }
    //     }
    //     return new ItemStack(null, 0, 0, null); // Default: No bullets
    // }

    
}
