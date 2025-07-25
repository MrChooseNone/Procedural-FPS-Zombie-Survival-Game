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

    ----------------------------------------
    Modified 2025 by Alexander Ohlsson
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
    public GameObject[] frames;
    public GameObject[] props;
    public float thick_ness;
    public GameObject diffPrefab;

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
            CreateObject(way, building, "Building", null, null, null, false, diffPrefab);
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

        for (int i = 0; i < way.NodeIDs.Count; i++)
        {
            OsmNode temp2 = map.nodes[way.NodeIDs[i]];
            Vector3 tempS1 = temp2 - origin;
            Vector3 worldBottom = new Vector3(tempS1.x + originOffset.x, 0, tempS1.z + originOffset.z);
            float groundY = terrain.SampleHeight(worldBottom);

            maxHeight = Mathf.Max(maxHeight, groundY); //track the highest point
        }
        float highestPoint = maxHeight + way.Height; // Add building height
        GameObject randomFrame = frames[Random.Range(0, frames.Length)];

        //clockwise or counter clockwise
         List<Vector3> footprint = new List<Vector3>();
        foreach (var nodeID in way.NodeIDs)
        {
            OsmNode osmNode = map.nodes[nodeID];
            Vector3 local = osmNode - origin; 
            Vector3 world2D = new Vector3(local.x, 0f, local.z) + (origin - map.bounds.Centre);
            footprint.Add(world2D);  // y=0 here, we only care about XZ
        }
        bool isCCW = (SignedPolygonAreaXZ(footprint) > 0f);


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
            Vector3 lastBottom = Vector3.zero;
            for (int j = 0; j <= subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                Vector3 wallPoint = Vector3.Lerp(s1, s2, t);

                Vector3 worldBottom = new Vector3(wallPoint.x + originOffset.x, 0, wallPoint.z + originOffset.z);
                float groundY = terrain.SampleHeight(worldBottom);

                Vector3 bottom = new Vector3(wallPoint.x, groundY, wallPoint.z);
                Vector3 top = new Vector3(wallPoint.x, highestPoint, wallPoint.z);
                SpawnSegment(bottom, top, randomFrame, thick_ness, origin);

                vectors.Add(bottom);
                vectors.Add(top);

                float meshWidth = Vector3.Distance(s1, s2) / subdivisions;
                float meshHeight = way.Height;

                uvs.Add(new Vector2(t * meshWidth / scaleFactor, 0));
                uvs.Add(new Vector2(t * meshWidth / scaleFactor, meshHeight / scaleFactor));

                //Vector3 wallNormal = Vector3.Cross(Vector3.up, s2 - s1).normalized;
                Vector3 edgeDir = (s2 - s1).normalized;
                Vector3 wallNormal = isCCW
                    ? Vector3.Cross(Vector3.up, edgeDir).normalized
                    : Vector3.Cross(edgeDir, Vector3.up).normalized;
                normals.Add(wallNormal);
                normals.Add(wallNormal);

                if (j > 0)
                {
                    int idx = vectors.Count;
                    int b1 = idx - 4;
                    int t1 = idx - 3;
                    int b2 = idx - 2;
                    int t2 = idx - 1;

                    if (!isCCW)
                    {
                        // Keep same order as before if CCW:
                        indices.Add(b1); indices.Add(b2); indices.Add(t1);
                        indices.Add(t1); indices.Add(b2); indices.Add(t2);
                    }
                    else
                    {
                        // Reverse winding if CW:
                        indices.Add(b1); indices.Add(t1); indices.Add(b2);
                        indices.Add(t1); indices.Add(t2); indices.Add(b2);
                    }
                    // indices.Add(b1); indices.Add(b2); indices.Add(t1);
                    // indices.Add(t1); indices.Add(b2); indices.Add(t2);
                    GameObject randomProp = props[Random.Range(0, props.Length)];
                    float rand = Random.Range(0f, 1f);
                    if (rand > 0.9)
                    {
                        SpawnProp(bottom, lastBottom, randomProp, 1, origin);
                    }
                }
                lastBottom = bottom;
            }
            Vector3 x1 = new Vector3(s1.x, highestPoint, s1.z);
            Vector3 x2 = new Vector3(s2.x, highestPoint, s2.z);
            SpawnSegment(x1, x2, randomFrame, thick_ness, origin);
        }
        

        Debug.Log(way.NodeIDs.Count);
        if (way.NodeIDs.Count == 5)
        {
            Vector3 firstTopRoof = Vector3.zero;
            Vector3 secondTopRoof = Vector3.zero;
            float randomRoofHeight = Random.Range(1, 5);
            for (int i = 0; i < way.NodeIDs.Count; i++)
            {
                OsmNode w1 = map.nodes[way.NodeIDs[i]];
                OsmNode w2 = map.nodes[way.NodeIDs[(i + 1) % way.NodeIDs.Count]];

                Vector3 s1 = w1 - origin;
                Vector3 s2 = w2 - origin;
                if (i == 0 || i == 2)   // for the 2 parallel sides
                {
                    float x = 0.5f;  // for the midpoint
                    Vector3 pointOnWall = Vector3.Lerp(s1, s2, x);

                    Vector3 topRoof = new Vector3(pointOnWall.x, highestPoint + randomRoofHeight, pointOnWall.z);
                    Vector3 topRight = new Vector3(s1.x, highestPoint, s1.z);
                    Vector3 topLeft = new Vector3(s2.x, highestPoint, s2.z);
                    SpawnSegment(topRoof, topRight, randomFrame, thick_ness, origin);
                    SpawnSegment(topRoof, topLeft, randomFrame, thick_ness, origin);
                    if (i == 0)
                    {
                        firstTopRoof = topRoof;
                    }
                    else
                    {
                        secondTopRoof = topRoof;
                    }

                    vectors.Add(topRoof);
                    vectors.Add(topRight);
                    vectors.Add(topLeft);

                    Vector3 wallNormalroof = Vector3.Cross(Vector3.up, s2 - s1).normalized;
                    normals.Add(wallNormalroof);
                    normals.Add(wallNormalroof);
                    normals.Add(wallNormalroof);

                    int idxr = vectors.Count;
                    int roof1 = idxr - 3;
                    int roof2 = idxr - 2;
                    int roof3 = idxr - 1;
                    indices.Add(roof1); indices.Add(roof2); indices.Add(roof3);
                }
            }
            for (int i = 0; i < way.NodeIDs.Count; i++)
            {
                OsmNode w1 = map.nodes[way.NodeIDs[i]];
                OsmNode w2 = map.nodes[way.NodeIDs[(i + 1) % way.NodeIDs.Count]];

                Vector3 s1 = w1 - origin;
                Vector3 s2 = w2 - origin;
                if (i == 1 || i == 3)   // for the 2 parallel sides
                {
                    
                    Vector3 topRight = new Vector3(s1.x, highestPoint, s1.z);
                    Vector3 topLeft = new Vector3(s2.x, highestPoint, s2.z);
                    SpawnSegment(firstTopRoof, secondTopRoof, randomFrame, thick_ness, origin);

                    vectors.Add(firstTopRoof);
                    vectors.Add(secondTopRoof);
                    vectors.Add(topRight);
                    vectors.Add(topLeft);

                    Vector3 wallNormalroof = Vector3.Cross(Vector3.up, s2 - s1).normalized;

                    normals.Add(wallNormalroof);
                    normals.Add(wallNormalroof);
                    normals.Add(wallNormalroof);
                    normals.Add(wallNormalroof);

                    int idx = vectors.Count;
                    int b1 = idx - 4;
                    int t1 = idx - 3;
                    int b2 = idx - 2;
                    int t2 = idx - 1;
                    if (i == 1)
                    {
                        indices.Add(b1); indices.Add(b2); indices.Add(t1);
                        indices.Add(t1); indices.Add(b2); indices.Add(t2);
                    }
                    else
                    {
                        indices.Add(b1); indices.Add(t1); indices.Add(t2);
                        indices.Add(t1); indices.Add(b2); indices.Add(t2);
                    }
                }
            }
        }
        
        // Prepare vertices for roof and top walls
            // Vector3 v1 = w1 - origin;
            // Vector3 v2 = w2 - origin;
            // float originalY1 = v1.y;
            // float originalY2 = v2.y;
            // v1.y = terrain.SampleHeight(new Vector3(v1.x + originOffset.x, 0, v1.z + originOffset.z));
            // v2.y = terrain.SampleHeight(new Vector3(v2.x + originOffset.x, 0, v2.z + originOffset.z));

            // Vector3 v3 = v1 + new Vector3(0, way.Height + originalY1, 0);
            // Vector3 v4 = v2 + new Vector3(0, way.Height + originalY2, 0);

            // Vector3 v8 = new Vector3(0, way.Height, 0);
            // Vector3 v9 = new Vector3(0, way.Height, 0);
            // Vector3 v10 = new Vector3(0, way.Height, 0);

            // if (r1 != null && r2 != null && r3 != null)
            // {
            //     Vector3 v5 = r1 - origin;
            //     Vector3 v6 = r2 - origin;
            //     Vector3 v7 = r3 - origin;

            //     v8 = v5 + new Vector3(0, way.Height, 0);
            //     v9 = v6 + new Vector3(0, way.Height, 0);
            //     v10 = v7 + new Vector3(0, way.Height, 0);
            // }

            // vectors.Add(v1);
            // vectors.Add(v2);
            // vectors.Add(v3);
            // vectors.Add(v4);
            // vectors.Add(v8);
            // vectors.Add(v9);
            // vectors.Add(v10);

            // float roofMeshWidth = Vector3.Distance(v1, v2);
            // float roofMeshHeight = Vector3.Distance(v1, v3);

            // uvs.Add(new Vector2(0, 0));
            // uvs.Add(new Vector2(roofMeshWidth / scaleFactor, 0));
            // uvs.Add(new Vector2(0, roofMeshHeight / scaleFactor));
            // uvs.Add(new Vector2(roofMeshWidth / scaleFactor, roofMeshHeight / scaleFactor));

            // uvs.Add(new Vector2(0.5f, 0));
            // uvs.Add(new Vector2(0.5f, 1));
            // uvs.Add(new Vector2(0.25f, 0.5f));

            // normals.Add(-Vector3.forward);
            // normals.Add(-Vector3.forward);
            // normals.Add(-Vector3.forward);
            // normals.Add(-Vector3.forward);
            // normals.Add(Vector3.up);
            // normals.Add(Vector3.up);
            // normals.Add(Vector3.up);

            // int idx7 = vectors.Count - 1;
            // int idx6 = vectors.Count - 2;
            // int idx5 = vectors.Count - 3;
            // int idx4 = vectors.Count - 4;
            // int idx3 = vectors.Count - 5;
            // int idx2 = vectors.Count - 6;
            // int idx1 = vectors.Count - 7;

            // Optionally add triangle indices for roof or other polygons here



            // And now the roof triangles
            //     indices.Add(0);
            //     indices.Add(idx3);
            //     indices.Add(idx4);

            //     // Don't forget the upside down one!
            //     indices.Add(idx4);
            //     indices.Add(idx3);
            //     indices.Add(0);
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
    
    float SignedPolygonAreaXZ(List<Vector3> points)
    {
        // points.Count must be ≥ 3. We assume points are in order,
        // and the polygon is closed (i.e. the edge from last→first is implied).
        float sum = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % points.Count];
            // Only use X,Z for 2D area:
            sum += (a.x * b.z) - (b.x * a.z);
        }
        // Actually this is 2×(signed area); positive if CCW, negative if CW.
        return sum * 0.5f;
    }

     public void SpawnSegment(Vector3 s1, Vector3 s2, GameObject linePrefab, float thickness, Vector3 origin)
    {

        GameObject line = linePrefab != null
            ? Instantiate(linePrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.tag = "CullTarget";

        Vector3 dir = s2 - s1;
        float length = dir.magnitude;
        float x = 0.5f;  // for the midpoint
        Vector3 pointOnWall = Vector3.Lerp(s1, s2, x);

        line.transform.position = pointOnWall + (origin - map.bounds.Centre);

        line.transform.rotation = Quaternion.LookRotation(dir.normalized);

        line.transform.localScale = new Vector3(thickness, thickness, length);
    }

    public void SpawnProp(Vector3 s1, Vector3 s2, GameObject Prefab, float size, Vector3 origin)
    {
        // 1. create or clone your line object
        GameObject line;
        if (Prefab != null) {

            line = Instantiate(Prefab);
            line.tag = "CullTarget";
            
            float x = 0.5f;  // for the midpoint
            Vector3 pointOnWall = Vector3.Lerp(s1, s2, x);
            Vector3 dir = s2 - s1;

            line.transform.position = pointOnWall +(origin - map.bounds.Centre);
            line.transform.position += new Vector3(2,0,0);
            line.transform.rotation = Quaternion.LookRotation(dir.normalized);

            line.transform.localScale = new Vector3(size, size, size);
        }
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