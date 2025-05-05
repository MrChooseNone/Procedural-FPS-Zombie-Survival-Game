using System;
using System.Linq;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class PlayerBuilding : NetworkBehaviour
{
    public bool isPlacing = false;
    public Camera playerCamera;
    public LayerMask placmentLayer;
    public GameObject wallWoodPrefab;
    public GameObject wallMetalPrefab;
    public GameObject createPrefab;
    public GameObject previewWall;
    private Vector3 start;
    private Vector3 end;
    public float wallHeight;
    public InventoryManager1 inventory;
    private GameObject[] wallInstances; // Array to store the wall instances
    private bool firstWallFlag = false;
    public Camera playCamera;
    public float destroyDistance;
    public LayerMask pickupLayer;
    public KeyCode wallWood;
    public float woodWallSpacing;
    public KeyCode wallMetal;
    public float metalWallSpacing;
    public KeyCode create;
    public float createSpacing;
    private bool isPopedUp;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;
        
        wallInstances = new GameObject[0]; // Initialize empty array
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) return;

        KeyCode pressedKey = KeyCode.None;
        string itemName = "";

        // Determine which key was pressed
        if (Input.GetKeyDown(wallWood))
        {
            pressedKey = wallWood;
            itemName = "Wood";
        }
        else if (Input.GetKeyDown(wallMetal))
        {
            pressedKey = wallMetal;
            itemName = "Stone";
        }
        else if (Input.GetKeyDown(create))
        {
            pressedKey = create;
            itemName = "Wood";
        }

        // Start placing
        if (pressedKey != KeyCode.None && !isPlacing && inventory.FindItemByName(itemName).quantity > 0)
        {
            StartPlacing(pressedKey, itemName);
        }

        // Handle updating and finishing placement
        if (isPlacing)
        {
            if (Input.GetKey(wallWood))
            {
                UpdatePlacing(wallWood, "Wood", woodWallSpacing);
            }
            else if (Input.GetKey(wallMetal))
            {
                UpdatePlacing(wallMetal, "Stone", metalWallSpacing);
            }
            else if (Input.GetKey(create))
            {
                UpdatePlacing(create, "Wood", createSpacing);
            }

            if (Input.GetKeyUp(wallWood))
            {
                FinishPlacing(wallWood, "Wood");
            }
            else if (Input.GetKeyUp(wallMetal))
            {
                FinishPlacing(wallMetal, "Stone");
            }
            else if (Input.GetKeyUp(create))
            {
                FinishPlacing(create, "Wood");
            }
        }

        // Right-click to cancel
        if (Input.GetMouseButton(1) && isPlacing)
        {
            CancelPlacement();
        }
        // Perform raycast to detect guns in the player's line of sight
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

    private void FinishPlacing(KeyCode key, string itemName)
    {
        if(!isLocalPlayer) return;
        if (wallInstances.Length > 0)
        {
            int count = 0;
            foreach (var wall in wallInstances)
            {

                // Make the wall solid by changing its material color
                if (wall != null)
                {
                    CmdSpawnWall(wall.transform.position, wall.transform.rotation, key);
                    Destroy(wall); // Remove local preview after server spawns real one
                    count++;
                    // wall.GetComponent<Renderer>().material.color = Color.white;
                }
            }
            ItemStack itemStack = inventory.FindItemByName(itemName);
            inventory.CmdRemoveItem(itemStack.uniqueKey, count);


            // Clear the array reference after placement is finished
            wallInstances = new GameObject[0];
            isPlacing = false;
        }
    }

    private void UpdatePlacing(KeyCode key, string itemName,float spacing)
    {
        if(!isLocalPlayer) return;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 10f, placmentLayer))
        {
            end = hit.point;

            // Calculate the distance between the start and end points
            float distance = Vector3.Distance(start, end);

            // Calculate the number of wall pieces needed based on spacing
            
            int numPrefabs = Mathf.FloorToInt(distance / spacing);
            

            if(numPrefabs <= inventory.FindItemByName(itemName).quantity){

                // Ensure the wall instances array has enough space
                if (wallInstances.Length != numPrefabs)
                {
                    // Clear previous wall instances
                    foreach (var wall in wallInstances)
                    {
                        if (wall != null)
                        {
                            Destroy(wall);
                        }
                    }

                    // Resize the array to hold the required number of walls
                    wallInstances = new GameObject[numPrefabs];
                }
                if (!firstWallFlag){
                    //adjust the first wall
                    Quaternion rot = Quaternion.LookRotation(end - start);
                    Quaternion AdjustedRot = rot * Quaternion.Euler(0, 90, 0); // Rotate by 90 degrees around Y-axis
                    previewWall.transform.rotation = AdjustedRot;
                }

                // Instantiate the wall pieces along the line
                for (int j = 0; j < numPrefabs; j++)
                {
                    Vector3 position = Vector3.Lerp(start, end, (float)j / numPrefabs);

                    // Calculate rotation to make the wall face from start to end
                    Quaternion rotation = Quaternion.LookRotation(end - start);
                    Quaternion AdjustedRotation = rotation * Quaternion.Euler(0, 90, 0); // Rotate by 90 degrees around Y-axis


                    if (wallInstances[j] == null)
                    {
                        // Instantiate new wall piece if not already present
                        if(key == wallWood){

                            wallInstances[j] = Instantiate(wallWoodPrefab, position, AdjustedRotation);
                            // previewWall.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f); // Transparent for preview
                        } else if(key == wallMetal) {
                            wallInstances[j] = Instantiate(wallMetalPrefab, position, AdjustedRotation);
                        } else if(key== create){
                            wallInstances[j] = Instantiate(createPrefab, position, AdjustedRotation);
                        }
                        
                        
                        // wallInstances[j].GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f); // Transparent preview
                    }
                    else
                    {
                        // Update the position and rotation of the existing wall piece
                        wallInstances[j].transform.position = position;
                        wallInstances[j].transform.rotation = AdjustedRotation;
                    }
                }

            }
        }
    }
    [Command]
    void CmdSpawnWall(Vector3 position, Quaternion rotation, KeyCode key)
    {
        GameObject wall = null;
        if(key == wallWood){

            wall = Instantiate(wallWoodPrefab, position, rotation);
                // previewWall.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f); // Transparent for preview
        } else if(key == wallMetal) {
            wall = Instantiate(wallMetalPrefab, position, rotation);
        } else if(key== create){
            wall = Instantiate(createPrefab, position, rotation);
        }
        if(wall != null) NetworkServer.Spawn(wall); // Sync to all clients
    }


    private void StartPlacing(KeyCode key, string itemName)
    {
        if(!isLocalPlayer) return;

            if(Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 10f, placmentLayer)){
                isPlacing = true;
                start = hit.point;
                if(key == wallWood){

                    previewWall = Instantiate(wallWoodPrefab, start, quaternion.identity);
                    // previewWall.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f); // Transparent for preview
                } else if(key == wallMetal) {
                    previewWall = Instantiate(wallMetalPrefab, start, quaternion.identity);
                } else if(key== create){
                    previewWall = Instantiate(createPrefab, start, quaternion.identity);
                }
                
            }
        
    }

    void CancelPlacement()
    {
        if(!isLocalPlayer) return;
        // Destroy all wall instances if placement is canceled
        foreach (var wall in wallInstances)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }

        wallInstances = new GameObject[0];
        isPlacing = false;
    }
}
