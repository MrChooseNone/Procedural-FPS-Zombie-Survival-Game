using UnityEngine;
using OsmSharp;
using OsmSharp.Streams;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;


public class OSMDataParser : MonoBehaviour
{
    public string osmFilePath = "path/to/your/osm.xml"; // Path to your OSM .xml file
    public float lat = 0;
    public float lon = 0;

    // Dictionary to store all nodes by their IDs for easy lookup
    private Dictionary<long, OsmSharp.Node> nodeDictionary = new Dictionary<long, OsmSharp.Node>();

    void Start()
    {
        if (File.Exists(osmFilePath))
        {
            ParseOSMData(osmFilePath);
        }
        else
        {
            Debug.LogError($"OSM file not found at path: {osmFilePath}");
        }
    }

    public void ParseOSMData(string filePath)
    {
        try
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                Debug.Log("Parsing OSM XML data...");

                // Parse the OSM XML data
                var source = new XmlOsmStreamSource(fileStream);

                // Process the OSM data
                foreach (var osmEntity in source)
                {
                    if (osmEntity is OsmSharp.Node node)
                    {
                        // Store nodes for later use
                        nodeDictionary[node.Id.Value] = node;
                    }
                    else if (osmEntity is OsmSharp.Way way && HasBuildingTag(way))
                    {
                        // Process building footprints
                        ProcessBuildingFootprint(way);
                    }
                }

                Debug.Log("OSM data parsing complete.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing OSM XML data: {ex.Message}");
        }
    }

    public bool HasBuildingTag(OsmSharp.Way way)
    {
        return way.Tags != null && way.Tags.ContainsKey("building");
    }

//     public void ProcessBuildingFootprint(OsmSharp.Way way)
//     {
//         Debug.Log($"Processing building ID {way.Id}...");

//         // Create a list to store the vertices of the building
//         var vertices = new List<Vector3>();

//         foreach (var nodeId in way.Nodes)
//         {
//             if (nodeDictionary.TryGetValue(nodeId, out OsmSharp.Node node))
//             {
//                 lat = (float)node.Latitude;
//                 lon = (float)node.Longitude;

//                 // Convert lat/lon to Unity world position
//                 Vector3 position = LatLonToUnityPosition(lat, lon);
//                 vertices.Add(position);
//             }
//         }

//         // Generate the building using the vertices
//         if (vertices.Count > 0)
//         {
//             GenerateBuilding(vertices);
//         }
//     }

//     public void GenerateBuilding(List<Vector3> vertices)
//     {
//         if (vertices.Count < 3)
//         {
//             Debug.LogWarning("Not enough vertices to form a building.");
//             return;
//         }

//         // Create a GameObject for the building
//         GameObject building = new GameObject("Building");

//         // Add components
//         var meshFilter = building.AddComponent<MeshFilter>();
//         var meshRenderer = building.AddComponent<MeshRenderer>();

//         // Generate the mesh
//         var buildingMesh = ExtrudePolygon(vertices.ToArray(), 10f); // Example: 10 units tall

//         // Assign the mesh
//         meshFilter.mesh = buildingMesh;

//         // Assign a material
//         meshRenderer.material = new Material(Shader.Find("Standard"))
//         {
//             color = Color.gray // Set default building color
//         };

//         Debug.Log($"Building created with {vertices.Count} vertices.");
//     }


public int[] TriangulatePolygon(Vector3[] vertices)
{
    List<int> triangles = new List<int>();
    List<int> indices = new List<int>();

    for (int i = 0; i < vertices.Length; i++)
        indices.Add(i);

    while (indices.Count > 3)
    {
        bool earFound = false;

        for (int i = 0; i < indices.Count; i++)
        {
            int prev = indices[(i - 1 + indices.Count) % indices.Count];
            int curr = indices[i];
            int next = indices[(i + 1) % indices.Count];

            if (IsEar(vertices, prev, curr, next, indices))
            {
                triangles.Add(prev);
                triangles.Add(curr);
                triangles.Add(next);

                indices.RemoveAt(i);
                earFound = true;
                break;
            }
        }

        if (!earFound)
        {
            Debug.LogError("Failed to triangulate polygon.");
            return new int[0];
        }
    }

    // Add the last remaining triangle
    triangles.Add(indices[0]);
    triangles.Add(indices[1]);
    triangles.Add(indices[2]);

    return triangles.ToArray();
}

private bool IsEar(Vector3[] vertices, int prev, int curr, int next, List<int> indices)
{
    Vector3 a = vertices[prev];
    Vector3 b = vertices[curr];
    Vector3 c = vertices[next];
    
    // Check if the points are collinear (i.e., they form a degenerate triangle)
    if (Vector3.Cross(b - a, c - a).magnitude < float.Epsilon)
    {
        // Skip this triangle if it's degenerate
        return false;
    }

    // Check if the triangle is convex
    if (Vector3.Cross(b - a, c - b).y <= 0)
        return false;

    // Ensure no other point is inside this triangle
    for (int i = 0; i < vertices.Length; i++)
    {
        if (!indices.Contains(i) || i == prev || i == curr || i == next)
            continue;

        if (PointInTriangle(vertices[i], a, b, c))
            return false;
    }

    return true;
}


private bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
{
    Vector3 v0 = c - a;
    Vector3 v1 = b - a;
    Vector3 v2 = p - a;

    float dot00 = Vector3.Dot(v0, v0);
    float dot01 = Vector3.Dot(v0, v1);
    float dot02 = Vector3.Dot(v0, v2);
    float dot11 = Vector3.Dot(v1, v1);
    float dot12 = Vector3.Dot(v1, v2);

    float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

    return (u >= 0) && (v >= 0) && (u + v < 1);
}

public Vector3 LatLonToUnityPosition(float lat, float lon)
{
    float x = lon * 0.1f; // Adjust scaling as needed for larger areas
    float z = lat * 0.1f; // Adjust scaling as needed for larger areas
    return new Vector3(x, 0, z); // Adjust Y if needed
}


// public Mesh ExtrudePolygon(Vector3[] baseVertices, float height)
// {
//     List<Vector3> vertices = new List<Vector3>();
//     List<int> triangles = new List<int>();

//     int baseVertexCount = baseVertices.Length;

//     // Add base vertices
//     vertices.AddRange(baseVertices);

//     // Add top vertices (extruded upwards by height)
//     for (int i = 0; i < baseVertexCount; i++)
//     {
//         vertices.Add(baseVertices[i] + Vector3.up * height);
//     }

//     // Create walls (connect base and top vertices)
//     for (int i = 0; i < baseVertexCount; i++)
//     {
//         int next = (i + 1) % baseVertexCount;

//         // First triangle of wall quad
//         triangles.Add(i);                      // Base vertex
//         triangles.Add(baseVertexCount + next); // Top next vertex
//         triangles.Add(baseVertexCount + i);    // Top current vertex

//         // Second triangle of wall quad
//         triangles.Add(i);                      // Base vertex
//         triangles.Add(next);                   // Base next vertex
//         triangles.Add(baseVertexCount + next); // Top next vertex
//     }

//     // Create triangles for the base (reversed winding order for the bottom face)
//     int[] baseTriangles = TriangulatePolygon(baseVertices);
//     triangles.AddRange(baseTriangles);

//     // Create triangles for the roof
//     Vector3[] roofVertices = new Vector3[baseVertexCount];
//     for (int i = 0; i < baseVertexCount; i++)
//     {
//         roofVertices[i] = baseVertices[i] + Vector3.up * height;
//     }
//     int[] roofTriangles = TriangulatePolygon(roofVertices);
//     for (int i = 0; i < roofTriangles.Length; i++)
//     {
//         // Add the roof triangles (with offset for roof vertices)
//         triangles.Add(roofTriangles[i] + baseVertexCount);
//     }

//     // Construct the mesh
//     Mesh mesh = new Mesh
//     {
//         vertices = vertices.ToArray(),
//         triangles = triangles.ToArray()
//     };
//     mesh.RecalculateNormals();
//     return mesh;
// }

public void ProcessBuildingFootprint(OsmSharp.Way way)
{
    Debug.Log($"Processing building ID {way.Id}...");

    var vertices = new List<Vector3>();

    foreach (var nodeId in way.Nodes)
    {
        if (nodeDictionary.TryGetValue(nodeId, out OsmSharp.Node node))
        {
            float lat = (float)node.Latitude;
            float lon = (float)node.Longitude;

            // Convert lat/lon to Unity world position
            Vector3 position = LatLonToUnityPosition(lat, lon);
            vertices.Add(position);
        }
    }

    if (vertices.Count > 2)
    {
        // Create a building GameObject
        GameObject building = new GameObject($"Building_{way.Id}");
        building.transform.position = Vector3.zero;

        // Generate walls and roof
        CreateWalls(building, vertices.ToArray(), 10f); // Example height: 10 units
        CreateRoof(building, vertices.ToArray(), 10f); // Example height: 10 units
    }
}

public void CreateWalls(GameObject building, Vector3[] vertices, float height)
{
    for (int i = 0; i < vertices.Length; i++)
    {
        int next = (i + 1) % vertices.Length;

        // Calculate wall center
        Vector3 start = vertices[i];
        Vector3 end = vertices[next];
        Vector3 center = (start + end) / 2;

        // Calculate wall dimensions
        float width = Vector3.Distance(start, end);
        float wallHeight = height;

        // Create a wall GameObject
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = $"Wall_{i}";
        wall.transform.parent = building.transform;

        // Set position and scale
        wall.transform.position = center + Vector3.up * (wallHeight / 2); // Center vertically
        wall.transform.localScale = new Vector3(width, wallHeight, 1);

        // Rotate wall to face the correct direction
        Vector3 direction = end - start;
        wall.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }
}

public void CreateRoof(GameObject building, Vector3[] vertices, float height)
{
    // Calculate roof center position
    Vector3 center = Vector3.zero;
    foreach (var vertex in vertices) center += vertex;
    center /= vertices.Length;

    // Create a roof GameObject
    GameObject roof = new GameObject("Roof");
    roof.transform.parent = building.transform;

    // Add mesh components
    var meshFilter = roof.AddComponent<MeshFilter>();
    var meshRenderer = roof.AddComponent<MeshRenderer>();

    // Create roof mesh
    Mesh roofMesh = new Mesh();
    roofMesh.vertices = vertices.Select(v => v + Vector3.up * height).ToArray();
    roofMesh.triangles = TriangulatePolygon(vertices);
    roofMesh.RecalculateNormals();
    roofMesh.RecalculateBounds();

    meshFilter.mesh = roofMesh;

    // Assign material
    meshRenderer.material = new Material(Shader.Find("Standard")) { color = Color.gray };
}




}











// using System.Collections.Generic;
// using System.IO;
// using System.Xml;
// using System.Threading;
// using UnityEngine;

// public class OSMBuildingMeshGenerator : MonoBehaviour
// {
//     public string filePath; // Path to the OSM XML file.
//     public float scale = 1.0f; // Scale factor for Unity coordinates.
//     public int maxRetries = 10; // Maximum number of retries to wait for file access.
//     public int retryDelayMs = 500; // Delay between retries in milliseconds.

//     private Dictionary<string, Vector3> nodeDictionary = new Dictionary<string, Vector3>();

//     void Start()
//     {
//         if (!string.IsNullOrEmpty(filePath))
//         {
//             string xmlData = ReadFileWhenReady(filePath);
//             if (xmlData != null)
//             {
//                 ParseOSM(xmlData);
//             }
//             else
//             {
//                 Debug.LogError("Failed to access the file within the retry limit: " + filePath);
//             }
//         }
//         else
//         {
//             Debug.LogError("File path is empty or invalid.");
//         }
//     }

//     string ReadFileWhenReady(string path)
//     {
//         int attempts = 0;

//         while (attempts < maxRetries)
//         {
//             Thread.Sleep(retryDelayMs);
//             try
//             {
//                 // Try to open and read the file
//                 using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
//                 {
//                     using (var reader = new StreamReader(stream))
//                     {
//                         return reader.ReadToEnd();
//                     }
//                 }
//             }
//             catch (IOException)
//             {
//                 // File is likely in use, wait and retry
//                 attempts++;
//                 Thread.Sleep(retryDelayMs);
//             }
//         }

//         // Return null if the file couldn't be accessed after all retries
//         return null;
//     }

//     void ParseOSM(string xml)
//     {
//         XmlDocument doc = new XmlDocument();
//         doc.LoadXml(xml);

//         // Parse nodes
//         XmlNodeList nodes = doc.SelectNodes("//node");
//         foreach (XmlNode node in nodes)
//         {
//             string id = node.Attributes["id"].Value;
//             float lat = float.Parse(node.Attributes["lat"].Value);
//             float lon = float.Parse(node.Attributes["lon"].Value);

//             // Convert lat/lon to Unity coordinates
//             Vector3 position = LatLonToUnity(lat, lon);
//             nodeDictionary[id] = position;
//         }

//         // Parse ways and generate buildings
//         XmlNodeList ways = doc.SelectNodes("//way");
//         foreach (XmlNode way in ways)
//         {
//             bool isBuilding = false;
//             float height = 10.0f; // Default height if none provided.
//             List<Vector3> footprint = new List<Vector3>();

//             foreach (XmlNode tag in way.SelectNodes("tag"))
//             {
//                 string k = tag.Attributes["k"].Value;
//                 string v = tag.Attributes["v"].Value;

//                 if (k == "building")
//                 {
//                     isBuilding = true;
//                 }

//                 if (k == "height")
//                 {
//                     height = float.Parse(v);
//                 }
//             }

//             if (isBuilding)
//             {
//                 foreach (XmlNode nd in way.SelectNodes("nd"))
//                 {
//                     string refId = nd.Attributes["ref"].Value;
//                     if (nodeDictionary.ContainsKey(refId))
//                     {
//                         footprint.Add(nodeDictionary[refId]);
//                     }
//                 }

//                 if (footprint.Count > 2) // A valid polygon must have at least 3 points
//                 {
//                     GenerateBuildingMesh(footprint, height);
//                 }
//             }
//         }
//     }

//     Vector3 LatLonToUnity(float lat, float lon)
//     {
//         // Example conversion. Replace with your preferred method.
//         float x = lon * scale;
//         float z = lat * scale;
//         return new Vector3(x, 0, z);
//     }

//     void GenerateBuildingMesh(List<Vector3> footprint, float height)
//     {
//         GameObject building = new GameObject("Building");
//         MeshFilter mf = building.AddComponent<MeshFilter>();
//         MeshRenderer mr = building.AddComponent<MeshRenderer>();

//         Mesh mesh = new Mesh();

//         // Vertices for the building
//         List<Vector3> vertices = new List<Vector3>();
//         foreach (Vector3 point in footprint)
//         {
//             vertices.Add(point); // Ground vertices
//             vertices.Add(point + Vector3.up * height); // Roof vertices
//         }

//         // Triangles for walls and roof
//         List<int> triangles = new List<int>();
//         int n = footprint.Count;

//         // Add walls
//         for (int i = 0; i < n; i++)
//         {
//             int next = (i + 1) % n;
//             triangles.Add(i * 2);     // Bottom current
//             triangles.Add(next * 2); // Bottom next
//             triangles.Add(i * 2 + 1); // Top current

//             triangles.Add(next * 2); // Bottom next
//             triangles.Add(next * 2 + 1); // Top next
//             triangles.Add(i * 2 + 1); // Top current
//         }

//         // Add roof (fan method)
//         int roofStart = n * 2;
//         for (int i = 1; i < n - 1; i++)
//         {
//             triangles.Add(roofStart);
//             triangles.Add(roofStart + i);
//             triangles.Add(roofStart + i + 1);
//         }

//         // Assign mesh data
//         mesh.vertices = vertices.ToArray();
//         mesh.triangles = triangles.ToArray();
//         mesh.RecalculateNormals();

//         mf.mesh = mesh;

//         // Assign a basic material
//         mr.material = new Material(Shader.Find("Standard"));
//     }
// }
