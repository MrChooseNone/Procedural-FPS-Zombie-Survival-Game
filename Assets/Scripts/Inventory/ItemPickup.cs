using UnityEngine;
using Mirror;

public class ItemPickup : NetworkBehaviour
{
    public ItemData itemData; // Assign in the Inspector

    // Prevent multiple pick-ups locally
    private bool _pickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only the local player should run this
        if (_pickedUp) return;
        if (!other.TryGetComponent(out InventoryManager1 inventory)) return;
        if (!other.TryGetComponent(out NetworkIdentity ni) || !ni.isLocalPlayer) return;
        if (!other.TryGetComponent(out HealthSystem health)  || health.isDead) return;
        if (inventory.inventoryFull) return;

        // Mark as picked up so we don’t try again
        _pickedUp = true;

        // Tell the server to add the item
        inventory.CmdAddItem(itemData.itemName, itemData.quantity, itemData.isStackable);
        inventory.CmdSetAuth(ni);
        // Destroy the pickup on the server
        CmdDestroyPickup();
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroyPickup()
    {
        // only the server destroys
        if (!isServer) return;
        NetworkServer.Destroy(gameObject);
    }
}

