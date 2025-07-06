using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SkillsUI : MonoBehaviour
{
    [SerializeField]
    private bool isInventoryOpen = false;
    public GameObject panel;
    public FirstPersonController player;
    public WeaponPickupController weapon;
    public PunchComboSystem punch;
    public AudioSource audioSource;
    public AudioClip open;
    public AudioClip close;
    public DisplaySkills displaySkills;
    void Start()
    {
        DisableInventoryUI();
        
    }

    void Update()
    {
        
        
       
        if (Input.GetKeyDown(KeyCode.Tab))
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

        displaySkills.UpdateUISkills();

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
        panel.SetActive(false);
        player.enabled = true;
        if (weapon.equippedGun != null && weapon != null)
        {
            weapon.equippedGun.enabled = true;
        }
        punch.enabled = true;
        audioSource.PlayOneShot(close);
        
    }

    public void EnableInventoryUI()
    {
        // Enable the CanvasGroup interaction and set it to opaque
        panel.SetActive(true);
        player.enabled = false;
        if (weapon.equippedGun != null && weapon != null)
        {
            weapon.equippedGun.enabled = false;
        }
        punch.enabled = false;
        audioSource.PlayOneShot(open);
        
    }
}