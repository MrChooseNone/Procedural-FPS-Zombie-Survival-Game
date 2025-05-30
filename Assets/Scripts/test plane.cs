using UnityEngine;

public class testplane : MonoBehaviour
{
    public static bool PlanePlaneIntersection(Plane p1, Plane p2, out Vector3 pointOnLine, out Vector3 direction)
    {
        // Normals
        Vector3 n1 = p1.normal;
        Vector3 n2 = p2.normal;

        // Direction = cross(n1, n2)
        direction = Vector3.Cross(n1, n2);
        float denom = direction.sqrMagnitude;
        if (denom < 1e-6f)
        {
            // Normals are parallel (no unique intersection line)
            pointOnLine = Vector3.zero;
            direction = Vector3.zero;
            return false;
        }

        // Unity's Plane stores distance so that: dot(n, X) + d = 0
        // so plane eq is dot(n, X) = -d
        float d1 = p1.distance;
        float d2 = p2.distance;

        // Compute a point on the line:
        // p = [ cross(direction, n2)*d1 + cross(n1, direction)*d2 ] / |direction|^2
        pointOnLine = (Vector3.Cross(direction, n2) * d1 + Vector3.Cross(n1, direction) * d2) / denom;

        return true;
    }


    // public Vector3 normal1 = Vector3.up;
    // public float distance1 = -1f; // y = 1

    // public Vector3 normal2 = Vector3.forward;
    // public float distance2 = -2f; // z = 2

    // void Start()
    // {
    //     // Create Plane structs
    //     Plane plane1 = new Plane(normal1.normalized, distance1);
    //     Plane plane2 = new Plane(normal2.normalized, distance2);

    //     // Spawn visual quads for planes
    //     GameObject visualPlane1 = CreatePlaneVisual("Plane 1", plane1, Color.blue);
    //     GameObject visualPlane2 = CreatePlaneVisual("Plane 2", plane2, Color.green);

    //     // Compute intersection
    //     if (PlanePlaneIntersection(plane1, plane2, out var point, out var direction))
    //     {
    //         Debug.DrawRay(point, direction * 10f, Color.red, 100f);
    //         //Debug.DrawRay(point, -direction * 10f, Color.red, 100f);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("Planes are parallel or coincident, no unique intersection line.");
    //     }
    // }

    // GameObject CreatePlaneVisual(string name, Plane plane, Color color)
    // {
    //     GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
    //     quad.name = name;

    //     // Give it a color
    //     Material mat = new Material(Shader.Find("Unlit/Color"));
    //     mat.color = color;
    //     quad.GetComponent<Renderer>().material = mat;

    //     // Orient and position the quad
    //     quad.transform.localScale = new Vector3(10f, 10f, 1f); // make it large
    //     quad.transform.rotation = Quaternion.LookRotation(plane.normal);
    //     quad.transform.position = -plane.normal * plane.distance;

    //     return quad;
    // }
 
    public Vector3 normal1 = Vector3.up;
    public float distance1 = -1f;

    public Vector3 normal2 = Vector3.forward;
    public float distance2 = -2f;

    public float planeSize = 10f;

    void Start()
    {
        Plane p1 = new Plane(normal1.normalized, distance1);
        Plane p2 = new Plane(normal2.normalized, distance2);

        // Get intersection line
        if (PlanePlaneIntersection(p1, p2, out var point, out var dir))
        {
            // Spawn cut half-planes
            CreateHalfPlane(p1, point, dir, Color.blue, "Roof Wing A");
            CreateHalfPlane(p2, point, dir, Color.green, "Roof Wing B");

            Debug.DrawRay(point, dir * 10f, Color.red, 100f);
            Debug.DrawRay(point, -dir * 10f, Color.red, 100f);
        }
        else
        {
            Debug.LogWarning("Planes are parallel or coincident.");
        }
    }

    void CreateHalfPlane(Plane plane, Vector3 cutPoint, Vector3 cutDir, Color color, string name)
    {
        // Create quad vertices for full plane (local)
        Vector3 right = Vector3.Cross(cutDir, plane.normal).normalized;
        Vector3 forward = Vector3.Cross(right, plane.normal).normalized;

        // Center point of plane
        Vector3 center = -plane.normal * plane.distance;

        Vector3[] verts = new Vector3[4];
        verts[0] = center + (right + forward) * planeSize;   // top right
        verts[1] = center + (-right + forward) * planeSize;  // top left
        verts[2] = center + (-right - forward) * planeSize;  // bottom left
        verts[3] = center + (right - forward) * planeSize;   // bottom right

        // Clip the quad to the side of the cut line
        Vector3 planeNormal = Vector3.Cross(cutDir, plane.normal).normalized;

        // Use dot product to keep only vertices on the positive side of the cutting plane
        System.Collections.Generic.List<Vector3> keptVerts = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < 4; i++)
        {
            Vector3 toVert = verts[i] - cutPoint;
            if (Vector3.Dot(toVert, planeNormal) >= 0f)
                keptVerts.Add(verts[i]);
        }
        Debug.Log($"{name} — vertices kept after clipping: {keptVerts.Count}");

        // Ensure a triangle or quad
        if (keptVerts.Count < 3)
            return;

        // Triangulate (assuming keptVerts are convex and ordered)
        GameObject obj = new GameObject(name);
        obj.transform.position = Vector3.zero;
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = keptVerts.ToArray();

        int[] tris;
        if (keptVerts.Count == 3)
            tris = new int[] { 0, 1, 2 };
        else
            tris = new int[] { 0, 1, 2, 0, 2, 3 };

        mesh.triangles = tris;
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) { color = color };
        Instantiate(obj);
    }




}
