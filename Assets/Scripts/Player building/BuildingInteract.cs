using Mirror;
using UnityEngine;
public class BuildingInteract : NetworkBehaviour
{
    
    [Server]
    public void DestroyWall()
    {
        // Call this on the server, it will be destroyed on all clients
        NetworkServer.Destroy(gameObject);
    }
}