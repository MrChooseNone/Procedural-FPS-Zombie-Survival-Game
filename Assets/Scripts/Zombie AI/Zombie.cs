using System.Collections;
using Mirror;

using UnityEngine;
using UnityEngine.AI;
using Steamworks;
using System.Linq;



public class ZombieAI : NetworkBehaviour, IDamageble
{
    public float perceptionRange = 10f;   // Distance within which the zombie detects the player
    public float perceptionAngle = 120f;
    //public float spawnRange = 50f;        // Range within which the zombie can spawn
    public float followSpeed = 3.5f;      // Speed at which the zombie follows the player

   
    public NavMeshAgent navAgent;        // Reference to the zombie's NavMeshAgent
    [SyncVar(hook = nameof(OnFollowStateChanged))]
    public bool isFollowing;

    public bool isAttacking = false;
    private Vector3 randomDestination;    // The randomly chosen spawn location
    public NavMeshGenerator navGen;
    //------------------State machine----------------
    public EnemySTateMachine STateMachine{get; set;}
    public IdleState IdleState{get;set;}
    public ChaseState chaseState{get;set;}
    public AttackState attackState{get;set;}
    public float MaxHealth { get ; set; } = 100f;
    public float CurrentHealth { get; set; }

    //-----------idle state------------------
    public float RandomMovementRange = 15f;
    //public float RandomMovementSpeed = 4f;
    //----------attack state-----------
    public float damageAmount = 10f;
    public float attackCooldown = 2f;
    public float distanceToAttack;
    //------------chase state-----------------
    //public float chaseAcceleration = 1f;
    public float distanceToPlayer;

    //------------network-------------------
    [SerializeField]
    [SyncVar(hook = nameof(OnClosestPlayerChanged))]
    public GameObject closestPlayer;
    [SerializeField]
    private GameObject[] allTargets;

    //-------die------------
    [SerializeField]
    private GameObject head;
    [SerializeField]
    private GameObject bloodEffectPrefab;
    [SerializeField]
    //private GameObject headlessPrefab;
    public float destroyDelay = 2f;
    public bool hasSpawnedBlood = false;
    
    private RagdollController ragdollController;
    public Rigidbody[] rb;

    //knockback
    private bool isKnockedBack;
    public float knockbackDuration = 0.3f; // How long knockback lasts
    public float knockbackStun = 0.2f;
    public float knockbackDistance = 1.5f; // How far knockback moves
    public float knockbackSpeed = 5f; // How fast knockback moves
    //spawn
    public Vector3 spawnAreaCenter; // Center of the spawn area
    public Vector3 spawnAreaSize; // Size of the spawn area

    //wall
    public float checkDistance = 10f;
    
    public float damageWallPerHit = 10f;
    private bool isAttackingWall = false;
    //follow after shot
    public float followTime;

    //animations
    public Animator zombieAnimator;
    public string currAnimation;
    
    private CustomNetworkManager manager;
    public GameObject headObject;
    //attack types
    public bool isLanchZombie = false;
    public bool isExploadZombie = false;
    public bool isBloatedZombie = false;
    public bool isScreamerZombie = false;
    public bool isNormalZombie = false;

    //attack variables
    public GameObject explosionEffect;
    public float explosionRadius = 5f;
    public int explosionDamage = 20;
    public bool hasExploaded = false;
    private NetworkIdentity networkZombieId;
    public LayerMask obstacleMask;
    public float eyeHeight;
    //-----------------Audio--------------------
    public AudioSource audioSource;
    public AudioClip[] idleSounds;
    public AudioClip[] chaseSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] deathSounds;

    private CustomNetworkManager Manager
    {

        get
        {
            if (manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    

    private void Awake()
    {
        navGen = FindAnyObjectByType<NavMeshGenerator>();
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = followSpeed;
        //----------state machine-------------
        STateMachine = new EnemySTateMachine();
        IdleState = new IdleState(this, STateMachine);
        chaseState = new ChaseState(this, STateMachine);
        attackState = new AttackState(this, STateMachine);
        STateMachine.Initialize(IdleState);
        ragdollController = GetComponent<RagdollController>();
        rb = GetComponentsInChildren<Rigidbody>();
        networkZombieId =GetComponent<NetworkIdentity>();
    }

    // Start is called before the first frame update
    [Server]
    IEnumerator Start()
    {
        // Generate a random position within the spawn area (XZ only)
        Vector3 randomPosition = spawnAreaCenter + new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            200f, // Set Y = 0 for now, we'll adjust it using terrain
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        // Adjust Y to match terrain height + terrain base Y
        float terrainHeight = Terrain.activeTerrain.SampleHeight(randomPosition) + Terrain.activeTerrain.GetPosition().y;
        randomPosition.y = terrainHeight;
        transform.position = randomPosition;

        // Debugging: Log the final position with adjusted Y
        Debug.Log("Final position after terrain height adjustment: " + randomPosition);
        if(navGen != null){

            yield return new WaitUntil(() => navGen.isNavMesh);
            allTargets = GameObject.FindGameObjectsWithTag("Player");
            CurrentHealth = MaxHealth;

            navAgent = GetComponent<NavMeshAgent>();
            navAgent.speed = followSpeed;

            // // Spawn the zombie at a random position
            // randomDestination = new Vector3(Random.Range(-spawnRange, spawnRange), 0f, Random.Range(-spawnRange, spawnRange));
            // randomDestination.y = Terrain.activeTerrain.SampleHeight(randomDestination);
            // transform.position = randomDestination;

            // // Set the initial zombie destination to a random point within spawn range
            // navAgent.SetDestination(randomDestination);
            CurrentHealth = MaxHealth;
        }
        
    }

    #region Animation trigger
    private void AnimationTriggerEvent(AnimationTriggerType triggerType){
        STateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }
    public enum AnimationTriggerType{
            EnemyDamage,
            PlayFootstepSound
    }
    #endregion

    [Server]
    void Update()
    {
        followTime -= Time.fixedDeltaTime;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] defenders = GameObject.FindGameObjectsWithTag("Defend");

        // Merge the arrays into allTargets
        allTargets = players.Concat(defenders).ToArray();
        Debug.Log("Server is updating targets");

        if (followTime <= 0)
        {
            FindClosestPlayer();
        }

        if (closestPlayer != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.transform.position);

            if (distanceToPlayer <= perceptionRange && !isFollowing)
            {
                Vector3 dirToPlayer = (closestPlayer.transform.position - transform.position).normalized;
                float angleBetween = Vector3.Angle(transform.forward, dirToPlayer);

                if (angleBetween < perceptionAngle / 2f)
                {
                    // Adjust raycast origin to enemy's "eye level"
                    Vector3 eyePosition = transform.position + Vector3.up * eyeHeight; // e.g. eyeHeight = 1.6f

                    // Raycast to check for obstacles
                    if (!Physics.Raycast(eyePosition, dirToPlayer, distanceToPlayer, obstacleMask))
                    {
                        StartFollowingPlayer();
                    }

                    else
                    {
                        Debug.DrawRay(eyePosition, dirToPlayer * distanceToPlayer, Color.red);
                    }
        
                }
            }
            else if (distanceToPlayer > perceptionRange && isFollowing && followTime <= 0)
            {
                StopFollowingPlayer();
            }

            isAttacking = (distanceToPlayer < distanceToAttack);

            // Path check & wall detection
            if(navAgent.isActiveAndEnabled){
                NavMeshPath path = new NavMeshPath();
                navAgent.CalculatePath(closestPlayer.transform.position, path);
            }

            if (!isAttackingWall)
            {
                Vector3 directionToPlayer = (closestPlayer.transform.position - transform.position).normalized;

                if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, checkDistance))
                {
                    if (hit.collider.CompareTag("Wall"))
                    {
                        Debug.Log("Wall detected blocking the path. Attacking wall.");
                        StartCoroutine(AttackWall(hit.collider.gameObject));
                    }
                }
            }
        }
        else
        {
            if (isFollowing) StopFollowingPlayer();
            isAttacking = false;
        }

        // State machine update
        if (STateMachine != null)
        {
            STateMachine.CurrentEnemyState.FrameUpdate();
        }

        // Optional animation state switching (currently commented out)
        // if (isFollowing)
        //     ChangeAnimation("Zombie run");
        // else
        //     ChangeAnimation("Zombie idle");
    }

    

    IEnumerator AttackWall(GameObject wall)
    {
        isAttackingWall = true;
        navAgent.SetDestination(wall.transform.position);

        WallHealth wallHealth = wall.GetComponentInParent<WallHealth>();
        while (wallHealth != null && wallHealth.health > 0)
        {
            Debug.Log("wall take damage");
            wallHealth.TakeDamage(damageWallPerHit);
            yield return new WaitForSeconds(attackCooldown);
        }

        //navAgent.isStopped = false;
        isAttackingWall = false;
    }


    [Server]
    public void ChangeAnimation(string newAnimation){
        if(currAnimation != newAnimation){
            currAnimation = newAnimation;
            zombieAnimator.CrossFade(newAnimation, 0.1f, 0);
        }
    }
    void OnFollowStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("Zombie is now following the player on client.");
        }
        else
        {
            Debug.Log("Zombie stopped following.");
        }
    }
    void OnClosestPlayerChanged(GameObject oldPlayer, GameObject newPlayer)
    {
        if (newPlayer != null)
        {
            Debug.Log("Closest player updated on client: " + newPlayer.name);
        }
    }
    [Server]
    void FixedUpdate(){
        STateMachine.CurrentEnemyState.PhysicsUpdate();
    }
    [Server]
    void FindClosestPlayer()
    {
        
        float shortestDistance = Mathf.Infinity;
        GameObject newClosestPlayer = null;

        // if (Manager.GamePlayers.Count != players.Length)
        // {
        //     players = GameObject.FindGameObjectsWithTag("Player");
        // }

        foreach (var player in allTargets)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < shortestDistance && distance <= perceptionRange)
            {
                shortestDistance = distance;
                newClosestPlayer = player;
            }
        }

        closestPlayer = newClosestPlayer;
    }
    [Server]
    // Start following the player
    public void StartFollowingPlayer()
    {
        isFollowing = true;
    }
    [Server]
    // Stop following the player and return to random wandering
    public void StopFollowingPlayer()
    {
        isFollowing = false;
        if(closestPlayer != null){

            Vector3 playerLastPos = closestPlayer.transform.position;
            // Optionally, you can make the zombie wander randomly after losing track of the player
            randomDestination = new Vector3(Random.Range(playerLastPos.x -5, playerLastPos.x +5), 0f, Random.Range(playerLastPos.z -5, playerLastPos.z +5));
            navAgent.SetDestination(randomDestination);
        }

    }
[Server]
    public void Damage(float amount, NetworkIdentity networkIdentity = null, NetworkIdentity shooterIdent = null)
    {
        CurrentHealth -= amount;

        if (CurrentHealth <= 0f)
        {
            Die();
            if (shooterIdent != null)
            {
                PlayerSkills playerSkills = shooterIdent.GetComponent<PlayerSkills>();
                if (playerSkills != null)
                {
                    playerSkills.GainXP(SkillType.Marksmanship, 20f);
                }
            }
        } 
    }
[Server]
    public IEnumerator ApplyKnockback(Vector3 hitDirection)
    {
        isKnockedBack = true;

        // Disable NavMeshAgent while moving
        navAgent.isStopped = true;
        hitDirection.y = 0;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + hitDirection.normalized * knockbackDistance;

        float elapsedTime = 0f;

        while (elapsedTime < knockbackDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / knockbackDuration);
            elapsedTime += Time.deltaTime * knockbackSpeed;
            yield return null;
        }
        while (knockbackDuration < elapsedTime && elapsedTime < knockbackDuration + knockbackStun)
        {
            elapsedTime += Time.deltaTime * knockbackSpeed;
            yield return null;
        }

        // Re-enable NavMeshAgent
        navAgent.isStopped = false;
        isKnockedBack = false;
    }
[Server]
    public void Die()
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.volume = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Length)]);
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        // Spawn blood effect on all clients
        if (bloodEffectPrefab != null && !hasSpawnedBlood)
        {

            RpcSpawnBloodEffect(head.transform.position);
            hasSpawnedBlood = true;
        }
        // Spawn headless version
        // GameObject headlessZombie = Instantiate(headlessPrefab, transform.position, transform.rotation);
        // NetworkServer.Spawn(headlessZombie);
        ragdollController.CmdEnableRagdoll(Vector3.zero);
        GetComponent<ZombieDeath>().Die();

        // Destroy original
        // NetworkServer.Destroy(gameObject);
        if(isBloatedZombie && !hasExploaded){
            Debug.Log("Called expload");
            Explode();
            hasExploaded = true;
        }else{
            StartCoroutine(DelayDestroy(gameObject));
        }

    }

    // Coroutine to delay zombie destruction
    [Server]
    private IEnumerator DelayDestroy(GameObject headless)
    {
        yield return new WaitForSeconds(destroyDelay);

        // Ensure only the zombie is destroyed
        if (CompareTag("Zombie")) 
        {
            NetworkServer.Destroy(headless);
        }
    }

    // RPC to remove the head (executed on all clients)
    [ClientRpc]
    void RpcRemoveHead(GameObject headObject)
    {
        // Set the head to be small (simulate removal)
        headObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        // Optionally, you can destroy it instead
        // Destroy(headObject);
    }

    // RPC to spawn the blood effect (executed on all clients)
    [ClientRpc]
    void RpcSpawnBloodEffect(Vector3 position)
    {
        GameObject bloodEffectPrefabRef = Instantiate(bloodEffectPrefab, position, Quaternion.identity);
        //bloodEffectPrefabRef.transform.SetParent(headObject.transform);
    }
    //zombie Attacks----------------------------------------
    [Server]
    private void OnTriggerEnter(Collider  collision)
    {
        if (attackState != null && attackState.IsCharging())
        {
            IDamageble damageable = collision.gameObject.GetComponentInParent<IDamageble>();
            NetworkIdentity networkId = collision.gameObject.GetComponentInParent<NetworkIdentity>();
            if (damageable != null && networkId != null && networkId != networkZombieId)
            {
                damageable.Damage(damageAmount, networkId);
                Debug.Log("Charged into player for damage!");
            }
        }
    }
    [Server]
    public void Explode()
    {
        Debug.Log("executed expload");
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            NetworkServer.Spawn(effect); // Sync explosion effect across clients
            Destroy(effect, 3f);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in hitColliders)
        {
            IDamageble target = col.GetComponentInParent<IDamageble>();
            NetworkIdentity netId = col.GetComponentInParent<NetworkIdentity>();
            WallHealth wallHealth = col.GetComponentInParent<WallHealth>();
            if (target != null && netId != null && target != GetComponent<IDamageble>())
            {
                target.Damage(explosionDamage, netId);
            }
            if(wallHealth != null){
                wallHealth.TakeDamage(explosionDamage);
            }
        }

        // Destroy the zombie after explosion
        NetworkServer.Destroy(gameObject);
    }



    
}
