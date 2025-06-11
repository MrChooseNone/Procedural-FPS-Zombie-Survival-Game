using UnityEngine;

public class ChaseState : EnemyState
{
    

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
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if(!enemy.isFollowing){
            enemy.STateMachine.ChangeState(enemy.IdleState);
        } else if(enemy.isAttacking){
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