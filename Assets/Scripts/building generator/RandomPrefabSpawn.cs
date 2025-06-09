using UnityEngine;
using System.Collections;
using Mirror;

public class RandomPrefabSpawner : NetworkBehaviour
{
    public GameObject[] debrisPrefabs;

    public int spawnCount = 100;

    public float rayHeight = 50f;
    public LayerMask groundLayerMask;
    public bool isDone = false;

    public Terrain terrain;
    [Server]
    void Start()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogError("DebrisSpawner: no debrisPrefabs assigned.");
            return;
        }

        if (terrain == null)
        {
            Debug.LogError("DebrisSpawner: please assign a Terrain reference.");
            return;
        }

        StartCoroutine(SpawnDebrisCoroutine());
    }

    /// <summary>
    /// Coroutine that spawns `spawnCount` debris prefabs across the terrain.
    /// </summary>
    [Server]
    private IEnumerator SpawnDebrisCoroutine()
    {
        yield return new WaitForSeconds(10f);
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float minX = terrainPos.x;
        float maxX = terrainPos.x + terrainSize.x;
        float minZ = terrainPos.z;
        float maxZ = terrainPos.z + terrainSize.z;


        for (int i = 0; i < spawnCount; i++)
        {

            float randomX = Random.Range(minX, maxX);
            float randomZ = Random.Range(minZ, maxZ);


            float rayOriginY = terrainPos.y + terrainSize.y + rayHeight;
            Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);


            Ray ray = new Ray(rayOrigin, Vector3.down);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, terrainSize.y + rayHeight * 2f, groundLayerMask))
            {

                Vector3 spawnPoint = hitInfo.point;


                GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];


                Quaternion randomYaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);


                GameObject spawned = Instantiate(prefab, spawnPoint, randomYaw);
                spawned.tag = "Prop";
                NetworkServer.Spawn(spawned);

                // Optionally, you can give each debris item a tiny random tilt:
                float tiltX = Random.Range(-5f, +5f);
                float tiltZ = Random.Range(-5f, +5f);
                spawned.transform.Rotate(new Vector3(tiltX, 0f, tiltZ), Space.Self);

                // If i want to parent all debris under this spawner:
                // spawned.transform.parent = this.transform;
            }
            else
            {
                // Ray did not hit the ground 

                // For simplicity, we just skip and move on.
            }


            yield return null;
        }
        isDone = true;
        Debug.Log("done spawning");
    }
}
