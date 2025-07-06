using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBuilding : NetworkBehaviour
{
    public bool isPlacing = false;
    public Camera playerCamera;
    public LayerMask placmentLayer;
    public GameObject previewPrefab;
    private Vector3 start;

    public InventoryManager1 inventory;

    public Camera playCamera;
    public float destroyDistance;
    public LayerMask pickupLayer;

    public GameObject buildMenuPanel;
    private bool isPopedUp;

    public GameObject buttonPrefab;
    public Transform gridParent;
    public Button nextPageButton, prevPageButton;

    public List<BuildItem> allItems;
    public int playerLevel = 1;

    public int itemsPerPage = 6;
    private int currentPage = 0;
    public BuildItem currentItem;
    private bool isBuildMenu = false;

    [System.Serializable]
    public class BuildItem
    {
        public string name;
        public GameObject prefab;
        public GameObject previewPrefab;
        public Sprite icon;
        public int unlockLevel; // unlock requirement
        public string itemCost;
        public int ItemAmountCost;
    }

    private List<BuildItem> unlockedItems = new List<BuildItem>();
    public float snapRange;
    public LayerMask wallLayer;
    public float snapThreshold;
    public PlayerSkills playerSkills;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (inventory == null || buildMenuPanel == null || gridParent == null)
        {
            Debug.LogError("Missing references on PlayerBuilding!");
            return;
        }
        if (!isLocalPlayer) return;
        nextPageButton.onClick.AddListener(() => ChangePage(1));
        prevPageButton.onClick.AddListener(() => ChangePage(-1));
        buildMenuPanel.SetActive(false);
        UpdateUnlockedItems();
        RefreshPage();

    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.B))
        {
            buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
            isBuildMenu = buildMenuPanel.activeSelf;
            if (isBuildMenu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }


        
        

        // Handle updating and finishing placement
        if (isPlacing && currentItem != null)
        {
            UpdatePlacing(currentItem.itemCost, currentItem.ItemAmountCost, currentItem.prefab);
        }

        // Right-click to cancel
        if ((Input.GetMouseButton(1) || Input.GetKeyDown(KeyCode.Escape)) && isPlacing)
        {
            CancelPlacement();
        }
        RaycastHit hit;
        Vector3 rayOrigin = playCamera.transform.position; // Start of the ray (player's position)
        Vector3 rayDirection = playCamera.transform.forward; // Ray direction (the direction the player is facing)
        float rayDistance = destroyDistance; // Max distance for the raycast
        //destroy wall
        BuildingInteract newWall = null;
        InteractivePopup popup = null;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, pickupLayer))
        {
            // Check if the raycast hit an object with a WeaponController
            newWall = hit.collider.GetComponentInParent<BuildingInteract>();
            popup = hit.collider.GetComponentInParent<InteractivePopup>();
        }
        if(popup != null && !isPopedUp){
            popup.ShowPopup(transform);
            isPopedUp = true;
        }else if(isPopedUp){
            popup.HidePopup();
            isPopedUp = false;
        }
        if(Input.GetKeyDown(KeyCode.X) && !isPlacing){
            Debug.Log("Tried to destroy wall");
            if(newWall != null){
                Debug.Log("got wall");
                newWall.DestroyWall();
            }
        }
    }
    
    void RefreshPage()
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        int start = currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, unlockedItems.Count);

        for (int i = start; i < end; i++)
        {
            var item = unlockedItems[i];
            GameObject buttonObj = Instantiate(buttonPrefab, gridParent);
            var icon = buttonObj.GetComponentInChildren<Image>();
            icon.sprite = item.icon;

            var btn = buttonObj.GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => {
                SelectBuildable(item);
                buildMenuPanel.SetActive(false);
            });
        }

        prevPageButton.gameObject.SetActive(currentPage > 0);
        nextPageButton.gameObject.SetActive(end < unlockedItems.Count);
    }

    void ChangePage(int amount)
    {
        currentPage += amount;
        RefreshPage();
    }
    void UpdateUnlockedItems()
    {
        unlockedItems = allItems.FindAll(item => playerLevel >= item.unlockLevel);
    }

    public void SelectBuildable(BuildItem item)
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        currentItem = item;
        if (!isPlacing && currentItem != null)
        {
            if (playerSkills.HasUnlocked(SkillType.Engineering, item.unlockLevel))
            {
                StartPlacing(currentItem.previewPrefab);
            }
        }
    }

    private void FinishPlacing(string itemCost, int ItemAmountCost, GameObject prefab)
    {
        if (!isLocalPlayer) return;

        CmdSpawnWall(prefab.transform.position, prefab.transform.rotation, currentItem.name);
        Destroy(previewPrefab);

        ItemStack itemStack = inventory.FindItemByName(currentItem.itemCost);
        if (itemStack.itemName != null)
        {
            inventory.CmdRemoveItem(itemStack.uniqueKey, ItemAmountCost);
            if (playerSkills != null)
            {
                playerSkills.GainXP(SkillType.Engineering, 20f);
            }
        }
        isPlacing = false;
        
    }

    private void UpdatePlacing(string itemCost, int ItemAmountCost, GameObject prefab)
    {
        if (!isLocalPlayer) return;
        Vector3 start = new Vector3(0, 0, 0);
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 10f, placmentLayer))
        {
            start = hit.point;
            if (previewPrefab != null)
            {
                previewPrefab.transform.position = start;
                float newRot = playerCamera.transform.eulerAngles.y;
                previewPrefab.transform.rotation = Quaternion.Euler(0f, newRot, 0f);
            }
        }
            else
            {
                start = playerCamera.transform.position + playerCamera.transform.forward * 10f;
                if (previewPrefab != null)
                {
                    previewPrefab.transform.position = start;
                    previewPrefab.transform.rotation = playerCamera.transform.rotation;

                }
            }
        Collider[] nearby = Physics.OverlapSphere(previewPrefab.transform.position, snapRange, wallLayer);

        foreach (Collider col in nearby)
        {
            Snappable snappable = col.GetComponent<Snappable>();
            if (snappable == null) continue;

            foreach (Transform theirPoint in snappable.snapPoints)
            {
                foreach (Transform myPoint in previewPrefab.GetComponent<Snappable>().snapPoints)
                {
                    float dist = Vector3.Distance(myPoint.position, theirPoint.position);
                    if (dist < snapThreshold)
                    {
                        // Snap logic
                        Vector3 offset = myPoint.position - previewPrefab.transform.position;
                        previewPrefab.transform.position = theirPoint.position - offset;

                        // Optional: Match rotation (90° increments)
                        previewPrefab.transform.rotation = Quaternion.LookRotation(-theirPoint.forward);
                        break;
                    }
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            previewPrefab.transform.Rotate(Vector3.up, 15f);
        }
        if (Input.GetMouseButton(0))
        {
            FinishPlacing(itemCost, ItemAmountCost, previewPrefab);
        }
        
    }
    [Command]
    void CmdSpawnWall(Vector3 position, Quaternion rotation, string itemName)
    {
        GameObject wall = null;
        BuildItem item = allItems.Find(i => i.name == itemName);
        
        wall = Instantiate(item.prefab, position, rotation);
        
        if(wall != null) NetworkServer.Spawn(wall); // Sync to all clients
    }


    private void StartPlacing(GameObject prefab)
    {
        if (!isLocalPlayer) return;
        
        isPlacing = true;

        start = playerCamera.transform.position + playerCamera.transform.forward * 10f;
        
        previewPrefab = Instantiate(prefab, start, quaternion.identity);
        
    }

    void CancelPlacement()
    {
        if(!isLocalPlayer) return;
        // Destroy all wall instances if placement is canceled
      
        Destroy(previewPrefab);
         

        isPlacing = false;
    }
}
