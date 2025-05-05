using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class PlayerWallInteraction : NetworkBehaviour
{
    public float interactionRange = 5f;
    public Camera playerCamera;
    public GameObject externalPanel;

    
    private bool isInventoryOpen = false;
    public CanvasGroup inventoryCanvasGroup;
    public FirstPersonController player;
    public WeaponPickupController weapon;
    public PunchComboSystem punch;
    public TMP_InputField[] noteInputFields;
    public List<Transform> slots;
    public InventoryContainer externalInventory;
    private InteractivePopup currentPopup = null;

    void Start()
    {
        if (!isLocalPlayer ) return;
        DisableInventoryUI();
        
        foreach(Transform child in externalPanel.transform){
            if(child.CompareTag("Slot")){
                Debug.Log("Prepared slots");
                slots.Add(child);
            }
        }
    }

    void Update()
    {
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionRange))
        {
            externalInventory = hit.collider.GetComponentInParent<InventoryContainer>();

            if (externalInventory != null)
            {
                InteractivePopup popup = hit.collider.GetComponentInParent<InteractivePopup>();
                if (popup != currentPopup)
                {
                    currentPopup?.HidePopup();
                    popup?.ShowPopup(transform);
                    currentPopup = popup;
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    // If we already have an open inventory, close it
                    if (externalInventory != null && externalInventory.inventoryOpen)
                    {
                        externalInventory.CloseInventory();
                        DisableInventoryUI(); // Hide the panel and reset visuals
                        externalInventory = null;
                        isInventoryOpen = false;
                    }
                    else
                    {
                        // Open the inventory
                        externalInventory.CmdRequestOpenInventory();

                        externalInventory.externalInventoryPanel = externalPanel;
                        externalInventory.slotParents = slots;
                        externalInventory.playerWallInteraction = this;

                        
                        isInventoryOpen = true;
                    }

                    currentPopup?.HidePopup();
                    currentPopup = null;
                }
            }
            else
            {
                currentPopup?.HidePopup();
                currentPopup = null;
            }
        }
        else
        {
            currentPopup?.HidePopup();
            currentPopup = null;
        }

        // Close inventory on ESC
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) && isInventoryOpen)
        {
            externalInventory.CloseInventory();
            DisableInventoryUI();
            externalInventory = null;
            isInventoryOpen = false;
        }
    }


    [Command]
    void CmdOpenInventory(InventoryContainer externalInventory){
        externalInventory.CmdRequestOpenInventory();
    }
    [Command]
    void CmdCloseInventory(InventoryContainer externalInventory){
        externalInventory.CloseInventory();
    }
    
    

    public void ToggleInventory()
    {
        // Toggle the inventory open/close
        isInventoryOpen = !isInventoryOpen;

        

        // Lock or unlock the cursor based on whether the inventory is open
        if (isInventoryOpen)
        {
            // Make the cursor visible and unlock it for UI interactions
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnableInventoryUI();
        }
        else
        {
            // Make the cursor invisible and lock it if needed
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            DisableInventoryUI();
            
        }
    }

    public void DisableInventoryUI()
    {
        // Disable the CanvasGroup interaction and set it to transparent
        inventoryCanvasGroup.interactable = false;
        inventoryCanvasGroup.blocksRaycasts = false;
        inventoryCanvasGroup.alpha = 0; // Set alpha to 0 for full transparency
        player.enabled = true;
        if(weapon.equippedGun != null && weapon != null){
            weapon.equippedGun.enabled = true;
        }
        punch.enabled = true;
        
    }

    public void EnableInventoryUI()
    {
        // Enable the CanvasGroup interaction and set it to opaque
        inventoryCanvasGroup.interactable = true;
        inventoryCanvasGroup.blocksRaycasts = true;
        inventoryCanvasGroup.alpha = 1; // Set alpha to 1 for full opacity
        player.enabled = false;
        if(weapon.equippedGun != null && weapon != null){
            weapon.equippedGun.enabled = false;
        }
        punch.enabled = false;
        
    }

    

}
