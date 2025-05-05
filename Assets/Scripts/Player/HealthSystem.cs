
using Mirror;
using UnityEngine;
using UnityEngine.UI;




public class HealthSystem : NetworkBehaviour, IDamageble
{
    public float maxHealth = 100f;
    private float currentHealth;
    public Image healthBar;  // Reference to the Image component of the health bar

    public float MaxHealth { get; set ; }
    public float CurrentHealth { get; set; }

    public ScreenShake shake;
    public DamageEffect damageEffect;

    public GameObject deathScreen;

    public GameObject playerRagdoll;
    public GameObject playerHands;
    public GameObject playerBody;
    public Transform deathScreenCameraPosition;
    public Camera camera;
    public GameObject staminaUI;
    public GameObject ammoUI;
    public FirstPersonController fpsController;
    public WeaponPickupController wpController;
    public InventoryManager1 inventory;

    //camera position
    public Vector3 deathScreenPosition;  // Set in Inspector
    public Quaternion deathScreenRotation; // Set in Inspector
    public float lerpSpeed = 2f;  // Speed of movement
    private bool moveToDeathScreen = false;
    public GameObject player;
    private Vector3 currPlayerPosition;
    private Quaternion currPlayerRotation; // Set in Inspector
    private NetworkIdentity currRagdoll;
    private GameObject currRagdollGameobject;
    public bool isDead = false;
    public float magnitude;
    public float duration;

    void Start()
    {
        currentHealth = maxHealth; // Initialize health
        UpdateHealthBar();
        isDead = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)){

            shake.Shake(magnitude, duration);
        }
        if (Input.GetKeyDown(KeyCode.Y) && isLocalPlayer) // Press Y to call Die()
        {
            CmdDie();
        }
        if (moveToDeathScreen && currRagdoll != null && isLocalPlayer)
        {
            currRagdollGameobject = currRagdoll.gameObject;
            Debug.Log("ragdoll is not null");

            // Move the player towards the death screen position
            // Vector3 targetPosition = currRagdoll.transform.position + Vector3.up * 2f - currRagdoll.transform.forward * .2f;
            player.transform.position = Vector3.MoveTowards(player.transform.position, currPlayerPosition + deathScreenPosition * 4, Time.deltaTime * lerpSpeed);


            // Calculate direction to look at the ragdoll
            Vector3 lookTarget = currRagdollGameobject.transform.position;
            if(currRagdollGameobject == null ) Debug.Log("ragdoll game object is null");

            Debug.Log("ragdoll game object" + currRagdollGameobject);


            Vector3 directionToRagdoll = (lookTarget - player.transform.position).normalized;
            
            // Calculate the target rotation to look at the ragdoll
            Quaternion targetRotation = Quaternion.LookRotation(directionToRagdoll);

            // Smoothly rotate towards the ragdoll
            player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, targetRotation, Time.deltaTime * lerpSpeed * 100);
            // Smoothly interpolate the camera's rotation to (0, 0, 0) in Euler angles
            //så jävla facking stupid shit ass
            camera.transform.rotation = Quaternion.Euler(73f, 180f, 0f);


            Rigidbody rb = player.GetComponent<Rigidbody>();
            
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.DrawRay(player.transform.position, directionToRagdoll * 5, Color.red, 2f);
            // Debug.Log("Direction to Ragdoll: " + directionToRagdoll);
            // Debug.Log("Player Position: " + player.transform.position);
            // Debug.Log("Look Target Position: " + lookTarget);
            // Debug.Log("Direction to Ragdoll: " + directionToRagdoll);


        }
    }
    public void Damage( float damage, NetworkIdentity target)
    {
        if (target != null) 
        {
            CmdDamage(target, damage);
        }
    }

    [Command(requiresAuthority = false)] //  Allow any client to request damage
    public void CmdDamage(NetworkIdentity target, float damage)
    {
        if (target != null)
        {
            target.GetComponent<HealthSystem>().RpcDamage(damage);
        }
    }

    [TargetRpc] //  Runs on the target player's client
    public void RpcDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (shake != null)
        {
            Debug.Log("call shake");
            shake.Shake(magnitude, duration);
        }
        if (damageEffect != null)
        {
            Debug.Log("call damageEffect");
            damageEffect.ShowDamage();
        }

        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");
        UpdateHealthBar();

        if (currentHealth <= 0 && !isDead)
        {
            Debug.Log("call die");
            CmdDie();
        }
    }
    
    void UpdateHealthBar()
    {
        if(!isLocalPlayer) return;
        // Set the fill amount based on the current health
        if(healthBar != null){

        healthBar.fillAmount = currentHealth / maxHealth;
        }
    }
    [Command]
    private void CmdDie(){
        RpcDie(connectionToClient);
        RpcDropInventory(connectionToClient);
    }

    [TargetRpc]
    private void RpcDie(NetworkConnectionToClient target)
    {
        // if(!isLocalPlayer){return;}
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Dead";
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        currPlayerPosition = player.transform.position;
        Quaternion tempRotation = camera.transform.rotation;
        currPlayerRotation = Quaternion.Euler(0, tempRotation.eulerAngles.y, 0);

        Debug.Log($"{gameObject.name} has died.");

        CmdSpawnPlayerRagdoll();
        RpcDisableBody(networkIdentity);

        // Destroy the player model (or disable it)
        playerBody.SetActive(false);
        // playerHands.SetActive(false);

        staminaUI.SetActive(false);
        ammoUI.SetActive(false);
        inventory.enabled = false;
        healthBar.enabled = false;
        damageEffect.enabled = false;
        wpController.enabled = false;
        fpsController.enabled = false;

        

        // Show Death Screen
        ShowDeathScreen();

    }
    [ClientRpc]
    private void RpcDisableBody(NetworkIdentity networkIdentity){
        GameObject deadplayer = networkIdentity.gameObject;
        HealthSystem healthSystem = deadplayer.GetComponent<HealthSystem>();
        healthSystem.playerBody.SetActive(false);
    }

    [TargetRpc]
    private void RpcDropInventory(NetworkConnectionToClient target){
        Debug.Log("inventorymanager " + inventory);
        foreach(var item in inventory.inventory){
            Debug.Log("item data in command for key" + item.Key + item.Value);
            ItemData itemData = inventory.FindItemData(item.Value.itemName);
            Debug.Log("item data in command " + itemData + itemData.prefab);
            Vector3 dropPosition = transform.position ;
            float terrainHeight = Terrain.activeTerrain.SampleHeight(dropPosition);
            if(dropPosition.y <= terrainHeight){
                dropPosition.y = terrainHeight;
            }
            int iterations = Mathf.CeilToInt((float)item.Value.quantity / item.Value.quantityPerItem);
            
                for(int i = 0; i < iterations; i++){

                    GameObject droppedItem = Instantiate(itemData.prefab, dropPosition, Quaternion.identity);
                    NetworkServer.Spawn(droppedItem);
                }
            


            inventory.CmdRemoveItem(item.Key, item.Value.quantity );
        }
        //drop gun
        if(wpController != null && wpController.equippedGun != null){
            wpController.DropGun();
        }
    }

    [Command]
    private void CmdSpawnPlayerRagdoll(){
        if (!isServer) return; // Ensure only the server runs this
        GameObject ragdollInstance = Instantiate(playerRagdoll, transform.position, transform.rotation);
        CopyTransforms(transform, ragdollInstance.transform);
        NetworkServer.Spawn(ragdollInstance);
        NetworkIdentity networkIdentity = ragdollInstance.GetComponent<NetworkIdentity>();
        RpcSpawnRagdoll(connectionToClient, networkIdentity);

    }
    [TargetRpc]
    private void RpcSpawnRagdoll(NetworkConnectionToClient target, NetworkIdentity networkIdentity){
        currRagdoll = networkIdentity;
        Debug.Log("Ragdoll NetworkIdentity: " + currRagdoll.gameObject.name); // Ensure it's set to the ragdoll
        
    }

    [Command]
    private void CmdDespawnPlayerRagdoll(NetworkIdentity networkIdentity){
        NetworkServer.Destroy(networkIdentity.gameObject);
    }

    // Recursive function to copy pose from player to ragdoll
    void CopyTransforms(Transform source, Transform target)
    {
        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);
            Transform targetChild = target.Find(sourceChild.name);
            
            if (targetChild != null)
            {
                targetChild.position = sourceChild.position;
                targetChild.rotation = sourceChild.rotation;
                CopyTransforms(sourceChild, targetChild);
            }
        }
    }

    void IDamageble.Die()
    {
        CmdDie();
    }

    void ShowDeathScreen()
    {
        deathScreen.SetActive(true);
        moveToDeathScreen = true;
        Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
    }

    void HideDeathScreen(){
        deathScreen.SetActive(false);
        moveToDeathScreen = false;
        Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
    }

    [Command]
    public void RespawnButton(){
        RpcRespawn(connectionToClient);
    }

    [TargetRpc]
    public void RpcRespawn(NetworkConnectionToClient target)
    {
        if(!isLocalPlayer){return;}
        isDead = false;
        gameObject.tag = "Player";
        player.transform.position = new Vector3(0f, Terrain.activeTerrain.SampleHeight(Vector3.zero) + 5, 0f);
        CmdResetHealth();
        

        Debug.Log($"{gameObject.name} has respawned.");

        CmdDespawnPlayerRagdoll(currRagdoll);
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        RpcEnableBody(networkIdentity);

        // Destroy the player model (or disable it)
        playerBody.SetActive(true);
        // playerHands.SetActive(true);
        
        staminaUI.SetActive(true);
        ammoUI.SetActive(true);
        inventory.enabled = true;
        healthBar.enabled = true;
        damageEffect.enabled = true;
        wpController.enabled = true;
        fpsController.enabled = true;

        

        // Show Death Screen
        HideDeathScreen();

    }

    [ClientRpc]
    private void RpcEnableBody(NetworkIdentity networkIdentity){
        GameObject deadplayer = networkIdentity.gameObject;
        HealthSystem healthSystem = deadplayer.GetComponent<HealthSystem>();
        healthSystem.playerBody.SetActive(true);
    }

    // Command to reset health on the server
    [Command]
    public void CmdResetHealth()
    {
        currentHealth = maxHealth;
        RpcUpdateHealth(connectionToClient, currentHealth); // Sync health across all clients
    }

    // ClientRpc to update health on the client
    [TargetRpc]
    private void RpcUpdateHealth(NetworkConnectionToClient target, float health)
    {
        currentHealth = health;
        UpdateHealthBar(); // Update the health bar on the client
    }
}









/* 
void Start()
    {
        currentHealth = maxHealth; // Initialize health
        UpdateHealthBar();
        isDead = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y) && isLocalPlayer) // Press Y to call Die()
        {
            CmdDie();
        }
        if (moveToDeathScreen && currRagdoll != null && isLocalPlayer)
        {
            currRagdollGameobject = currRagdoll.gameObject;
            Debug.Log("ragdoll is not null");

            // Move the player towards the death screen position
            // Vector3 targetPosition = currRagdoll.transform.position + Vector3.up * 2f - currRagdoll.transform.forward * .2f;
            player.transform.position = Vector3.MoveTowards(player.transform.position, currPlayerPosition + deathScreenPosition * 4, Time.deltaTime * lerpSpeed);


            // Calculate direction to look at the ragdoll
            Vector3 lookTarget = currRagdollGameobject.transform.position;
            if(currRagdollGameobject == null ) Debug.Log("ragdoll game object is null");

            Debug.Log("ragdoll game object" + currRagdollGameobject);


            Vector3 directionToRagdoll = (lookTarget - player.transform.position).normalized;
            
            // Calculate the target rotation to look at the ragdoll
            Quaternion targetRotation = Quaternion.LookRotation(directionToRagdoll);

            // Smoothly rotate towards the ragdoll
            player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, targetRotation, Time.deltaTime * lerpSpeed * 100);
            // Smoothly interpolate the camera's rotation to (0, 0, 0) in Euler angles
            //så jävla facking stupid shit ass
            camera.transform.rotation = Quaternion.Euler(73f, 180f, 0f);


            Rigidbody rb = player.GetComponent<Rigidbody>();
            
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.DrawRay(player.transform.position, directionToRagdoll * 5, Color.red, 2f);
            // Debug.Log("Direction to Ragdoll: " + directionToRagdoll);
            // Debug.Log("Player Position: " + player.transform.position);
            // Debug.Log("Look Target Position: " + lookTarget);
            // Debug.Log("Direction to Ragdoll: " + directionToRagdoll);


        }
    }
    [TargetRpc]
    public void RpcDamage(NetworkConnectionToClient target, float damage){
        if (isDead) return;
        currentHealth -= damage;
        if (shake != null){
            Debug.Log("call shake");
            shake.Shake(.2f, 3f);
        }
        if (damageEffect != null){
            Debug.Log("call damageEffect");
            damageEffect.ShowDamage();
        }
        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");
        UpdateHealthBar();
        if (currentHealth <= 0 && !isDead)
        {
            Debug.Log("call die");
            CmdDie();
        }
    }
    public void Damage(float damage)
    {
        CmdDamage(damage); // Clients request damage via a Command
    }

    [Command] // Runs on the server
    public void CmdDamage(float damage)
    {
        RpcDamage(connectionToClient, damage); // Server updates the specific client
    }
    
    void UpdateHealthBar()
    {
        if(!isLocalPlayer) return;
        // Set the fill amount based on the current health
        if(healthBar != null){

        healthBar.fillAmount = currentHealth / maxHealth;
        }
    }
    [Command]
    private void CmdDie(){
        RpcDie(connectionToClient);
    }

    [TargetRpc]
    private void RpcDie(NetworkConnectionToClient target)
    {
        // if(!isLocalPlayer){return;}
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Dead";
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        currPlayerPosition = player.transform.position;
        Quaternion tempRotation = camera.transform.rotation;
        currPlayerRotation = Quaternion.Euler(0, tempRotation.eulerAngles.y, 0);

        Debug.Log($"{gameObject.name} has died.");

        CmdSpawnPlayerRagdoll();
        RpcDisableBody(networkIdentity);

        // Destroy the player model (or disable it)
        playerBody.SetActive(false);
        playerHands.SetActive(false);

        staminaUI.SetActive(false);
        ammoUI.SetActive(false);
        inventory.enabled = false;
        healthBar.enabled = false;
        damageEffect.enabled = false;
        wpController.enabled = false;
        fpsController.enabled = false;

        

        // Show Death Screen
        ShowDeathScreen();

    }
    [ClientRpc]
    private void RpcDisableBody(NetworkIdentity networkIdentity){
        GameObject deadplayer = networkIdentity.gameObject;
        HealthSystem healthSystem = deadplayer.GetComponent<HealthSystem>();
        healthSystem.playerBody.SetActive(false);
    }


    [Command]
    private void CmdSpawnPlayerRagdoll(){
        if (!isServer) return; // Ensure only the server runs this
        GameObject ragdollInstance = Instantiate(playerRagdoll, transform.position, transform.rotation);
        CopyTransforms(transform, ragdollInstance.transform);
        NetworkServer.Spawn(ragdollInstance);
        NetworkIdentity networkIdentity = ragdollInstance.GetComponent<NetworkIdentity>();
        RpcSpawnRagdoll(connectionToClient, networkIdentity);

    }
    [TargetRpc]
    private void RpcSpawnRagdoll(NetworkConnectionToClient target, NetworkIdentity networkIdentity){
        currRagdoll = networkIdentity;
        Debug.Log("Ragdoll NetworkIdentity: " + currRagdoll.gameObject.name); // Ensure it's set to the ragdoll
        
    }

    [Command]
    private void CmdDespawnPlayerRagdoll(NetworkIdentity networkIdentity){
        NetworkServer.Destroy(networkIdentity.gameObject);
    }

    // Recursive function to copy pose from player to ragdoll
    void CopyTransforms(Transform source, Transform target)
    {
        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);
            Transform targetChild = target.Find(sourceChild.name);
            
            if (targetChild != null)
            {
                targetChild.position = sourceChild.position;
                targetChild.rotation = sourceChild.rotation;
                CopyTransforms(sourceChild, targetChild);
            }
        }
    }

    void IDamageble.Die()
    {
        CmdDie();
    }

    void ShowDeathScreen()
    {
        deathScreen.SetActive(true);
        moveToDeathScreen = true;
        Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
    }

    void HideDeathScreen(){
        deathScreen.SetActive(false);
        moveToDeathScreen = false;
        Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
    }

    [Command]
    public void RespawnButton(){
        RpcRespawn(connectionToClient);
    }

    [TargetRpc]
    public void RpcRespawn(NetworkConnectionToClient target)
    {
        if(!isLocalPlayer){return;}
        isDead = false;
        gameObject.tag = "Player";
        player.transform.position = Vector3.zero;
        CmdResetHealth();
        

        Debug.Log($"{gameObject.name} has respawned.");

        CmdDespawnPlayerRagdoll(currRagdoll);
        NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        RpcEnableBody(networkIdentity);

        // Destroy the player model (or disable it)
        playerBody.SetActive(true);
        playerHands.SetActive(true);
        
        staminaUI.SetActive(true);
        ammoUI.SetActive(true);
        inventory.enabled = true;
        healthBar.enabled = true;
        damageEffect.enabled = true;
        wpController.enabled = true;
        fpsController.enabled = true;

        

        // Show Death Screen
        HideDeathScreen();

    }

    [ClientRpc]
    private void RpcEnableBody(NetworkIdentity networkIdentity){
        GameObject deadplayer = networkIdentity.gameObject;
        HealthSystem healthSystem = deadplayer.GetComponent<HealthSystem>();
        healthSystem.playerBody.SetActive(true);
    }

    // Command to reset health on the server
    [Command]
    public void CmdResetHealth()
    {
        currentHealth = maxHealth;
        RpcUpdateHealth(connectionToClient, currentHealth); // Sync health across all clients
    }

    // ClientRpc to update health on the client
    [TargetRpc]
    private void RpcUpdateHealth(NetworkConnectionToClient target, float health)
    {
        currentHealth = health;
        UpdateHealthBar(); // Update the health bar on the client
    }
}

*/