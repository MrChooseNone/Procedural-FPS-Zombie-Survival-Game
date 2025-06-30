using UnityEngine;
using UnityEngine.AI;

public class IdleState : EnemyState
{
    private float waitTime;
    private float soundTime;
    private Vector3 _targetPos;
    private bool isMoving = false; // Track if zombie is currently moving
    public float wiggleRoom = 0.1f;
    
    public IdleState(ZombieAI enemy, EnemySTateMachine enemySTateMachine) : base(enemy, enemySTateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Entered Idle State");
        enemy.ChangeAnimation("Zombie idle");
        waitTime = Random.Range(2f, 5f); // Initial wait before first move
        _targetPos = enemy.transform.position; // Stay in place at first
        isMoving = false;
        soundTime = Random.Range(2f, 5f); // Initial wait before first move
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
    

        if (enemy.isFollowing)
        {
            enemy.STateMachine.ChangeState(enemy.chaseState);
            return;
        }
        waitTime -= Time.deltaTime;
        soundTime -= Time.deltaTime;
        if (waitTime <= 0)
        {
            isMoving = false;

        }
        if (soundTime <= 0)
        {
            enemy.audioSource.pitch = Random.Range(0.9f, 1.1f);
            enemy.audioSource.volume = Random.Range(0.9f, 1.1f);
            enemy.audioSource.PlayOneShot(enemy.idleSounds[Random.Range(0, enemy.idleSounds.Length)]);
            soundTime = Random.Range(2f, 5f); // Initial wait before first move
        }
        

        if (Vector3.Distance(enemy.transform.position, _targetPos) < wiggleRoom)
        {
            enemy.ChangeAnimation("Zombie idle");
        }


        // Only set a new destination if we're not already moving
        if (!isMoving )
        {
            waitTime = Random.Range(1f, 7f); // Random pause before next move
            _targetPos = MoveRandomPoint();
            isMoving = true;
            enemy.ChangeAnimation("Zombie run");
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public Vector3 MoveRandomPoint()
    {
        Vector3 randomPoint = enemy.transform.position + (Random.insideUnitSphere * enemy.RandomMovementRange);
        randomPoint.y = 0; // Ensure it's at ground level

        // Ensure the point is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, enemy.RandomMovementRange, NavMesh.AllAreas))
        {
            enemy.navAgent.SetDestination(hit.position);
            return hit.position;
        }

        // Fallback to the current position if no valid point is found
        return enemy.transform.position;
    }
}
