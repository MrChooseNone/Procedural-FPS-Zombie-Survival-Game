using UnityEngine;
using System.Collections.Generic;

public class InteriorZombieSpawner : MonoBehaviour
{
    [Tooltip("List of different zombie prefabs")]
    public List<GameObject> zombiePrefabs;

    [Tooltip("All potential spawn points for zombies")]
    public Transform[] spawnPoints;

    [Tooltip("Minimum number of zombies to spawn")]
    public int minZombies = 3;

    [Tooltip("Maximum number of zombies to spawn")]
    public int maxZombies = 10;

    private void Start()
    {
        SpawnZombies();
    }
    

    void SpawnZombies()
    {
        if (zombiePrefabs == null || zombiePrefabs.Count == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Missing zombie prefabs or spawn points.");
            return;
        }

        int zombieCount = Random.Range(minZombies, maxZombies + 1);
        Debug.Log($"Spawning {zombieCount} zombies.");

        List<Transform> shuffledPoints = new(spawnPoints);
        ShuffleList(shuffledPoints);

        for (int i = 0; i < zombieCount; i++)
        {
            Transform spawnPoint = shuffledPoints[i % shuffledPoints.Count];
            GameObject randomZombie = zombiePrefabs[Random.Range(0, zombiePrefabs.Count)];
            Instantiate(randomZombie, spawnPoint.position, spawnPoint.rotation);
        }
    }

    void ShuffleList(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (list[i], list[randIndex]) = (list[randIndex], list[i]);
        }
    }
}

