using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class LootableObject : NetworkBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float dropChance = 1f; // 1 = 100%, 0 = 0%
    }
    public LootItem[] lootItems;
    public Transform spawnPoint;      // Spawn position
    public float lootForce = 5f;      // Force applied to loot
    public float lootTime = 3f;       // Time required to loot
    public Canvas worldCanvas;        // World-space UI
    public Image loadingBarUI;        // Progress bar
    

    private bool isHolding = false;
    private float lootProgress = 0f;
    public BuildingInteriorLink interiorLink;
    public NetworkIdentity networkIdentity3;
    public bool isLink = false;
    public bool hasLooted = false;
    public GameObject ReturnSpawnPoint;
    public bool isTeleportBack = false;
    public InteriorCategory category;
    [SyncVar] private GameObject linkedRoom;

    public GameObject GetLinkedRoom() => linkedRoom;
    [Server]
    public void SetLinkedRoom(GameObject room)
    {
        linkedRoom = room;
    }
    

    void Start()
    {
        interiorLink = GetComponentInParent<BuildingInteriorLink>();
    }


    void Update()
    {
        if (isHolding && !hasLooted)
        {
            lootProgress += Time.deltaTime;
            loadingBarUI.fillAmount = lootProgress / lootTime;

            if (lootProgress >= lootTime)
            {
                CompleteLooting();
            }
        }
        else if (lootProgress > 0)
        {
            ResetLooting();
        }
    }

    public void StartLooting(NetworkIdentity networkIdent)
    {
        if (isHolding) return; // Prevent multiple loots at once
        networkIdentity3 = networkIdent;
        // targetNetId = networkIdent.netId;
        //Debug.Log(networkIdentity);
        isHolding = true;

        worldCanvas.gameObject.SetActive(true);
        loadingBarUI.fillAmount = 0f;
    }

    public void StopLooting()
    {
        isHolding = false;
        ResetLooting();
    }

    private void CompleteLooting()
    {
        isHolding = false;
        lootProgress = 0f;

        worldCanvas.gameObject.SetActive(false);
        loadingBarUI.fillAmount = 0;
        if (networkIdentity3 != null)
        {
            CmdSpawnLoot(networkIdentity3);
            if (!isLink && !isTeleportBack)
            { 
                hasLooted = true;
            }
            
        }
        
    }

    private void ResetLooting()
    {
        lootProgress = 0f;
        loadingBarUI.fillAmount = 0f;
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnLoot(NetworkIdentity networkIdentity2)
    {
        if (isLink)
        {

            if (networkIdentity2 != null)
            {
                if (InteriorSceneManager.Instance == null)
                {
                    Debug.LogError("InteriorSceneManager.Instance is null!?");
                    return;
                }
                if (linkedRoom == null)
                {
                    InteriorSceneManager.Instance.MovePlayerToInterior(networkIdentity2, this);
                }
                else
                {
                    InteriorSceneManager.Instance.TeleportPlayerToRoom(networkIdentity2, linkedRoom);
                }
                
                
                InteriorSceneManager.Instance.teleportBack = ReturnSpawnPoint;
            }
        }
        else if (isTeleportBack)
        {
            if(networkIdentity2 != null){
                if (InteriorSceneManager.Instance == null || InteriorSceneManager.Instance.teleportBack == null)
                {
                    Debug.LogError("InteriorSceneManager.Instance is null!?");
                    return;
                }
                Debug.Log("tried teleport back!");
                InteriorSceneManager.Instance.MovePlayerBack(networkIdentity2);
            } 
        }
        else
        {
            Debug.Log("looting");
            int lootCount = Random.Range(2, 5);
            int attempts = 0;

            while (lootCount > 0 && attempts < 100) // safety check
            {
                attempts++;

                LootItem candidate = lootItems[Random.Range(0, lootItems.Length)];
                if (Random.value <= candidate.dropChance)
                {
                    GameObject lootItem = Instantiate(candidate.prefab, spawnPoint.position, Quaternion.identity);
                    if (lootItem != null)
                    {
                        NetworkServer.Spawn(lootItem);

                        Rigidbody rb = lootItem.GetComponent<Rigidbody>();
                        if (rb != null)
                        {


                            rb.AddForce(Vector3.forward * lootForce, ForceMode.Impulse);
                        }

                        lootCount--;
                        
                    }
                }
            }
        } 
    }

}
