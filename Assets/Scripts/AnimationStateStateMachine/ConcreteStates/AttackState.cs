using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class AttackState : EnemyState
{
    private float lastAttackTime;
    public float preChargeCooldown = 2f;
    public float chargeForce = 20f;
    public float postChargeCooldown = 3f;
    public float chargeDistance = 10f;
    public float attackRange = 1.5f;
    public int damage = 10;

    private bool canCharge = true;
    private bool isCharging = false;

    public AttackState(ZombieAI enemy, EnemySTateMachine enemySTateMachine) : base(enemy, enemySTateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Entered Attack State");
        lastAttackTime = 0f; // Reset attack cooldown on entry
        enemy.navAgent.isStopped = true; // Stop movement while attacking
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.navAgent.isStopped = false; // Resume movement after exiting attack state
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        // Check if the enemy should return to chase state
        if (!enemy.isAttacking && canCharge)
        {
            enemy.STateMachine.ChangeState(enemy.chaseState);
            return;
        }

        // Check if enough time has passed since last attack
        if (Time.time >= lastAttackTime + enemy.attackCooldown)
        {
           enemy.StartCoroutine(TryAttack());
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    
    IEnumerator TryAttack()
    {
        if(enemy.isNormalZombie || enemy.isBloatedZombie){

            IDamageble damageable = enemy.closestPlayer.GetComponent<IDamageble>();
            NetworkIdentity networkIdentity = enemy.closestPlayer.GetComponent<NetworkIdentity>();
            Debug.Log("tried damage");
            if (damageable != null && networkIdentity != null)
            {
                damageable.Damage(enemy.damageAmount, networkIdentity);
                lastAttackTime = Time.time; // Use Time.time for accurate tracking
                Debug.Log("Zombie attacked the player!" + damageable);
            }
        } else if (enemy.isLanchZombie)
        {
            canCharge = false;
            Debug.Log("launch zombie attacked");

            enemy.navAgent.isStopped = true;

            // Lock the player's position at the start of the charge
            Vector3 lockedPlayerPosition = enemy.closestPlayer.transform.position;
            lastAttackTime = Time.time; // Use Time.time for accurate tracking

            yield return new WaitForSeconds(preChargeCooldown);
            enemy.ChangeAnimation("Lanch");

            isCharging = true;

            Vector3 chargeDirection = (lockedPlayerPosition - enemy.transform.position).normalized;
            Vector3 startPosition = enemy.transform.position;
            Vector3 targetPosition = startPosition + chargeDirection * chargeDistance;

            float elapsedTime = 0f;
            float duration = 0.3f;
            enemy.navAgent.enabled = false;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                enemy.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Snap to target position to clean up precision
            enemy.transform.position = targetPosition;
            enemy.navAgent.Warp(targetPosition);

            enemy.navAgent.enabled = true;
            isCharging = false;
            enemy.navAgent.isStopped = false;
            enemy.ChangeAnimation("Zombie idle");
            yield return new WaitForSeconds(postChargeCooldown);
            canCharge = true;
        }else if (enemy.isExploadZombie)
        {
            Debug.Log("explode zombie attacked");
            // Optional pre-explosion delay
            enemy.navAgent.isStopped = true;
            yield return new WaitForSeconds(0.5f);

            enemy.Explode();
        } else if(enemy.isScreamerZombie){
            lastAttackTime = Time.time; // Use Time.time for accurate tracking
            enemy.ChangeAnimation("scream");
            AlertNearbyZombies();
            yield return new WaitForSeconds(0.5f);
            enemy.followTime = 2f;
            enemy.StopFollowingPlayer();
            enemy.ChangeAnimation("Zombie run");
            MoveRandomPoint();
            yield return new WaitForSeconds(2f);

        }
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    public void AlertNearbyZombies()
    {
        Collider[] zombies = Physics.OverlapSphere(enemy.transform.position, 100f);
        foreach (Collider col in zombies)
        {
            ZombieAI otherZombie = col.GetComponentInParent<ZombieAI>();
            if (otherZombie != null && !otherZombie.isFollowing)
            {
                Debug.Log("alert xombie" + otherZombie);
                otherZombie.closestPlayer = enemy.closestPlayer;
                otherZombie.followTime = 20f;
                otherZombie.StartFollowingPlayer();
            }
        }
    }

    public Vector3 MoveRandomPoint()
    {
        Vector3 randomPoint = enemy.transform.position + (Random.insideUnitSphere * enemy.RandomMovementRange);
        randomPoint.y = 0; // Ensure it's at ground level

        // Ensure the point is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, enemy.RandomMovementRange, NavMesh.AllAreas))
        {
            Debug.Log("move xombie" + hit.position);
            enemy.navAgent.SetDestination(hit.position);
            return hit.position;
        }

        // Fallback to the current position if no valid point is found
        return enemy.transform.position;
    }

}