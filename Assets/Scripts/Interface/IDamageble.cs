using UnityEngine;
using Mirror;

public interface IDamageble
{
    void Damage(float amount, NetworkIdentity networkIdentity = null, NetworkIdentity shooterident = null);
    void Die();
    float MaxHealth {get; set;}
    float CurrentHealth {get; set;}
}
