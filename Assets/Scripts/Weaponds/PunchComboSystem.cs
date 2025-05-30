using System.Collections;
using UnityEngine;
using Mirror;

public class PunchComboSystem : NetworkBehaviour
{
    public Animator animator;
    public Animator bodyAnimator;
    private int comboStep = 0;
    private float lastPunchTime;
    public float comboResetTime = 1.0f;  
    public float punchRange = 2.0f;  
    public float punchDamage = 10;  
    public LayerMask enemyLayer;  
    public WeaponPickupController weaponPickupController;
    public Transform rightHand;
    public Transform leftHand;
    public bool canPunch = true;
    public float punchDelay;
    public GameObject hitEffectPrefab;
    
    public float sphereRadius = 0.5f; 

    private readonly string[] comboAnimations = { "Punch1", "Punch2", "Punch3" };

    void Update()
    {
        if (!isLocalPlayer) return;  

        if (Input.GetMouseButtonDown(0) && !weaponPickupController.hasGun && canPunch) 
        {
            CmdPerformPunch();
        }
    }

    [Command]
    void CmdPerformPunch()
    {
        RpcPlayPunchAnimation(comboStep);
        // DetectHit();
        comboStep = (comboStep + 1) % comboAnimations.Length;
        lastPunchTime = Time.time;
    }

    [ClientRpc]
    void RpcPlayPunchAnimation(int step)
    {
        
        canPunch = false;
        animator.CrossFade(comboAnimations[step], 0.1f, 0);
        bodyAnimator.CrossFade(comboAnimations[step] + "Hands", 0.1f, 1);
        StartCoroutine(PunchColdown());
    }

    public void DetectHit()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        Vector3 punchPoint = Vector3.zero;
        string aniName;
        if(info.IsName("Punch2")  ){
            aniName = "HitLeft";
            punchPoint = leftHand.position; // Get the exact hand position
        } else if(info.IsName("Punch1")) {
            aniName = "HitRight";
            punchPoint = rightHand.position; // Get the exact hand position
        }else {
            aniName = "BigHit";
            punchPoint = rightHand.position; // Get the exact hand position
        }

        // Debugging - Draw the sphere at the punch impact location
        Debug.DrawRay(punchPoint, Vector3.up * sphereRadius, Color.red, 2f); // Small line to mark impact
        Debug.DrawRay(punchPoint, Vector3.down * sphereRadius, Color.red, 2f); // Small line to mark impact
        Debug.DrawRay(punchPoint, Vector3.right * sphereRadius, Color.red, 2f); // Small line to mark impact
        Debug.DrawRay(punchPoint, Vector3.left * sphereRadius, Color.red, 2f); // Small line to mark impact
        Debug.DrawRay(punchPoint, Vector3.forward * sphereRadius, Color.red, 2f); // Small line to mark impact
        Debug.DrawRay(punchPoint, Vector3.back * sphereRadius, Color.red, 2f); // Small line to mark impact
        
        bool hit;
        // Detect enemies in the punch area
        Collider[] hitColliders = Physics.OverlapSphere(punchPoint, sphereRadius, enemyLayer);

        hit = false;
        foreach (Collider col in hitColliders)
        {
            IDamageble damageable = null;
            ZombieAI enemy = null;
            // damageable = col.GetComponent<IDamageble>();
            // if(damageable == null){
                damageable = col.GetComponentInParent<IDamageble>();
                enemy = col.GetComponentInParent<ZombieAI>();
                Debug.Log("damagabel" + damageable);
            //}
            if (damageable != null)
            {
                if(col.TryGetComponent(out NetworkIdentity networkIdentity) && enemy == null){
                    Debug.Log("network" + networkIdentity);
                    if(networkIdentity != netIdentity){

                        damageable.Damage(punchDamage, networkIdentity);
                        Debug.Log($"Hit {col.name} and applied {punchDamage} damage.");
                        Instantiate(hitEffectPrefab, col.transform.position, col.transform.rotation);
                        hit = true;
                    }
                }else {

                    damageable.Damage(punchDamage);
                    Debug.Log($"Hit {col.name} and applied {punchDamage} damage.");
                    Instantiate(hitEffectPrefab, col.transform.position, col.transform.rotation);
                    hit = true;
                }
                Vector3 hitDirection = (col.transform.position - transform.position).normalized;
                if(enemy != null){

                    StartCoroutine(enemy.ApplyKnockback(hitDirection));
                    CmdChangeZombieAnimation(aniName, enemy.netIdentity);
                }

                if(hit == true){
                    return;
                }
            }
        }
    
    }
    
    void LateUpdate()
    {
        if (Time.time - lastPunchTime > comboResetTime)
        {
            comboStep = 0;
        }
    }
    [Command(requiresAuthority = false)]
    void CmdChangeZombieAnimation(string newAni, NetworkIdentity identity) {
        if (NetworkServer.active) {
            if (identity != null)
            {
                ZombieAI enemy = identity.GetComponent<ZombieAI>();
                if (enemy != null) {
                    enemy.ChangeAnimation(newAni);
                } 
            } 
        }
    }   

    IEnumerator PunchColdown(){
        yield return new WaitForSeconds(punchDelay);
        canPunch = true;
    }

    
}
