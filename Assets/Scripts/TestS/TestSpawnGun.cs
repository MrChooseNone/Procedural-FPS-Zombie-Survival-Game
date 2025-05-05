using UnityEngine;
using Mirror;

public class TestSpawnGun : NetworkBehaviour
{
    public GameObject gunPrefab;
    public GameObject arPrefab;

    public GameObject ItemPrefab1;
    public GameObject ItemPrefab2;
    public GameObject ItemPrefab3;
    public Vector3 position;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Server]
    void Start()
    {
        GameObject gun1 = Instantiate(gunPrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(gun1);
        GameObject gun2 = Instantiate(gunPrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(gun2);
        
        GameObject gun3 = Instantiate(arPrefab, transform.position, transform.rotation);
        Debug.Log("spawned ar");
        NetworkServer.Spawn(gun3);
        GameObject gun4 = Instantiate(arPrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(gun4);
        GameObject gun5 = Instantiate(arPrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(gun5);
        GameObject gun6 = Instantiate(ItemPrefab1, transform.position, transform.rotation);
        NetworkServer.Spawn(gun6);
        GameObject gun7 = Instantiate(ItemPrefab2, transform.position, transform.rotation);
        NetworkServer.Spawn(gun7);
        GameObject gun8 = Instantiate(ItemPrefab3, transform.position, transform.rotation);
        NetworkServer.Spawn(gun8);

        
        GameObject gun9 = Instantiate(ItemPrefab1, transform.position, transform.rotation);
        NetworkServer.Spawn(gun9);
        GameObject gun10 = Instantiate(ItemPrefab1, transform.position, transform.rotation);
        NetworkServer.Spawn(gun10);
        GameObject gun11 = Instantiate(ItemPrefab1, transform.position, transform.rotation);
        NetworkServer.Spawn(gun11);
        for(int i = 0; i < 10; i++){

            GameObject gun12 = Instantiate(ItemPrefab1, transform.position, transform.rotation);
            NetworkServer.Spawn(gun12);
        }
        

        
        
        
        

    }

    

    
}
