
using Mirror;
using UnityEngine;
public class WallHealth : NetworkBehaviour
{
    public float health = 100f;
    [Server]
    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            DestroyWall();
        }
    }
    [Server]
    private void DestroyWall()
    {
        // Call this on the server, it will be destroyed on all clients
        NetworkServer.Destroy(gameObject);

        
    }
}
