using UnityEngine;

public class ChaseState : EnemyState
{
    private float soundTime;

    public ChaseState(ZombieAI enemy, EnemySTateMachine enemySTateMachine) : base(enemy, enemySTateMachine)
    {
    }

    public override void AnimationTriggerEvent(ZombieAI.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }
    public override void EnterState()
    {
        base.EnterState();
        AlertNearbyZombies();
        enemy.ChangeAnimation("Zombie run");
        Debug.Log("entered chase state");
        enemy.audioSource.pitch = Random.Range(0.9f, 1.1f);
        enemy.audioSource.volume = Random.Range(0.9f, 1.1f);
        enemy.audioSource.PlayOneShot(enemy.chaseSounds[Random.Range(0, enemy.chaseSounds.Length)]);
        soundTime = Random.Range(2f, 5f); // Initial wait before first move
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        soundTime -= Time.deltaTime;
        if (soundTime <= 0)
        {
            enemy.audioSource.pitch = Random.Range(0.9f, 1.1f);
            enemy.audioSource.volume = Random.Range(0.9f, 1.1f);
            enemy.audioSource.PlayOneShot(enemy.chaseSounds[Random.Range(0, enemy.chaseSounds.Length)]);
            soundTime = Random.Range(2f, 5f); // Initial wait before first move
        }
        if (!enemy.isFollowing)
        {
            enemy.STateMachine.ChangeState(enemy.IdleState);
        }
        else if (enemy.isAttacking)
        {
            enemy.STateMachine.ChangeState(enemy.attackState);
        }
        float speedMultiplier = Mathf.Clamp01(1 - (enemy.distanceToPlayer / enemy.perceptionRange)); 
        enemy.navAgent.speed = Mathf.Lerp(2f, enemy.followSpeed, speedMultiplier);
        
        if(enemy != null && enemy.navAgent != null && enemy.closestPlayer != null && enemy.navGen.isNavMesh && enemy.navAgent.isActiveAndEnabled){

            enemy.navAgent.SetDestination(enemy.closestPlayer.transform.position);
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public void AlertNearbyZombies()
    {
        Collider[] zombies = Physics.OverlapSphere(enemy.transform.position, 10f);
        foreach (Collider col in zombies)
        {
            ZombieAI otherZombie = col.GetComponentInParent<ZombieAI>();
            if (otherZombie != null && !otherZombie.isFollowing)
            {
                otherZombie.StartFollowingPlayer();
            }
        }
    }

}