using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Animations.Rigging;
using TMPro;

public class WeaponPickupController : NetworkBehaviour
{
    public Transform weaponHolder;
    public float pickupRange = 2f; // Range for picking up weapons
    public LayerMask weaponLayer;  // Layer for weapon pickups
    public WeaponController equippedGun;
    private WeaponController currentGun = null;
    public bool canPickup = false;
    public bool isowned = false;
    public bool hasGun = false;
    public string weaponName;
    public TwoBoneIKConstraint rightHand;
    public TwoBoneIKConstraint leftHand;
    public Animator animator;
    public RigBuilder rig;
    public Transform noWeaponRight;
    public Transform noWeaponLeft;

    public float pickupDistance = 3f;  // The maximum distance to check for the gun
    public LayerMask pickupLayer;  // Layer to check for pickup-able guns

    private InteractivePopup popup;
    public Camera camera;
    private WeaponController gun;

    public TextMeshProUGUI ammoText;
    public GameObject RightHandNoWeapon;
    public GameObject LeftHandNoWeapon;
    public GameObject canvas;
    void Start()
    {
        
        if(!isLocalPlayer) {
            canvas.SetActive(false);
            return;
        }

        if(rightHand != null && leftHand != null && RightHandNoWeapon != null &&LeftHandNoWeapon != null){
            rightHand.data.target = RightHandNoWeapon.transform;
            leftHand.data.target = LeftHandNoWeapon.transform;
            rig.Build();
        }
        
    }
    [Command]
    void CmdSetRig(NetworkIdentity gun) {
        RpcSetRig(gun);
    }
    [ClientRpc]
    void RpcSetRig(NetworkIdentity gun) {
         WeaponController weapon = gun.gameObject.GetComponent<WeaponController>();
         equippedGun = weapon;
        weaponName = weapon.weaponName;
        Debug.Log("equipped gun: " + equippedGun);
        if (equippedGun == null) return; // Ensure equippedGun is not null
        rightHand.data.target = equippedGun.grip_r;
        leftHand.data.target = equippedGun.grip_l;
        rig.Build();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.E) && canPickup && equippedGun == null) // Pickup Gun
        {
            Debug.Log("tried to pickup gun");
            StartCoroutine(TryPickupGun(currentGun));
        }

        if (Input.GetKeyDown(KeyCode.Q) && equippedGun != null) // Drop Gun
        {
            DropGun();
        }
        if(equippedGun != null){
            hasGun = true;
        }else{
            hasGun = false;
        }

       // Perform raycast to detect guns in the player's line of sight
        RaycastHit hit;
        Vector3 rayOrigin = camera.transform.position; // Start of the ray (player's position)
        Vector3 rayDirection = camera.transform.forward; // Ray direction (the direction the player is facing)
        float rayDistance = pickupDistance; // Max distance for the raycast

        // Debugging the ray (will show in the Scene view)
        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.green);

        WeaponController newGun = null; // New gun that might be hit

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, pickupLayer))
        {
            // Check if the raycast hit an object with a WeaponController
            newGun = hit.collider.GetComponent<WeaponController>();
        }

        if (newGun != null)
        {
            // If the player is looking at the new gun
            if (!canPickup || currentGun != newGun) // Only show the popup if it's a different gun
            {
                if (canPickup && currentGun != null)
                {
                    // Hide popup for the previous gun if the player switches guns
                    currentGun.HidePopup();
                }

                // Update the current gun and show the popup for the new gun
                canPickup = true;
                currentGun = newGun;
                if(equippedGun== null){

                    currentGun.ShowPopup(transform);
                }
            }
        }
        else
        {
            // If the raycast didn't hit anything or the object is not a gun, hide the popup
            if (canPickup && currentGun != null)
            {
                currentGun.HidePopup(); // Hide popup for the current gun
                canPickup = false;
                currentGun = null;
            }
        }

        if(equippedGun != null && ammoText != null){
            ammoText.text = equippedGun.currentAmmo.ToString() + "/" + equippedGun.maxAmmo.ToString(); 
        }

        
        
    }

    public void DropGun(){
        if(!isLocalPlayer) return;
        CmdResetRig();
        equippedGun.CmdDrop();
        equippedGun = null;
    }
    


    IEnumerator TryPickupGun(WeaponController gun)
    {
        
            if (gun != null)
            {
                Debug.Log("gun is not null");
                NetworkIdentity player = GetComponent<NetworkIdentity>();
                CmdRequestAuthority(gun.netIdentity, player);
                
                Debug.Log("gun before delay");
                yield return new WaitForSeconds(.1f);
                Debug.Log("gun after delay");

                // Call the CmdPickup method to pick up the gun on the server
                gun.CmdPickup(player);

                Debug.Log("gun cmd pickup");
                // Equip the gun (this could be used to switch to the picked-up weapon)
                equippedGun = gun;
                weaponName = gun.weaponName;
                rightHand.data.target = equippedGun.grip_r;
                leftHand.data.target = equippedGun.grip_l;
                rig.Build();
                isowned = true;
                
                CmdSetRig(gun.netIdentity);
                Debug.Log("gun end");
                // Optionally hide any pickup prompt
                // HidePickupPrompt();
            }
        
    }
    [Command(requiresAuthority = false)]
    public void CmdRequestAuthority(NetworkIdentity targetObject, NetworkIdentity player)
    {
        //Make sure the server is handling this
        if (isServer)
        {
            if(targetObject.isOwned){
                targetObject.RemoveClientAuthority();
                Debug.Log("removed owner");
            }
            
            targetObject.AssignClientAuthority(player.connectionToClient);
            Debug.Log("set owner" + player.connectionToClient);
        }
    }
    [Command]
    void CmdResetRig() {
        ResetConstarints();
    }
    [ClientRpc]
    void ResetConstarints(){
        rightHand.data.target = RightHandNoWeapon.transform;
        leftHand.data.target = LeftHandNoWeapon.transform;
        rig.Build();
    }

    
}
