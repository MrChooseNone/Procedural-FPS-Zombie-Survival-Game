using UnityEngine;
using Mirror;

public class PlayerInteractionScript : NetworkBehaviour
{
    public float interactionRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    private LootableObject currentLootable;

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(interactKey))
        {
            TryStartLoot();
        }

        if (Input.GetKeyUp(interactKey))
        {
            StopLoot();
        }
    }

    private void TryStartLoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactionRange))
        {
            LootableObject lootable = hit.collider.GetComponent<LootableObject>();
            NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
            if (lootable != null && networkIdentity != null)
            {
                currentLootable = lootable;
                lootable.StartLooting(networkIdentity);
            }
        }
    }

    private void StopLoot()
    {
        if (currentLootable != null)
        {
            currentLootable.StopLooting();
            currentLootable = null;
        }
    }
}