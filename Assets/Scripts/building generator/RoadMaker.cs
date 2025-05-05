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


class RoadMaker : InfrastructureBehaviour
{
    public Material roadMaterial;
    public bool Roadflag = false;
    public GameObject lightPost;
    [System.Serializable]
    public class PrefabProbability
    {
        public GameObject prefab;  // The prefab to spawn
        public float probability;  // Probability of spawning the prefab
    }
    public PrefabProbability[] prefabProbabilities;
    GameObject[] randomPrefab = new GameObject[10];

    
    IEnumerator Start()
    {
        // Wait for the map to become ready
        while (!map.IsReady)
        {
            yield return null;
        }
        
        
        // Iterate through the roads and build each one
        foreach (var way in map.ways.FindAll((w) => { return w.IsRoad; }))
        {
            yield return StartCoroutine(CreatePrefabArray());
            Roadflag = true;
           
            CreateObject(way, roadMaterial, way.Name, lightPost, randomPrefab);
            yield return null;
        }
    }
    
    
    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices)
    {
        int subdivisions = 8; // Increase this for smoother curves
        float totalLength = 0f;
        float uvTilingFactor = 1f; // Adjust texture tiling along the road

        for (int i = 1; i < way.NodeIDs.Count; i++)
        {
            OsmNode p1 = map.nodes[way.NodeIDs[i - 1]];
            OsmNode p2 = map.nodes[way.NodeIDs[i]];

            Vector3 s1 = p1 - origin;
            Vector3 s2 = p2 - origin;

            Vector3 diff = (s2 - s1).normalized;
            var cross = Vector3.Cross(diff, Vector3.up) * 3.7f * way.Lanes;

            float segmentLength = Vector3.Distance(s1, s2);
            float currentSegmentLength = 0f; // Reset per segment

            for (int j = 0; j <= subdivisions; j++) // Generate intermediate points
            {
                float t = j / (float)subdivisions; // Interpolation factor
                Vector3 midPoint = Vector3.Lerp(s1, s2, t);
                currentSegmentLength = t * segmentLength; // Distance along the segment

                // Compute width of road at this point
                Vector3 v1 = midPoint + cross;
                Vector3 v2 = midPoint - cross;

                // Adjust to terrain
                Vector3 h1 = origin - map.bounds.Centre;

                v1.y = terrain.SampleHeight(new Vector3(v1.x + h1.x, 0, v1.z + h1.z));
                v2.y = terrain.SampleHeight(new Vector3(v2.x + h1.x, 0, v2.z + h1.z));

                // Store points
                vectors.Add(v1);
                vectors.Add(v2);

                // Corrected UV scaling
                float uvY = (totalLength + currentSegmentLength) / uvTilingFactor;
                uvs.Add(new Vector2(0, uvY));
                uvs.Add(new Vector2(1, uvY));

                normals.Add(Vector3.up);
                normals.Add(Vector3.up);

                // Generate triangles if not the first iteration
                if (j > 0)
                {
                    int idx1 = vectors.Count - 4;
                    int idx2 = vectors.Count - 3;
                    int idx3 = vectors.Count - 2;
                    int idx4 = vectors.Count - 1;

                    // First triangle (v1, v3, v2)
                    indices.Add(idx1);
                    indices.Add(idx3);
                    indices.Add(idx2);

                    // Second triangle (v3, v4, v2)
                    indices.Add(idx3);
                    indices.Add(idx4);
                    indices.Add(idx2);
                }
            }

            totalLength += segmentLength; // Accumulate total length after processing the segment
        }
    }


    
    IEnumerator CreatePrefabArray(){
        for(int i = 0; i < 10; i++){

            randomPrefab[i] = SpawnRandomPrefab();
        }
        yield return null;
    }

    public GameObject SpawnRandomPrefab()
    {
        
        float randomValue = Random.Range(0f, 1f);  // Random value between 0 and 1
        float cumulativeProbability = 0f;

        foreach (var item in prefabProbabilities)
        {
            cumulativeProbability += item.probability;

            if (randomValue <= cumulativeProbability)
            {
                return item.prefab;
            }
        }
        return lightPost;
    }
}