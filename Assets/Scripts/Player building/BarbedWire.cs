using UnityEngine;
using System.Collections.Generic;

public class BarbedWire : MonoBehaviour
{
    public float slowAmount = 0.5f;        // 50% speed reduction
    public float damagePerSecond = 10f;    // Damage applied per second

    private Dictionary<GameObject, float> affectedZombies = new Dictionary<GameObject, float>();

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            ZombieAI zombie = other.GetComponent<ZombieAI>();
            if (zombie != null && !affectedZombies.ContainsKey(other.gameObject))
            {
                zombie.ModifySpeed(slowAmount);  // Apply slow
                affectedZombies.Add(other.gameObject, 0f); // Start damage timer
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            ZombieAI zombie = other.GetComponent<ZombieAI>();
            if (zombie != null && affectedZombies.ContainsKey(other.gameObject))
            {
                zombie.RestoreSpeed();  // Reset to normal speed
                affectedZombies.Remove(other.gameObject);
            }
        }
    }

    void Update()
    {
        List<GameObject> zombiesToRemove = new List<GameObject>();

        foreach (var entry in affectedZombies)
        {
            GameObject zombieObj = entry.Key;
            ZombieAI zombie = zombieObj.GetComponent<ZombieAI>();

            if (zombie == null) continue;

            // Apply damage over time
            float lastTime = affectedZombies[zombieObj];
            float newTime = lastTime + Time.deltaTime;
            affectedZombies[zombieObj] = newTime;

            if (newTime >= 1f)
            {
                zombie.Damage(damagePerSecond);
                affectedZombies[zombieObj] = 0f;
            }
        }
    }
}

