using UnityEngine;

public class ZombieDeath : MonoBehaviour
{
    public delegate void DeathDelegate();
    public event DeathDelegate OnDeath;

    public void Die()
    {
        OnDeath?.Invoke();
        //Destroy(gameObject);
    }
}
