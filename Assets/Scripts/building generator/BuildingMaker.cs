using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MapMaker;

/*
    Copyright (c) 2017 Sloan Kelly

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/


class BuildingMaker : InfrastructureBehaviour
{
    public Material building;
    private OsmNode r1;
    private OsmNode r3;
    private Vector3 v8;
    private Vector3 v9;
    private Vector3 v10;
    public bool isFinished = false;

    IEnumerator Start()
    {
        // Wait until the map is ready
        while (!map.IsReady)
        {
            yield return null;
        }

        // Iterate through all the buildings in the 'ways' list
        foreach (var way in map.ways.FindAll((w) => { return w.IsBuilding && w.NodeIDs.Count > 1; }))
        {
            // Create the object
            CreateObject(way, building, "Building");
            yield return null;
        }
        isFinished = true;
    }

    
    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices)
    {
        // Get the centre of the roof
        //Vector3 oTop = new Vector3(0, way.Height, 0);

        // First vector is the middle point in the roof
        //vectors.Add(oTop);
        //normals.Add(Vector3.up);
        //uvs.Add(new Vector2(0.5f, 0.5f));
        

        List<int> roofPointsRemoved = new List<int>();
        
        float maxHeight = float.MinValue;
        Vector3 originOffset = origin - map.bounds.Centre;

        for (int i = 0; i < way.NodeIDs.Count; i++){
            OsmNode temp2 = map.nodes[way.NodeIDs[i]];
            Vector3 tempS1 = temp2 - origin;
            Vector3 worldBottom = new Vector3(tempS1.x + originOffset.x, 0, tempS1.z + originOffset.z);
            float groundY = terrain.SampleHeight(worldBottom);

            maxHeight = Mathf.Max(maxHeight, groundY); //track the highest point
        }
        float highestPoint = maxHeight + way.Height; // Add building height

        for (int i = 0; i < way.NodeIDs.Count; i++)
        {
            // Find roof points (r1, r2, r3)
            OsmNode r1 = null, r2 = null, r3 = null;

            // Backwards loop to find r1
            for (int k = i; k >= 0; k--)
            {
                int idx = (k - 1 + way.NodeIDs.Count) % way.NodeIDs.Count;
                if (!roofPointsRemoved.Contains(idx))
                {
                    r1 = map.nodes[way.NodeIDs[idx]];
                    break;
                }
            }

            r2 = map.nodes[way.NodeIDs[i]];

            for (int j = i + 1; j < way.NodeIDs.Count; j++)
            {
                int idx = (j) % way.NodeIDs.Count;
                if (!roofPointsRemoved.Contains(idx))
                {
                    r3 = map.nodes[way.NodeIDs[idx]];
                    break;
                }
            }

            // Wall nodes
            OsmNode w1 = map.nodes[way.NodeIDs[i]];
            OsmNode w2 = map.nodes[way.NodeIDs[(i + 1) % way.NodeIDs.Count]];

            Vector3 s1 = w1 - origin;
            Vector3 s2 = w2 - origin;

            int subdivisions = 4;
            float scaleFactor = 5f;
            // float maxHeight = float.MinValue;

            // for (int j = 0; j <= subdivisions; j++)
            // {
            //     float t = j / (float)subdivisions;
            //     Vector3 wallPoint = Vector3.Lerp(s1, s2, t);

            //     Vector3 worldBottom = new Vector3(wallPoint.x + originOffset.x, 0, wallPoint.z + originOffset.z);
            //     float groundY = terrain.SampleHeight(worldBottom);

            //     maxHeight = Mathf.Max(maxHeight, groundY); //track the highest point
            // }

            // highestPoint = maxHeight + way.Height; // Add building height

            for (int j = 0; j <= subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                Vector3 wallPoint = Vector3.Lerp(s1, s2, t);

                Vector3 worldBottom = new Vector3(wallPoint.x + originOffset.x, 0, wallPoint.z + originOffset.z);
                float groundY = terrain.SampleHeight(worldBottom);

                Vector3 bottom = new Vector3(wallPoint.x, groundY, wallPoint.z);
                Vector3 top = new Vector3(wallPoint.x, highestPoint, wallPoint.z);

                vectors.Add(bottom);
                vectors.Add(top);

                float meshWidth = Vector3.Distance(s1, s2) / subdivisions;
                float meshHeight = way.Height;

                uvs.Add(new Vector2(t * meshWidth / scaleFactor, 0));
                uvs.Add(new Vector2(t * meshWidth / scaleFactor, meshHeight / scaleFactor));

                Vector3 wallNormal = Vector3.Cross(Vector3.up, s2 - s1).normalized;
                normals.Add(wallNormal);
                normals.Add(wallNormal);

                if (j > 0)
                {
                    int idx = vectors.Count;
                    int b1 = idx - 4;
                    int t1 = idx - 3;
                    int b2 = idx - 2;
                    int t2 = idx - 1;

                    indices.Add(b1); indices.Add(b2); indices.Add(t1);
                    indices.Add(t1); indices.Add(b2); indices.Add(t2);
                }
            }

            
            

            // Prepare vertices for roof and top walls
            Vector3 v1 = w1 - origin;
            Vector3 v2 = w2 - origin;
            float originalY1 = v1.y;
            float originalY2 = v2.y;
            v1.y = terrain.SampleHeight(new Vector3(v1.x + originOffset.x, 0, v1.z + originOffset.z));
            v2.y = terrain.SampleHeight(new Vector3(v2.x + originOffset.x, 0, v2.z + originOffset.z));

            Vector3 v3 = v1 + new Vector3(0, way.Height + originalY1, 0);
            Vector3 v4 = v2 + new Vector3(0, way.Height + originalY2, 0);

            Vector3 v8 = new Vector3(0, way.Height, 0);
            Vector3 v9 = new Vector3(0, way.Height, 0);
            Vector3 v10 = new Vector3(0, way.Height, 0);

            if (r1 != null && r2 != null && r3 != null)
            {
                Vector3 v5 = r1 - origin;
                Vector3 v6 = r2 - origin;
                Vector3 v7 = r3 - origin;

                v8 = v5 + new Vector3(0, way.Height, 0);
                v9 = v6 + new Vector3(0, way.Height, 0);
                v10 = v7 + new Vector3(0, way.Height, 0);
            }

            vectors.Add(v1);
            vectors.Add(v2);
            vectors.Add(v3);
            vectors.Add(v4);
            vectors.Add(v8);
            vectors.Add(v9);
            vectors.Add(v10);

            float roofMeshWidth = Vector3.Distance(v1, v2);
            float roofMeshHeight = Vector3.Distance(v1, v3);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(roofMeshWidth / scaleFactor, 0));
            uvs.Add(new Vector2(0, roofMeshHeight / scaleFactor));
            uvs.Add(new Vector2(roofMeshWidth / scaleFactor, roofMeshHeight / scaleFactor));

            uvs.Add(new Vector2(0.5f, 0));
            uvs.Add(new Vector2(0.5f, 1));
            uvs.Add(new Vector2(0.25f, 0.5f));

            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            int idx7 = vectors.Count - 1;
            int idx6 = vectors.Count - 2;
            int idx5 = vectors.Count - 3;
            int idx4 = vectors.Count - 4;
            int idx3 = vectors.Count - 5;
            int idx2 = vectors.Count - 6;
            int idx1 = vectors.Count - 7;

    // Optionally add triangle indices for roof or other polygons here



            // And now the roof triangles
        //     indices.Add(0);
        //     indices.Add(idx3);
        //     indices.Add(idx4);
            
        //     // Don't forget the upside down one!
        //     indices.Add(idx4);
        //     indices.Add(idx3);
        //     indices.Add(0);
        }
        // Debug.Log("Roof points: " + roofPoints + roofPoints.Count);
        // for(int i = 0; i < roofPoints.Count; i++){
        //     int index1 = (i -1 + roofPoints.Count) % roofPoints.Count;
        //     int index2 = i;
        //     int index3 = (i + 1) % roofPoints.Count;

        //     Vector3 p1 = roofPoints[index1];
        //     Vector3 p2 = roofPoints[index2];
        //     Vector3 p3 = roofPoints[index3];

        //     if(IsEar(p1, p2, p3, vectors)){
        //         indices.Add(index1);
        //         indices.Add(index2);
        //         indices.Add(index3);
        //     }
        // }

    }

    public bool IsEar(Vector3 p1, Vector3 p2, Vector3 p3, List<Vector3> polygon)
    {
        // Check if the triangle is convex
        // if (!IsConvex(p1, p2, p3)){
        //     Debug.Log("Triangle is not convex");
        //     return false;
        // }
        // Check if no other points are inside the triangle
        foreach (Vector3 point in polygon)
        {
            // Skip the vertices that form the triangle
            if (point == p1 || point == p2 || point == p3)
                continue;

            // If any point is inside the triangle, it's not an ear
            if (IsPointInTriangle(point, p1, p2, p3))
                return false;
        }

        // If no points are inside and the triangle is convex, it's an ear
        return true;
    }

   
    // private bool IsConvex(Vector3 p1, Vector3 p2, Vector3 p3)
    // {
    //     bool isCon = true;
    //     // Compute the cross product of the two edges (p2 - p1) and (p3 - p1)
    //     Vector3 edge1 = p2 - p1;
    //     Vector3 edge2 = p3 - p1;
    //     float crossProduct = edge1.x * edge2.y - edge1.y * edge2.x;

    //     // Check if the cross product is negative, which indicates concave behavior
    //     if (crossProduct > 0)
    //     {
    //         // Initialize the sign of the cross product
    //         return isCon;
    //     }
    //     else if ((crossProduct > 0) != isCon)
    //     {
    //         // If the sign of the cross product changes, the polygon is concave
    //         return false;
    //     }
    //     return isCon;
    // }

    public bool IsPointInTriangle(Vector3 point, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        
        // Calculate vectors
        Vector3 v0 = v3 - v1;
        Vector3 v1v = v2 - v1;
        Vector3 vp = point - v1;

        // Compute dot products
        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1v);
        float dot02 = Vector3.Dot(v0, vp);
        float dot11 = Vector3.Dot(v1v, v1v);
        float dot12 = Vector3.Dot(v1v, vp);

        // Compute the determinant
        float denom = (dot00 * dot11 - dot01 * dot01);
        if (Mathf.Approximately(denom, 0))
        {
            // The triangle is degenerate (collinear points)
            return false;
        }

        // Compute barycentric coordinates
        float invDenom = 1 / denom;
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is inside the triangle
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }


}