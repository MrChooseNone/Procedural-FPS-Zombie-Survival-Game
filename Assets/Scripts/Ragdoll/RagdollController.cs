using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.AI;
using Unity.VisualScripting;

public class RagdollController : NetworkBehaviour
{
    public Rigidbody[] ragdollBodies;
    public Animator animator;
    public bool isNpc;
    public ZombieAI zombieController;
    public FirstPersonController playerController;
    public float recoverDelay = 3f;
    public NavMeshAgent navAgent;
    private Vector3 ragdollEndPosition;
    public GameObject ragdollPrefab; // Ragdoll prefab to spawn
    public GameObject ragdollInstance;
    public Transform ragdollHead; // The head transform of the ragdoll
    public Camera playerCamera; // Player's camera
    public GameObject playerBody;
    
    public WeaponPickupController wpController;
    public InventoryManager1 inventory;
    public Collider playerCollider;
    public float lerpSpeed;
    public bool isPushed;
    public LayerMask ignoreLayer;
    private LayerMask currLayers;
    public Rigidbody headRigidbody; // Store the head's Rigidbody
    public Transform joint;
    
    

    void Start()
    {
        if(isNpc){

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        } 
        if(playerCollider != null)
        currLayers = playerCollider.excludeLayers;

        //DisableRagdoll(); // Start with ragdoll disabled
    }
    void Update()
    {
        if(!isLocalPlayer) return;
        if(isPushed && isLocalPlayer && ragdollInstance != null){
            Debug.Log("follow ragdoll");
            FollowRagdoll();
        }else if (Vector3.Distance(joint.transform.localPosition, new Vector3(0, 0.75f, 0)) < 0.01f)
        {
            resetFollow();
        }
    }
    [Command(requiresAuthority = false)]
    public void CmdEnableRagdoll(Vector3 force){
        if(isServer) {
            Debug.Log("cmd enable ragdoll");
            RpcEnableRagdoll(force);
        }
    }

    [ClientRpc]
    public void RpcEnableRagdoll(Vector3 force)
    {
        if (isNpc)
        {
            navAgent.isStopped = true;
        }
        EnableRagdoll(force);
    }

    private void EnableRagdoll(Vector3 force)
    {
        Debug.Log(" enable ragdoll");
        animator.enabled = false; // Disable animation to prevent conflicts
        if (isNpc)
        {
            if(zombieController != null) zombieController.enabled = false;
            
            foreach (Rigidbody rb in ragdollBodies)
            {
                rb.isKinematic = false; // Enable physics
            }
            foreach (Rigidbody rb in ragdollBodies)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
        else
        {
            playerController.enabled = false;
            playerBody.SetActive(false);
        
            inventory.enabled = false;
            
            wpController.enabled = false;
            playerCollider.excludeLayers = ignoreLayer;
        
            if (ragdollInstance == null && isServer)
            {
                
                CmdSpawnRagdoll(force);
            }
            
 
            isPushed = true;

            // Make sure the ragdoll follows the player's head
            
            
        }
        StartCoroutine(RecoverFromRagdoll());
    }
    [Command(requiresAuthority = false)]
    public void CmdSpawnRagdoll(Vector3 force){
        if(!isServer) return;
        Debug.Log("spawned ragdoll");
        ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        DestroyTimer timer = ragdollInstance.GetComponent<DestroyTimer>();
        if(timer != null) timer.timer = recoverDelay;
        NetworkServer.Spawn(ragdollInstance, connectionToClient);
        //RpcFollowHead(connectionToClient, ragdollInstance.GetComponent<NetworkIdentity>());
        RpcApplyForceToRagdoll(ragdollInstance.GetComponent<NetworkIdentity>(), force);
        FindFollowHead(ragdollInstance);
    }
    [ClientRpc]
    private void RpcApplyForceToRagdoll(NetworkIdentity ragdoll, Vector3 force)
    {
        ragdollInstance = ragdoll.gameObject;
        Rigidbody[] rigidbodies = ragdoll.gameObject.GetComponentsInChildren<Rigidbody>();
        if(isLocalPlayer){
            ragdollBodies = rigidbodies;
        }
        

        foreach (Rigidbody rb in rigidbodies)
        {
            if (headRigidbody == null && rb.gameObject.name.Equals("Head")) // Match by name
            {
                headRigidbody = rb;
            }
            // Apply an impulse force in an upward and forward direction
            rb.AddForce(force, ForceMode.Impulse);
        }
    }


    
    void FindFollowHead(GameObject ragdoll){
        ragdollHead = FindChildWithTag(ragdoll.transform, "Head");
        
        if (ragdollHead == null)
        {
            Debug.LogError("Ragdoll head not found!");
        }
        else
        {
            Debug.Log("Ragdoll head assigned successfully.");
        }
        
    }

    Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child; // Return the transform of the child with the specified tag
            }
        }
        return null; // Return null if no child with the tag is found
    }
    


    [Command(requiresAuthority = false)]
    public void CmdDespawnRagdoll(NetworkIdentity ragdollIdentity){
        if(!isServer) return;
        NetworkServer.Destroy(ragdollIdentity.gameObject);
    }

    void FollowRagdoll()
    {
        if (ragdollInstance != null)
        {
            transform.position = Vector3.Lerp(transform.position, GetRagdollPosition(), Time.deltaTime * lerpSpeed);

            joint.position = Vector3.Lerp(transform.position, headRigidbody.transform.position, Time.deltaTime * lerpSpeed);
            joint.localRotation = Quaternion.Lerp(joint.localRotation, headRigidbody.transform.localRotation , Time.deltaTime * lerpSpeed);
        } else {
            resetFollow();
        }
    }
    void resetFollow() {
        joint.localPosition = new Vector3(0, 0.75f, 0);
        joint.localRotation = Quaternion.identity;
    }

    

    private void DisableRagdoll()
    {
        if (isNpc)
        {
            foreach (Rigidbody rb in ragdollBodies)
            {
                
                rb.isKinematic = true;
                
            }
            navAgent.Warp(ragdollEndPosition); // Move agent to ragdoll position
            zombieController.enabled = true;
            navAgent.isStopped = false;
        }
        else
        {
            playerController.enabled = true;
            playerBody.SetActive(true);
        
            inventory.enabled = true;
            
            wpController.enabled = true;
            isPushed = false;
            resetFollow();
        }
        animator.enabled = true;
    }

    private IEnumerator RecoverFromRagdoll()
    {
        yield return new WaitForSeconds(recoverDelay); // Time spent on the ground
        
        ragdollEndPosition = GetRagdollPosition(); // Get final position
        Debug.Log(ragdollEndPosition);

        DisableRagdoll(); // Disable ragdoll physics
        if(isNpc){

            
        if (navAgent != null)
        {
            navAgent.Warp(ragdollEndPosition); // Move agent to ragdoll position
            //navAgent.enabled = true;
            navAgent.isStopped = false; // Resume movement
        }
            // if (animator.HasState(0, Animator.StringToHash("GetUp")))
            // {
            //     animator.Play("GetUp"); // Play recovery animation
            // }
            // else
            // {
                
            //}

        }else {
            
            
            isPushed = false;
        }

    }

    private Vector3 GetRagdollPosition()
    {
        // Find the lowest rigidbody (to avoid floating recovery)
        Vector3 avgPosition = Vector3.zero;
        int count = 0;

        foreach (Rigidbody rb in ragdollBodies)
        {
            avgPosition += rb.position;
            count++;
        }

        return count > 0 ? avgPosition / count : transform.position;
    }

    

    // private IEnumerator SmoothStandUp()
    // {
    //     Quaternion startRot = transform.rotation;
    //     Quaternion targetRot = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    //     float duration = 0.5f;
    //     float elapsed = 0f;

    //     while (elapsed < duration)
    //     {
    //         transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }

    //     transform.rotation = targetRot;
    // }
}
