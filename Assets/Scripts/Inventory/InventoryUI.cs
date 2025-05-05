using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel; // Reference to your inventory panel
    private bool isInventoryOpen = false;
    public CanvasGroup inventoryCanvasGroup;
    public FirstPersonController player;
    public WeaponPickupController weapon;
    public PunchComboSystem punch;
    public TMP_InputField[] noteInputFields;
    void Start()
    {
        DisableInventoryUI();
        
    }

    void Update()
    {
        bool isAnyInputFocused = false;
        if(isInventoryOpen){


            foreach (var inputField in noteInputFields)
            {
                if (inputField.isFocused)
                {
                    isAnyInputFocused = true;
                    break; // Exit loop early if any field is focused
                }
            }

            // If any input field is focused, block inventory actions like closing
            // if (isAnyInputFocused)
            // {
            //     return;
            // }
        }
        // Check for input (I key to open/close inventory)
        if (Input.GetKeyDown(KeyCode.I) && !isAnyInputFocused)
        {
            ToggleInventory();
        } else if(Input.GetKeyDown(KeyCode.Escape) && isInventoryOpen){
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            DisableInventoryUI();
            isInventoryOpen = false;
        }
    }

    void ToggleInventory()
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