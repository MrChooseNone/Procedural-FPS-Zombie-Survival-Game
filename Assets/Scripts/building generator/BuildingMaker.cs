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

        for (int i = 0; i < way.NodeIDs.Count; i++)
        {
            //roof only
            for(int k = i; k > -way.NodeIDs.Count; k--){

                if(!roofPointsRemoved.Contains((k - 1 + way.NodeIDs.Count) %way.NodeIDs.Count)){
                    r1 = map.nodes[way.NodeIDs[(k - 1 + way.NodeIDs.Count) %way.NodeIDs.Count]];
                    break;
                }
            }
            OsmNode r2 = map.nodes[way.NodeIDs[i]];
            
            for(int j = i; j < way.NodeIDs.Count; j++){
                if(!roofPointsRemoved.Contains((j+1)%way.NodeIDs.Count)){
                    r3 = map.nodes[way.NodeIDs[(j+1)%way.NodeIDs.Count]];
                    break;
                }
            }
            //walls
            OsmNode w1 = map.nodes[way.NodeIDs[i]];
            OsmNode w2 = map.nodes[way.NodeIDs[(i+1)%way.NodeIDs.Count]];

            Vector3 h1 = origin - map.bounds.Centre;

            Vector3 v1 = w1 - origin;
            Vector3 v2 = w2 - origin;
            v1.y = terrain.SampleHeight(new Vector3(v1.x + h1.x, 0, v1.z + h1.z));
            v2.y = terrain.SampleHeight(new Vector3(v2.x + h1.x, 0, v2.z + h1.z));

            
            Vector3 v3 = v1 + new Vector3(0, way.Height, 0);
            Vector3 v4 = v2 + new Vector3(0, way.Height, 0);
            //for roof only
            if(r1 != null || r2 != null || r3 !=null){
                
                Vector3 v5 = r1 - origin;
                Vector3 v6 = r2 - origin;
                Vector3 v7 = r3 - origin;
                v8 = v5 + new Vector3(0, way.Height, 0);
                v9 = v6 + new Vector3(0, way.Height, 0);
                v10 = v7 + new Vector3(0, way.Height, 0);
            }
            else{
                
                v8 = new Vector3(0, way.Height, 0);
                v9 = new Vector3(0, way.Height, 0);
                v10 = new Vector3(0, way.Height, 0);
            }

            vectors.Add(v1); //nere
            vectors.Add(v2);  //nere
            vectors.Add(v3);  
            vectors.Add(v4);  
            vectors.Add(v8);  ///uppe tillbaka ett steg
            vectors.Add(v9);  //uppe den som man kollar
            vectors.Add(v10);  //uppe framåt ett steg

            float meshWidth = Vector3.Distance(v1, v2);
            float meshHeight = Vector3.Distance(v1, v3);

            float scaleFactor = 5f; // Adjust this to control texture tiling
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(meshWidth / scaleFactor, 0)); 
            uvs.Add(new Vector2(0, meshHeight / scaleFactor));
            uvs.Add(new Vector2(meshWidth / scaleFactor, meshHeight / scaleFactor));

            uvs.Add(new Vector2(0.5f, 0));  // Example UV for the roof
            uvs.Add(new Vector2(0.5f, 1));  // Example UV for the roof
            uvs.Add(new Vector2(0.25f, 0.5f)); // Example UV for another part of the roof


            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(Vector3.up);  // Assuming roof normals are facing up
            normals.Add(Vector3.up);  // Assuming roof normals are facing up
            normals.Add(Vector3.up);  // Assuming roof normals are facing up

            int idx1, idx2, idx3, idx4, idx5, idx6, idx7;
            idx7 = vectors.Count - 1; //v10
            idx6 = vectors.Count - 2; //v9
            idx5 = vectors.Count - 3; //v8
            idx4 = vectors.Count - 4; //v4
            idx3 = vectors.Count - 5; //v3
            idx2 = vectors.Count - 6; //v2
            idx1 = vectors.Count - 7; //v1

            // first triangle v1, v3, v2
            indices.Add(idx1);
            indices.Add(idx3);
            indices.Add(idx2);

            // second         v3, v4, v2
            indices.Add(idx3);
            indices.Add(idx4);
            indices.Add(idx2);

            // third          v2, v3, v1
            indices.Add(idx2);
            indices.Add(idx3);
            indices.Add(idx1);

            // fourth         v2, v4, v3
            indices.Add(idx2);
            indices.Add(idx4);
            indices.Add(idx3);

            if(IsEar(v8, v9, v10, vectors)){
                indices.Add(idx5);
                indices.Add(idx7);
                indices.Add(idx6);
                roofPointsRemoved.Add(i);
                Debug.Log("is ear");
            }

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