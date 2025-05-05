using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;


public class NavMeshGenerator : MonoBehaviour
{
    public bool isNavMesh = false;
    public GameObject mapContainer;  // Container for your map or environment
    private NavMeshSurface navMeshSurface;

    void Start()
    {
        // Find or add the NavMeshSurface component to the map container
        navMeshSurface = mapContainer.GetComponent<NavMeshSurface>();

        if (navMeshSurface == null)
        {
            // If no NavMeshSurface component exists, add one
            navMeshSurface = mapContainer.AddComponent<NavMeshSurface>();
        }

        StartCoroutine(GenerateNavMesh());
    }

    IEnumerator GenerateNavMesh()
    {
        yield return new WaitForSeconds(20);
        // Rebuild the NavMesh based on the environment's current state
        navMeshSurface.BuildNavMesh();
        isNavMesh = true;
    }
}
