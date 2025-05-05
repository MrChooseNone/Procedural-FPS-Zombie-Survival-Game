using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.AI;
using System.Collections.Generic;

public class ZombieSpawner : NetworkBehaviour
{
    [System.Serializable]
    public class SpawnablePrefab
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float probability;
    }
     public SpawnablePrefab[] spawnablePrefabs;
    //public GameObject[] zombiePrefab; // The zombie prefab to spawn
    //public int numberOfZombies = 10; // Number of zombies to spawn
    // public Vector3 spawnAreaCenter; // Center of the spawn area
    // public Vector3 spawnAreaSize; // Size of the spawn area
    public Terrain terrain;
    public NavMeshGenerator navGen;
    private List<GameObject> currentZombies = new List<GameObject>();
    public float minDistanceFromPlayer = 10f;
    public int maxZombies = 10;
    public float respawnDelay = 5f;
    public float spawnRadius = 20f;
    public GameObject[] players;
    void Awake()
    {
        navGen = FindAnyObjectByType<NavMeshGenerator>();
    }
    [Server]
    IEnumerator Start()
    {
        yield return new WaitUntil(() => navGen.isNavMesh);
        StartCoroutine(SpawnLoop());
    }
    
    
// void SpawnZombies()
// {
//     for (int i = 0; i < numberOfZombies; i++)
//     {
//         // Generate a random position within the spawn area (XZ only)
//         Vector3 randomPosition = spawnAreaCenter + new Vector3(
//             Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
//             0f, // Set Y = 0 for now, we'll adjust it using terrain
//             Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
//         );

//         // Adjust Y to match terrain height + terrain base Y
//         float terrainHeight = terrain.SampleHeight(randomPosition) + terrain.GetPosition().y;
//         randomPosition.y = terrainHeight;

//         // Debugging: Log the final position with adjusted Y
//         Debug.Log("Final position after terrain height adjustment: " + randomPosition);

//         // Check if the random position is on the NavMesh
//         // NavMeshHit hit;
//         // if (NavMesh.SamplePosition(randomPosition, out hit, 1f, NavMesh.AllAreas))
//         // {
//         //     // If it is on the NavMesh, use the hit position
//         //     randomPosition = hit.position;
//         //     Debug.Log("Zombie spawn position adjusted to NavMesh: " + randomPosition);
//         // }
//         // else
//         // {
//         //     Debug.LogError("Spawn position not on NavMesh! Adjusting position...");
//         //     // If not on the NavMesh, you could choose to adjust or skip this spawn
//         //     continue; // Skip this spawn attempt and try again (or handle differently)
//         // }

//         // Spawn the zombie
//         GameObject zombie = Instantiate(zombiePrefab, randomPosition, Quaternion.identity);
//         NetworkServer.Spawn(zombie);

//         // // Optionally, you can add a NavMeshAgent to the zombie here
//         // NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
//         // if (agent != null)
//         // {
//         //     agent.Warp(randomPosition); // Warp the agent to the corrected spawn position
//         // }
//     }
// }



    // Optional: Visualize the spawn area in the editor
    // void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
 
    [Server]
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            players = GameObject.FindGameObjectsWithTag("Player");

            if (currentZombies.Count < maxZombies)
            {
                Vector3 spawnPos = GetValidSpawnPosition();
                GameObject randomZombie = GetRandomPrefab();
                GameObject zombie = Instantiate(randomZombie, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(zombie);

                currentZombies.Add(zombie);

                // Track removal when zombie dies
                ZombieDeath deathScript = zombie.GetComponent<ZombieDeath>();
                if (deathScript != null)
                {
                    deathScript.OnDeath += () =>
                    {
                        StartCoroutine(RespawnZombieDelayed());
                        currentZombies.Remove(zombie);
                    };
                }
            }
        }
    }

    Vector3 GetValidSpawnPosition()
    {
        Vector3 pos = Vector3.zero;
        bool validPos = false;

        // Keep trying until we find a valid position
        while (!validPos)
        {
            pos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0, // assuming it's a flat terrain; adjust if 3D
                Random.Range(-spawnRadius, spawnRadius)
            );
            float terrainHeight = terrain.SampleHeight(pos) + terrain.GetPosition().y;
            pos.y = terrainHeight;

            // Check distance from all players
            validPos = true;
            foreach (var player in players)
            {
                if (Vector3.Distance(player.transform.position, pos) < minDistanceFromPlayer)
                {
                    validPos = false;
                    break;  // Break early if we find a player too close
                }
            }
        }

        return pos;
    }

    IEnumerator RespawnZombieDelayed()
    {
        yield return new WaitForSeconds(respawnDelay);
    }

    // Horde Spawning Function (called externally)
    public void SpawnHorde(int hordeSize)
    {
        StartCoroutine(HordeSpawnRoutine(hordeSize));
    }

    IEnumerator HordeSpawnRoutine(int hordeSize)
    {
        for (int i = 0; i < hordeSize; i++)
        {
            Vector3 pos = GetValidSpawnPosition();
            GameObject randomZombie = GetRandomPrefab();
            GameObject zombie = Instantiate(randomZombie, pos, Quaternion.identity);
            NetworkServer.Spawn(zombie);
            currentZombies.Add(zombie);
            yield return new WaitForSeconds(0.2f); // staggered spawn
        }
    }

    private GameObject GetRandomPrefab()
    {
        float totalProbability = 0f;
        foreach (var item in spawnablePrefabs)
        {
            totalProbability += item.probability;
        }

        float randomPoint = Random.value * totalProbability;
        float cumulative = 0f;

        foreach (var item in spawnablePrefabs)
        {
            cumulative += item.probability;
            if (randomPoint <= cumulative)
            {
                return item.prefab;
            }
        }

        return null;
    }
}



