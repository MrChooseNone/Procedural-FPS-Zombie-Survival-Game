using UnityEngine;
using Mirror;

public interface IDamageble
{
    void Damage(float amount, NetworkIdentity networkIdentity = null );
    void Die();
    float MaxHealth {get; set;}
    float CurrentHealth {get; set;}
}
