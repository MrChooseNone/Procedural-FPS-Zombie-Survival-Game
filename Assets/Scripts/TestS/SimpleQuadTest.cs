using UnityEngine;

public class CreateQuad : MonoBehaviour
{
    void Start()
    {
        // Create a new mesh
        Mesh mesh = new Mesh();

        // Define the vertices for the quad
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.5f, 0f, -0.5f); // Bottom-left
        vertices[1] = new Vector3( 0.5f, 0f, -0.5f); // Bottom-right
        vertices[2] = new Vector3( 0.5f, 0f,  0.5f); // Top-right
        vertices[3] = new Vector3(-0.5f, 0f,  0.5f); // Top-left

        // Define the triangles that make up the quad (two triangles to make a square)
        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 0;
        triangles[4] = 3;
        triangles[5] = 2;

        // Define the UVs (texture coordinates) for the quad
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0f, 0f); // Bottom-left
        uv[1] = new Vector2(1f, 0f); // Bottom-right
        uv[2] = new Vector2(1f, 1f); // Top-right
        uv[3] = new Vector2(0f, 1f); // Top-left

        // Set the mesh's vertices, triangles, and UVs
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // Optionally, you can recalculate normals to improve lighting
        mesh.RecalculateNormals();

        // Add the MeshFilter and MeshRenderer components to the GameObject
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Optionally, assign a default material if needed
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}
