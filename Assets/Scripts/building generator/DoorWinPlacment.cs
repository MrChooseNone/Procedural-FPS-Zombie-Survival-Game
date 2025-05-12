using System.Collections.Generic;

using UnityEngine;
using Mirror;

public class DoorWinPlacment : NetworkBehaviour
{
    public GameObject doorPrefab;
    public GameObject windowPrefab;
    public float doorHeight = 2f;
    public float windowHeight = 3f;
    public float windowSpacing = 10f;
    private BuildingMaker buildingmaker;
    private GameObject[] buildings;
    public bool isSpawned = false;
    public float nodeAvoidanceThreshold = 2f; // Minimum distance from nodes for placing windows
    public float verticalSpacing = 10f;
    public bool isDoor = false;
    public Terrain terrain;

    void Start()
    {
        buildingmaker = FindAnyObjectByType<BuildingMaker>();
        // if(Terrain.activeTerrain != null){
        //     terrain = Terrain.activeTerrain;
        // }
    }

    void Update()
    {
        if (buildingmaker != null && buildingmaker.isFinished == true && isSpawned == false)
        {
            isSpawned = true;
            SpawnDoorsAndWindows();
        }
    }

    void SpawnDoorsAndWindows()
    {
        Debug.Log("Spawning doors and windows...");
        buildings = GameObject.FindGameObjectsWithTag("Building");

        foreach (GameObject building in buildings)
        {
            Debug.Log($"Spawning for building: {building.name}");
            MeshFilter meshFilter = building.GetComponent<MeshFilter>();
            if (meshFilter == null) continue;

            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Vector3 meshCenter = building.transform.TransformPoint(mesh.bounds.center);
            HashSet<Vector3> placedPositions = new HashSet<Vector3>();
            HashSet<Vector3> buildingNodes = GetBuildingNodes(vertices, building); // Extract nodes
            float minTriangleEdgeLength = 1f;

            isDoor = false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = building.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v2 = building.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v3 = building.transform.TransformPoint(vertices[triangles[i + 2]]);

                if (Vector3.Distance(v1, v2) < minTriangleEdgeLength || Vector3.Distance(v2, v3) < minTriangleEdgeLength || Vector3.Distance(v3, v1) < minTriangleEdgeLength)
                    continue;

                Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

                if (Mathf.Abs(normal.y) < 0.1f)
                {
                    Vector3 wallMidpoint = (v1 + v2 + v3) / 3;
                    Vector3 toCenter = (wallMidpoint - meshCenter).normalized;
                    if (Vector3.Dot(normal, toCenter) <= 0)
                        continue;

                    if (!isDoor)
                    {
                        if (!IsPositionNearExisting(placedPositions, wallMidpoint))
                        {
                            Vector3 temp = wallMidpoint;
                            //adjust to terrain
                            temp.y = terrain.SampleHeight(temp);
                            
                            Debug.Log("Spawning door...");
                            
                            GameObject door = Instantiate(doorPrefab, temp, Quaternion.LookRotation(normal));
                            NetworkServer.Spawn(door);
                            //door.transform.SetParent(building.transform);
                            placedPositions.Add(temp);
                            isDoor = true;
                        }
                    }
                    else
                    {
                        PlaceWindowsAlongEdge(v1, v2, normal, placedPositions, buildingNodes, building);
                        PlaceWindowsAlongEdge(v2, v3, normal, placedPositions, buildingNodes, building);
                        PlaceWindowsAlongEdge(v3, v1, normal, placedPositions, buildingNodes, building);
                    }
                }
            }
        }
    }

    HashSet<Vector3> GetBuildingNodes(Vector3[] vertices, GameObject building)
    {
        HashSet<Vector3> nodes = new HashSet<Vector3>();
        foreach (var vertex in vertices)
        {
            nodes.Add(building.transform.TransformPoint(vertex));
        }
        return nodes;
    }

    void PlaceWindowsAlongEdge(Vector3 start, Vector3 end, Vector3 normal, HashSet<Vector3> placedPositions, HashSet<Vector3> buildingNodes, GameObject building)
    {
        float wallWidth = Vector3.Distance(start, end);
        int numWindows = Mathf.FloorToInt(wallWidth / windowSpacing);
        float buildHeight = GetBuildingHeight(building);
        int numLevels = (int)Mathf.Ceil(buildHeight/verticalSpacing);

        for (int j = 0; j < numWindows; j++)
        {
            Vector3 windowPosition = Vector3.Lerp(start, end, (j + 0.5f) / numWindows);
            windowPosition.y = windowHeight + terrain.SampleHeight(windowPosition);

            if (!IsPositionNearExisting(placedPositions, windowPosition) && !IsNearBuildingNode(buildingNodes, windowPosition))
            {
                Vector3 levelPosition = windowPosition;
                //adjust to terrain
                // levelPosition.y += terrain.SampleHeight(levelPosition);
                for(int k = 0; k < numLevels; k++){

                    Debug.Log("number of floors:" + numLevels);
                    GameObject window = Instantiate(windowPrefab, levelPosition, Quaternion.LookRotation(normal));
                    NetworkServer.Spawn(window);
                    placedPositions.Add(levelPosition);
                    levelPosition += new Vector3(0, 8, 0);
                }
            }
        }
    }

    bool IsNearBuildingNode(HashSet<Vector3> buildingNodes, Vector3 position)
    {
        foreach (var node in buildingNodes)
        {
            if (Vector3.Distance(node, position) < nodeAvoidanceThreshold)
                return true;
        }
        return false;
    }

    bool IsPositionNearExisting(HashSet<Vector3> placedPositions, Vector3 position, float threshold = 1f)
    {
        foreach (var placedPosition in placedPositions)
        {
            if (Vector3.Distance(placedPosition, position) < threshold)
                return true;
        }
        return false;
    }
 float GetBuildingHeight(GameObject building)
    {
        MeshFilter meshFilter = building.GetComponent<MeshFilter>();
        if (meshFilter == null) return 0f;

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 vertex in vertices)
        {
            // Transform the local vertex position to world space
            Vector3 worldVertex = building.transform.TransformPoint(vertex);

            // Update min and max Y values
            if (worldVertex.y < minY) minY = worldVertex.y;
            if (worldVertex.y > maxY) maxY = worldVertex.y;
        }
        float heightAboveTerrain = maxY - terrain.SampleHeight(building.transform.position);
        



        // Return the height (difference between max and min Y)
        return heightAboveTerrain;
    }

}



