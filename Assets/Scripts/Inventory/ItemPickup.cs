using UnityEngine;
using Mirror;

public class ItemPickup : NetworkBehaviour
{
    public ItemData itemData; // Assign an item from the Inspector

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out InventoryManager1 inventory) &&
            other.TryGetComponent(out NetworkIdentity networkIdentity) &&
            networkIdentity.isLocalPlayer && 
            other.TryGetComponent(out HealthSystem healthSystem))
        {
            if(healthSystem != null && !healthSystem.isDead && !inventory.inventoryFull && !inventory.isPickingUp){

                inventory.CmdAddItem(itemData.itemName, itemData.quantity, itemData.isStackable);
                inventory.CmdSetAuth(networkIdentity);
                CmdDestroyPickup();
            }
        }
    }

    [Command(requiresAuthority = false)] // Allow any client to request destruction
    private void CmdDestroyPickup()
    {
        if (!isServer) return; // Ensure only the server destroys the object

        NetworkServer.Destroy(gameObject);
    }

}
