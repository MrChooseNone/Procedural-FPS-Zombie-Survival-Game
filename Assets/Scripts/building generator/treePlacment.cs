using UnityEngine;
using MapMaker;
using System.Collections;
using System.Collections.Generic;



class TreePlacement : InfrastructureBehaviour
{
    public Material TreeMaterial;
    public GameObject TreePrefab;
    public bool isTree = true;
    public LayerMask obsticles;
    public Transform ParentObject;
    public int amount;
    [System.Serializable]
    public class PrefabProbability
    {
        public GameObject prefab;  // The prefab to spawn
        public float probability;  // Probability of spawning the prefab
    }
    public PrefabProbability[] prefabProbabilities;


    //protected new MapReader map;

    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices)
    {
        // Implementation for creating objects if necessary
    }
    void Update(){
        Vector3 testPosition = new Vector3(0, 0, 0); // Replace with your target position
        Debug.DrawRay(testPosition + Vector3.up * 10f, Vector3.down * 20f, Color.red, 0.1f);
    }

    IEnumerator Start()
    {
        // Wait until the map is ready
        while (!map.IsReady)
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(10);
        

        foreach (var way in map.ways.FindAll((w) => w.IsTree && w.NodeIDs.Count > 1))
        {

            PlaceTrees(way);

            yield return null;
        }
    }

    private void PlaceTrees(OsmWay way)
{
    
    // Get the local origin to work with relative positions
    Vector3 localOrigin = GetCentre(way);

    // Get the list of node positions relative to the local origin
    List<Vector3> polygonPoints = new List<Vector3>();
    foreach (var nodeId in way.NodeIDs)
    {
        OsmNode node = map.nodes[nodeId];
        polygonPoints.Add(node - localOrigin);
    }

    // Generate random points inside the polygon
    List<Vector3> treePositions = GenerateRandomPointsInsidePolygon(polygonPoints, amount); // Adjust count as needed
    
    // Instantiate trees at the generated positions
    foreach (var position in treePositions)
    {
        Vector3 pos = position + (localOrigin - map.bounds.Centre);
        //adjust to terrain
        pos.y = terrain.SampleHeight(pos);
        
        if (IsValidTreePosition(pos)){

            SpawnRandomPrefab(pos, ParentObject);
            
        }
    }
    
}

private List<Vector3> GenerateRandomPointsInsidePolygon(List<Vector3> polygon, int count)
{
    List<Vector3> points = new List<Vector3>();
    Bounds bounds = GetPolygonBounds(polygon);

    while (points.Count < count)
    {
        // Generate a random point within the bounds
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        Vector3 point = new Vector3(x, 0, z);

        // Check if the point is inside the polygon
        
            points.Add(point);
        
    }

    return points;
}

private Bounds GetPolygonBounds(List<Vector3> polygon)
{
    Vector3 min = polygon[0];
    Vector3 max = polygon[0];

    foreach (var point in polygon)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }

    return new Bounds((min + max) / 2, max - min);
}

private bool IsValidTreePosition(Vector3 position)
{

    // Perform an overlap check to ensure there's enough space for the tree
    if (Physics.CheckSphere(position, 2f, obsticles))
    {
        return false; // Position overlaps with an obstacle
    }

    return true; // Valid position
}

public void SpawnRandomPrefab(Vector3 pos, Transform parent)
    {
        
        float randomValue = Random.Range(0f, 1f);  // Random value between 0 and 1
        float cumulativeProbability = 0f;

        foreach (var item in prefabProbabilities)
        {
            cumulativeProbability += item.probability;

            if (randomValue <= cumulativeProbability)
            {
                Instantiate(item.prefab, pos, Quaternion.identity, parent);  // Spawn prefab at current position
                break;
            }
        }
    }

}
