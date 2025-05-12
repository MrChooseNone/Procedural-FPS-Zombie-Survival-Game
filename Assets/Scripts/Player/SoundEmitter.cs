using UnityEngine;
public class SoundEmitter : MonoBehaviour
{
    public float SoundRange = 10f;
    public float zombieFollowTime = 10f;

    public void EmittSound(float range){
        Collider[] zombies = Physics.OverlapSphere(transform.position, range);
        foreach(Collider col in zombies){
            ZombieAI zombie = col.GetComponentInParent<ZombieAI>();
            if (zombie != null){
                zombie.followTime = zombieFollowTime;
                zombie.closestPlayer = gameObject;
                zombie.StartFollowingPlayer();

            }
        }
    }
}