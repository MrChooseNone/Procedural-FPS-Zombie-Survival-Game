using UnityEngine;
using Mirror;
using System.Collections;
using Unity.Cinemachine;
using InfimaGames.LowPolyShooterPack;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UI;




public class WeaponController : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public string weaponName;
    public GameObject projectilePrefab;
    public Transform muzzle;
    public float fireRate = 0.2f;
    public int damage = 10;
    public float projectileSpeed = 20f;
    public float projectileImpulse = 50f;
    public float maxRange = 100f;
    public LayerMask hitMask;
    

    [Header("Ammo and Reload Settings")]
    public int maxAmmo = 30;  // Max ammo per clip
    public int currentAmmo;    // Current ammo count
    public float reloadTime = 2f; // Reload time in seconds

    private float nextFireTime;
    private float reloadTimeLeft; // Time left for the reload to finish
    private Animator animator;
    private NetworkIdentity netIdentity;
    private Rigidbody rb;
    private Collider gunCollider;
    private Camera playerCamera;
    public bool isPlayerInRange;
    public Collider gunPhysicCollider;

    public Transform grip_r;
    public Transform grip_l;
    private NetworkIdentity identityPlayer;

    [Header("Recoil Settings")]
    public float recoilX = -2f;  // Vertical (upward)
    public float recoilY = 1f;   // Horizontal (sideways)
    public float recoilZ = 0.5f; // Slight backward push
    public float recoilSpeed = 15f; // Speed of the recoil effect
    

    private Quaternion targetRecoilRotation;
    private Vector3 targetPosition;
    private bool isRecoiling = false;
    public float returnSpeed = 5f;  // Speed at which the weapon returns
    private Vector3 originalPosition;

    public ScreenShake shake;
    public float magnitude;
    public float duration;

    //-----------audio--------------------
    [SerializeField]
    private AudioSource fireSource;
    public AudioClip fireClip;
    public AudioClip ReloadClip;
    public AudioClip EmptyClip;
    public AudioClip noAmmoClip;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    [SerializeField]
    private AudioSource casingSource;
    public AudioClip[] casingClips;
    public bool hasPlayed = false;
    public float impactThreshold;

    //-----casing------------
    public GameObject casingPrefab;
    [SerializeField]
    private GameObject casingEject;
    public float ejectionForce = 2f;
    public float ejectionTorque = 1f;

    //------smoke--------------
    public ParticleSystem smoke;
    //inventory
    public InventoryManager1 inventory;
    public InteractivePopup interactivePopup;

    //---------------Melee----------------
    public bool isMelee;
    public float attackDelay;
    public GameObject meleeDecal;
    public GameObject meleeEffect;
    public GameObject dismemberEffect;
    public float attackRange;
    private string currAnimation;
    public float probabilityToDismember;
    private int comboStep = 0;
    public float lastAttackTime = 0f;
    public float comboResetTime = 1.5f; // Time before combo resets
    public float comboWindow = 0.4f; // Time to input next attack
    private bool canCombo = false;
    public float attackRadius = 1.2f;
    public float attackDistance = 2.5f; // Adjust to match sword reach
    private SoundEmitter soundEmitter;

    //------------durability-----------
    public float maxDurability;
    public float currDurability;
    public float durabilityLoss;
    public Image durabilityImage;
    public WeaponPickupController weaponPickupController;
    
    

    private void Start()
    {


        netIdentity = GetComponent<NetworkIdentity>();
        rb = GetComponent<Rigidbody>();
        gunCollider = GetComponent<Collider>();
        if (isMelee)
        {

            currentAmmo = 1;
        }
        else currentAmmo = 0;


        animator = GetComponent<Animator>();
        animator.enabled = false;

        smoke.Stop();

        currDurability = maxDurability;


    }

    void Update()
    {
        if (!isOwned) return; // Only local player can fire
        if (currDurability <= 0)
        {
            if (weaponPickupController != null)
            {
                weaponPickupController.DropGun();
                Destroy(gameObject);
            }
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && currentAmmo > 0 && reloadTimeLeft <= 0)
        {
            // Determine shooting direction
            if (playerCamera != null && identityPlayer != null)
            {
                if (!isMelee)
                {

                    Quaternion shootDirection = CalculateShootDirection();
                    CmdFire(muzzle.position, shootDirection, identityPlayer, isMelee);
                    smoke.Play();
                    currentAmmo--;

                    nextFireTime = Time.time + fireRate;
                    Invoke("StopSmoke", 2f);
                }
                else
                { // for melee
                    Debug.Log("melee start");
                    CmdFire(muzzle.position, Quaternion.identity, identityPlayer, isMelee);

                    nextFireTime = Time.time + fireRate;
                }
            }
            else
            {

                Debug.Log("identityplayer is  null" + identityPlayer);
            }
        }
        else if (Input.GetMouseButton(0) && Time.time >= nextFireTime && currentAmmo <= 0 && reloadTimeLeft <= 0)
        {
            if (fireSource != null && EmptyClip != null)
            {
                fireSource.pitch = Random.Range(0.9f, 1.1f);
                fireSource.PlayOneShot(EmptyClip);
                nextFireTime = Time.time + fireRate;
            }
        }

        // Handle reload
        if (Input.GetKeyDown(KeyCode.R) && reloadTimeLeft <= 0 && currentAmmo < maxAmmo)
        {
            // Start reload
            CmdReload();
        }
        if (Input.GetKeyDown(KeyCode.U) && reloadTimeLeft <= 0 && currentAmmo > 0)
        {
            
            CmdUnload();
        }

        // Update reload timer if necessary
        if (reloadTimeLeft > 0)
        {
            reloadTimeLeft -= Time.deltaTime;
        }

        if (durabilityImage != null)
        {
            durabilityImage.fillAmount = Mathf.Clamp01(currDurability / maxDurability);
        }
        
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if collided with ground (tagged "Ground") and strong enough
        if (!hasPlayed )
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce >= impactThreshold)
            {
                fireSource.pitch = Random.Range(0.9f, 1.1f);
                fireSource.volume = Random.Range(0.6f, 0.8f);
                fireSource.PlayOneShot(dropSound);
                hasPlayed = true;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Reset flag when gun leaves the ground
        
            hasPlayed = false;
        
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player") && !isPlayerInRange) // Ensure the object is the player
    //     {
    //         isPlayerInRange = true; // Player is within range to pick up
    //         PlayerInRange = other.gameObject.transform; // set player for popup
    //         ShowPopup();
    //     }
    // }

    // // Detect player exiting the pickup trigger area
    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         isPlayerInRange = false; // Player left the pickup area
    //         HidePopup();
    //     }
    // }

    void StopSmoke(){
        smoke.Stop();
    }

    [Command]
    public void CmdPickup(NetworkIdentity player)
    {
        
        Debug.Log("picked up gun");
        HidePopup();
        Debug.Log(player);
        // Transfer ownership
        // netIdentity.AssignClientAuthority(player.connectionToClient);

        

        // Hide gun on the ground (disable physics)
        inventory = player.GetComponent<InventoryManager1>();
        Debug.Log("weapon inventory: "+ inventory);

        soundEmitter = player.GetComponent<SoundEmitter>();
        
        RpcSetGunState(false);
        RpcPickup(player);
        weaponPickupController= player.GetComponent<WeaponPickupController>();
        // if (playerPickup != null)
        // {
        //     transform.SetParent(playerPickup.weaponHolder);
        //     transform.localPosition = Vector3.zero;  // Adjust as needed
        //     transform.localRotation = Quaternion.identity;  // Adjust as needed
        // }
        TargetAssignCamera(player.connectionToClient, weaponPickupController);
        TargetAssignIdentity(player.connectionToClient, player);
        identityPlayer = player;
        Debug.Log("identityPlayer is set" + identityPlayer);

        RpcSetParent(player);
        fireSource.pitch = Random.Range(0.9f, 1.1f);
        fireSource.volume = Random.Range(0.9f, 1.1f);
        fireSource.PlayOneShot(pickupSound);

    }

    
    [ClientRpc]
    void RpcPickup(NetworkIdentity player) {
        WeaponPickupController playerPickup= player.GetComponent<WeaponPickupController>();
        if (playerPickup != null)
        {
            
            transform.SetParent(playerPickup.weaponHolder);
            transform.localPosition = Vector3.zero;  // Adjust as needed
            transform.localRotation = Quaternion.identity;  // Adjust as needed
        }
    }

    [TargetRpc]
    private void TargetAssignIdentity(NetworkConnectionToClient target, NetworkIdentity player)
    {
        // This is now executed on the client
        identityPlayer = player;

        Debug.Log("identityPlayer is set on client: " + identityPlayer);
    }


    [TargetRpc]
    private void TargetAssignCamera(NetworkConnectionToClient target, WeaponPickupController player)
    {
        Debug.Log("assign camera");
        if (player != null)
        {
            Debug.Log("player is not null");
            // Get the camera from the player's hierarchy
            playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("Player Camera not found!");
            }
        }
        
    }
    [Command]
    public void CmdDrop()
    {
        netIdentity.RemoveClientAuthority();
        RpcSetGunState(true);
        inventory = null;
        soundEmitter = null;
    }

    [Command]
    public void CmdReload()
    {
        // // Prevent reloading if it's already in progress
        // if (reloadTimeLeft > 0 || currentAmmo == maxAmmo)
        //     return;

        // reloadTimeLeft = reloadTime;  // Set the reload time to start

        // int requestAmount = maxAmmo - currentAmmo;
        
        // int bulletCount = RpcGetBulletCount(identityPlayer.connectionToClient, inventory);
        // Debug.Log("Bullet count" + bulletCount);
        // if(requestAmount > bulletCount){
        //     requestAmount = bulletCount;
        // }
        // int reloadAmount = Mathf.Min(requestAmount, maxAmmo);
        if(!isMelee){

            RpcUseBullets(identityPlayer.connectionToClient, inventory);
            
            RpcPlayReloadAnimation(); // Play the reload animation
        }
    }

    [TargetRpc]
    private void RpcUseBullets(NetworkConnectionToClient target,InventoryManager1 inventory){
        // Prevent reloading if it's already in progress
        if (reloadTimeLeft > 0 || currentAmmo == maxAmmo)
            return;

        reloadTimeLeft = reloadTime;  // Set the reload time to start

        int requestAmount = maxAmmo - currentAmmo;
        ItemStack bulletStack = inventory.FindItemByName("Bullet");
        if(bulletStack.itemName == null) return;
            
        int bulletCount = bulletStack.quantity;
        Debug.Log("Bullet count" + bulletCount);
        if(requestAmount > bulletCount){
            requestAmount = bulletCount;
        }
        int reloadAmount = Mathf.Min(requestAmount, maxAmmo);
        Debug.Log("reload amount" + reloadAmount);
        inventory.CmdRemoveItem(bulletStack.uniqueKey, reloadAmount);
        currentAmmo += reloadAmount;
    }
    
    [Command]
    public void CmdUnload()
    {
        
        if(!isMelee){

            RpcUnloadBullets(identityPlayer.connectionToClient, inventory);
            
            RpcPlayReloadAnimation(); // Play the reload animation
        }
    }

    [TargetRpc]
    private void RpcUnloadBullets(NetworkConnectionToClient target,InventoryManager1 inventory){
        // Prevent reloading if it's already in progress
        if (reloadTimeLeft > 0)
            return;

        reloadTimeLeft = reloadTime;  // Set the reload time to start
            
        
        Debug.Log("unload amount" + currentAmmo);
        inventory.CmdAddItem("Bullet", currentAmmo, true);
        currentAmmo = 0;
    }
    // [TargetRpc]
    // private int RpcGetBulletCount(NetworkConnectionToClient target,InventoryManager1 inventory)
    // {
    //     if (inventory != null)
    //     {
    //         ItemStack bulletStack = inventory.GetItem("Bullet");
    //         return bulletStack.quantity;

    //     }
    //     return 0; // No bullets found
    // }

    [ClientRpc]
    private void RpcPlayReloadAnimation()
    {
        if (animator != null)
        {
            //Get the name of the animation state to play, which depends on weapon settings, and ammunition!
			string stateName = "GunARReaload";
			//Play the animation state!
			animator.CrossFade(stateName, 0.2f, 0);
        }
        if(fireSource != null && ReloadClip != null){
                fireSource.pitch = Random.Range(0.9f, 1.1f);
                fireSource.volume = Random.Range(0.9f, 1.1f);
                fireSource.PlayOneShot(ReloadClip);
        }
    }

    [ClientRpc]
    private void RpcSetGunState(bool isDropped)
    {
        rb.isKinematic = !isDropped;
        gunCollider.enabled = isDropped;
        gunPhysicCollider.enabled = isDropped;
        if(isDropped){
            animator.enabled = false;
            transform.SetParent(null);
        } else {
            animator.enabled = true;
            animator.CrossFade(weaponName + "Idle", 0.1f, 0);
        }
        gameObject.SetActive(true);

    }
    [ClientRpc]
    private void RpcSetParent(NetworkIdentity player)
    {
        if (!player.isLocalPlayer) return; // Only the owner should update this

        WeaponPickupController playerPickup = player.GetComponent<WeaponPickupController>();
        if (playerPickup != null)
        {
            transform.SetParent(playerPickup.weaponHolder);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            originalPosition = transform.localPosition;
        }
    }
    // public void Fire()
    // {
    //     if (!isOwned) return; // Ensure only the owner can fire

    //     Quaternion shootDirection = playerCamera.transform.rotation;

    //     // Locally spawn the projectile for instant feedback
    //     GameObject localProjectile = Instantiate(projectilePrefab, muzzle.position, shootDirection);
        

    //     // Destroy the local projectile after a short delay (it will be replaced by the server's authoritative version)
    //     Destroy(localProjectile, 0.2f);

    //     // Send the fire command to the server
    //     CmdFire(shootDirection);
    // }

    [Command]
    public void CmdFire(Vector3 muzzle, Quaternion shootDirection, NetworkIdentity player, bool isMelee)
    { 
        Debug.Log("melee call " + isMelee);
        if(!isMelee){
            currDurability -= durabilityLoss;
            Debug.Log("shoot" + player);
            
            // Spawn bullet on the server
            GameObject bullet = Instantiate(projectilePrefab, muzzle, shootDirection);

            
            NetworkServer.Spawn(bullet);
            if (shake != null)
            {
                Debug.Log("call shake");
                shake.Shake(magnitude, duration);
            }
            ProjectileBullet projectileBullet = bullet.GetComponent<ProjectileBullet>();
            projectileBullet.SetShooter(identityPlayer);
            projectileBullet.SetDamage(damage);
            RpcPlayFireAnimation(0);
            spawnCasing();
            soundEmitter.EmittSound(50);
        } else 
        {
            Debug.Log("melee attack " + player);

            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack > comboResetTime)
            {
                comboStep = 0; // Reset combo if too much time passed
            }

            if (timeSinceLastAttack <= comboWindow)
            {
                comboStep = (comboStep + 1) % 3; // Cycle between 0,1,2
            }
            else
            {
                comboStep = 0; // Reset if too slow
            }

            lastAttackTime = Time.time;
            ApplyRecoil();

            RpcPlayFireAnimation(comboStep);
        }

    }

    public void TriggerMeleeDetection(){
        
        PerformeHit();
    }

    [Command]
    private void PerformeHit(){
        // Get the player's camera for attack origin
        
        Camera cameraPlayer = identityPlayer.GetComponentInChildren<Camera>();
        if (cameraPlayer == null)
        {
            Debug.LogWarning("No camera found for player: " + identityPlayer.name);
            return;
        }
        RaycastHit[] hits;

        if(comboStep != 2){
            Vector3 startSwingPos = transform.position + transform.forward * .2f ; // Start slightly behind
            Vector3 attackDirection = cameraPlayer.transform.forward;
            //Vector3 endSwingPos = transform.position + transform.forward * 1.5f; // End slightly forward
            float newRange = attackDistance/2;
            Debug.DrawLine(startSwingPos, startSwingPos + attackDirection * newRange, Color.green, 1f);


        // Detect enemies within the capsule arc
            hits = Physics.SphereCastAll(startSwingPos, attackRadius, attackDirection, newRange, hitMask);

        }else{

            Vector3 attackOrigin = cameraPlayer.transform.position + cameraPlayer.transform.forward * .2f; // Start point slightly forward
            Vector3 attackDirection = cameraPlayer.transform.forward;
            

            Debug.DrawRay(attackOrigin, attackDirection * attackDistance, Color.blue, 1f); // Debugging
            Debug.DrawRay(attackOrigin, Vector3.down * attackRadius, Color.red, 1f); // Debugging
            Debug.DrawRay(attackOrigin, Vector3.up * attackRadius, Color.red, 1f); // Debugging
            Debug.DrawRay(attackOrigin, Vector3.left * attackRadius, Color.red, 1f); // Debugging
            Debug.DrawRay(attackOrigin, Vector3.right * attackRadius, Color.red, 1f); // Debugging
            hits = Physics.SphereCastAll(attackOrigin, attackRadius, attackDirection, attackDistance, hitMask);
        }

        HashSet<NetworkIdentity> hitEnemies = new HashSet<NetworkIdentity>(); // Track unique enemies

        foreach (RaycastHit hit in hits)
        {
            IDamageble damagable = hit.collider.GetComponentInParent<IDamageble>();
            ZombieAI enemy = hit.collider.GetComponentInParent<ZombieAI>();
            NetworkIdentity enemyIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
            Vector3 hitPosition = hit.point;

            if (damagable != null && enemyIdentity != null  && !hitEnemies.Contains(enemyIdentity) )
            {
                //int comboDamage = damage * (comboStep + 1); // Increase damage per combo step
                currDurability -= durabilityLoss;
                if (enemy != null)
                {
                    hitEnemies.Add(enemyIdentity); // Register this enemy as hit
                    damagable.Damage(damage);

                    float rand = Random.Range(0f, 1f);
                    if (rand < probabilityToDismember && hit.collider.CompareTag("dismember"))
                    {
                        hit.transform.localScale = Vector3.zero;
                        GameObject dismember = Instantiate(dismemberEffect, hitPosition, Quaternion.LookRotation(hit.normal));
                        NetworkServer.Spawn(dismember);
                    }
                    else if (rand < probabilityToDismember && hit.collider.CompareTag("dismemberHead"))
                    {
                        damagable.Damage(9999);
                        hit.transform.localScale = Vector3.zero;
                        GameObject dismember = Instantiate(dismemberEffect, hitPosition, Quaternion.LookRotation(hit.normal));
                        NetworkServer.Spawn(dismember);
                    }
                }
                else
                {
                    hitEnemies.Add(enemyIdentity); // Register this enemy as hit
                    NetworkIdentity networkIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
                    damagable.Damage(damage, networkIdentity);
                }
                GameObject effect = Instantiate(meleeEffect, hitPosition, Quaternion.identity);
                NetworkServer.Spawn(effect);
                GameObject decal = Instantiate(meleeDecal, hitPosition, Quaternion.identity);
                NetworkServer.Spawn(decal);
                
            } 

        }
    }

    [ClientRpc]
    void RpcClientShoot(Vector3 muzzle, Quaternion shootDirection) {
        Debug.Log("shoot client");
        
        
        // Play the fire animation
        
        

        // Spawn projectile and set velocity
        Instantiate(projectilePrefab, muzzle, shootDirection);
        //Rigidbody rbproj = projectile.GetComponent<Rigidbody>();

        // if (rb != null)
        //     rb.linearVelocity = projectile.transform.forward * projectileImpulse;
        
    }

    [ClientRpc]
    private void RpcPlayFireAnimation(int comboStep)
    {
        if(!isMelee){
            ApplyRecoil();
            if (animator != null)
            {
                // Check if the animation is already playing and prevent interruption
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Fire"))
                {
                    animator.Play("Fire", 0, 0.0f); // Fire animation (or use "MeleeAttack" if it's for melee)
                }
            }
            
            if(casingSource != null && casingClips != null){
                StartCoroutine(DelayedCasingSound());
            }
        } else {
            
            if (animator == null) return;

            if (comboStep == 0)
                animator.CrossFade(weaponName + "1", 0.1f, 0);
            else if (comboStep == 1)
                animator.CrossFade(weaponName + "2", 0.1f, 0);
            else if (comboStep == 2)
                animator.CrossFade(weaponName + "3", 0.1f, 0);
        
        }
        if(fireSource != null && fireClip != null){
                fireSource.pitch = Random.Range(0.9f, 1.1f);
                fireSource.volume = Random.Range(0.9f, 1.1f);
                fireSource.PlayOneShot(fireClip);
        }
    }
    private IEnumerator DelayedCasingSound()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 0.8f)); // Small delay before casing sound

        AudioClip currCasingClip = casingClips[Random.Range(0, casingClips.Length)];
        casingSource.pitch = Random.Range(0.9f, 1.1f);
        casingSource.PlayOneShot(currCasingClip);
    }
    private void spawnCasing(){
        GameObject casing = Instantiate(casingPrefab, casingEject.transform.position, casingEject.transform.rotation);
        Rigidbody casingRb = casing.GetComponent<Rigidbody>();
        if (casingRb != null)
        {
            // Calculate ejection direction
            Vector3 ejectionDirection = (transform.right * 0.5f) + (transform.up * 0.7f); // Slightly up and to the right
            ejectionDirection.Normalize();

            // Apply force
            casingRb.AddForce(ejectionDirection * ejectionForce, ForceMode.Impulse);

            // Apply random spin
            Vector3 randomTorque = new Vector3(
                Random.Range(-ejectionTorque, ejectionTorque),
                Random.Range(-ejectionTorque, ejectionTorque),
                Random.Range(-ejectionTorque, ejectionTorque)
            );
            casingRb.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }


    // public Quaternion CalculateShootDirection()
    // {
    //     // Default shooting direction (straight forward)
    //     Vector3 shootTarget = this.playerCamera.transform.position + this.playerCamera.transform.forward * maxRange;

    //     // Check if there's something in front of the player
    //     if (Physics.Raycast(this.playerCamera.transform.position, this.playerCamera.transform.forward, out RaycastHit hit, maxRange, hitMask))
    //     {
    //         shootTarget = hit.point; // Aim at the object hit
    //     }

    //     return Quaternion.LookRotation(shootTarget - muzzle.position);
    // }

        public Quaternion CalculateShootDirection()
        {
            // Get the gun's actual forward direction, including sway & recoil
            Vector3 shootDirection = -muzzle.up; 

            // Check if there's an object in front
            if (Physics.Raycast(muzzle.position, shootDirection, out RaycastHit hit, maxRange, hitMask))
            {
                return Quaternion.LookRotation(hit.point - muzzle.position); // Aim at the object
            }

            return Quaternion.LookRotation(shootDirection);
        }

    public void ApplyRecoil()
    {
        if (!isRecoiling)
        {
            StartCoroutine(SmoothRecoil());
        }
    }

    private IEnumerator SmoothRecoil()
    {
        isRecoiling = true;
        

        Quaternion originalRotation = transform.localRotation;
        Vector3 startPosition = transform.localPosition;

        // Generate a random recoil offset
        targetRecoilRotation = originalRotation * Quaternion.Euler(
            Random.Range(recoilX * 0.8f, recoilX * 1.2f),
            Random.Range(-recoilY, recoilY),
            Random.Range(-recoilZ, recoilZ) 
        );
        targetPosition = originalPosition + transform.localRotation * new Vector3(0, 0, -recoilZ);

        float t = 0f;

        // Smoothly move the weapon to the recoil position
        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localRotation = Quaternion.Slerp(originalRotation, targetRecoilRotation, t);
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        isRecoiling = false;
        StartCoroutine(ReturnToOriginalPosition());
    }

    private IEnumerator ReturnToOriginalPosition()
    {
        float t = 0f;
        Vector3 startPosition = transform.localPosition;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            transform.localPosition = Vector3.Lerp(startPosition, originalPosition, t);
            yield return null;
        }
    }

    //-------------popup-------------------

    public void ShowPopup(Transform PlayerInRange) {
        if( interactivePopup != null){
            Debug.Log("show popup");
            interactivePopup.ShowPopup(PlayerInRange);
        }
    }

    public void HidePopup( ) {
        if( interactivePopup != null){
            Debug.Log("hide popup");
            interactivePopup.HidePopup();
        }
    }

    
    
}

