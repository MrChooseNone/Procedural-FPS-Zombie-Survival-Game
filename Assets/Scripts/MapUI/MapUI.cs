using System.Runtime.CompilerServices;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class MapUI : NetworkBehaviour
{
    public GameObject inventoryPanel; // Reference to your inventory panel
    private bool isMapOpen = false;
    private GameObject mapUI;
    
    public WeaponPickupController weapon;
    public PunchComboSystem punch;
    
    void Start()
    {
        if(!isLocalPlayer) return;
        mapUI = GameObject.FindGameObjectWithTag("MapUI");
        DisableMapUI();
        
    }

    void Update()
    {
        if(!isLocalPlayer) return;
        
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        } else if(Input.GetKeyDown(KeyCode.Escape) && isMapOpen){
            
            DisableMapUI();
            isMapOpen = false;
        }
    }

    void ToggleMap()
    {
        // Toggle the inventory open/close
        isMapOpen = !isMapOpen;

        

        // Lock or unlock the cursor based on whether the inventory is open
        if (isMapOpen)
        {
            // Make the cursor visible and unlock it for UI interactions
            
            EnableMapUI();
        }
        else
        {
            // Make the cursor invisible and lock it if needed
            
            DisableMapUI();
            
        }
    }

    public void DisableMapUI()
    {
        // Disable the CanvasGroup interaction and set it to transparent
        if(mapUI == null) return;
        mapUI.SetActive(false);
        if(weapon.equippedGun != null && weapon != null){
            weapon.equippedGun.enabled = true;
        }
        punch.enabled = true;
        
    }

    public void EnableMapUI()
    {
        // Enable the CanvasGroup interaction and set it to opaque
        if(mapUI == null) return;
        mapUI.SetActive(true);
        if(weapon.equippedGun != null && weapon != null){
            weapon.equippedGun.enabled = false;
        }
        punch.enabled = false;
        
    }
}

