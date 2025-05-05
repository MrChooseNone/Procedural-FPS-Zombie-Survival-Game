
using UnityEngine;

public class EnemyState
{
    protected ZombieAI enemy;
    protected EnemySTateMachine enemyStateMachine;

    public EnemyState(ZombieAI enemy, EnemySTateMachine enemySTateMachine){

        this.enemy = enemy;
        this.enemyStateMachine = enemySTateMachine;
    }

    public virtual void EnterState(){}
    public virtual void ExitState(){}
    public virtual void FrameUpdate(){}
    public virtual void PhysicsUpdate(){}
    public virtual void AnimationTriggerEvent(ZombieAI.AnimationTriggerType triggerType){}
    
}
