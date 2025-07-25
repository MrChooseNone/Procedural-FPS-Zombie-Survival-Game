using System.Collections.Generic;
using UnityEngine;
using MapMaker;
using System.Collections;
using Mirror;




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
    Where the new function are SpawnMesh, SpawnPrefab, IsvalidTreePosition, AdjustBuildingHeight
*/


/// Base infrastructure creator.

[RequireComponent(typeof(MapReader))]
abstract class InfrastructureBehaviour : NetworkBehaviour
{
    
    /// The map reader object; contains all the data to build procedural geometry.
    protected Terrain terrain;

    
    protected MapReader map;
    protected RoadMaker Rmaker;
    

    void Awake()
    {
         
        map = GetComponent<MapReader>();
        Rmaker = GetComponent<RoadMaker>();
        if(Terrain.activeTerrain != null){

        terrain = Terrain.activeTerrain;
        }
        
    }

 
    /// Get the centre of an object or road.
   
    
    
    protected Vector3 GetCentre(OsmWay way)
    {
        Vector3 total = Vector3.zero;

        foreach (var id in way.NodeIDs)
        {
            total += map.nodes[id];
        }

        return total / way.NodeIDs.Count;
    }

  
    [Server]
    protected void CreateObject(OsmWay way, Material mat, string objectName, GameObject prefab = null, GameObject[] prefabArray= null, GameObject[] carArray= null, bool single = false, GameObject diffPrefab = null)
    {
        // Make sure we have some name to display
        objectName = string.IsNullOrEmpty(objectName) ? "OsmWay" : objectName;

        // Create an instance of the object and place it in the centre of its points
        GameObject go = null;

        Vector3 localOrigin = GetCentre(way);
        if(prefab != null){
            if(Rmaker.Roadflag){
                spawnMesh(way, localOrigin, objectName, mat);
                StartCoroutine(Delay());
                
                SpawnPrefabs(way, localOrigin, prefab, objectName, true, prefabArray,carArray);
                
                
            }else{

                SpawnPrefabs(way, localOrigin, prefab, objectName, false, null, null, single);
            }
        }
        else{
            spawnMesh(way, localOrigin, objectName, mat, diffPrefab);
        }
    }
    IEnumerator Delay(){
        yield return new WaitForSeconds(.3f);
    }

    private void spawnMesh(OsmWay way, Vector3 localOrigin, string objectName, Material mat, GameObject diffPrefab = null){
        GameObject go;
         
        go = new GameObject(objectName);
        go.transform.position = localOrigin - map.bounds.Centre;
        if(objectName == "Building"){
            // go.AddComponent<BuildingInteraction>();

            // Optional: Set tag or other properties
            go.tag = "Building";
            go.AddComponent<BuildingInteriorLink>();
        }

        // Add the mesh filter and renderer components to the object
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        MeshCollider mc = go.AddComponent<MeshCollider>();

        // Apply the material

        // Create the collections for the object's vertices, indices, UVs etc.
        List<Vector3> vectors = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        // Call the child class' object creation code
        OnObjectCreated(way, localOrigin, vectors, normals, uvs, indices);
        // Debug.Log("ver: "+ vectors.Count);
        // Debug.Log("nor: "+ normals.Count);
        // Debug.Log("ind: "+ indices.Count);
        // Debug.Log("uvs: "+ uvs.Count);
        // Apply the data to the mesh
        mf.mesh.vertices = vectors.ToArray();
        mf.mesh.normals = normals.ToArray();
        mf.mesh.triangles = indices.ToArray();
        mf.mesh.uv = uvs.ToArray();

        //mf.mesh.RecalculateNormals();

        mc.sharedMesh = mf.mesh;
        mr.material = mat;
        //AdjustBuildingHeight(go);

        if (!go.TryGetComponent<NetworkIdentity>(out var networkIdentity))
        {
            go.AddComponent<NetworkIdentity>();
        }
        if (diffPrefab != null)
        {
            GameObject difficultPrefab = Instantiate(diffPrefab, Vector3.zero, Quaternion.identity);

            //difficultPrefab.transform.SetParent(go.transform, worldPositionStays: true);
            difficultPrefab.transform.position = go.transform.position;
            

            //difficultPrefab.transform.localPosition = Vector3.up * 2f; 
            BuildingDifficult buildingDifficult = difficultPrefab.GetComponent<BuildingDifficult>();
            if (buildingDifficult != null)
            {
                buildingDifficult.buildingRenderer = mr;
                buildingDifficult.CalculateDifficulty();
            }

            // Spawn on network
            NetworkServer.Spawn(difficultPrefab);
        }

        if (Rmaker.Roadflag == true)
        {
            go.transform.position += new Vector3(0, 0.1f, 0);
            go.tag = "Road";
            go.layer = 16;
            Rmaker.Roadflag = false;
        }
        // Add a NetworkIdentity component to make the object network-aware
        go.isStatic = true;
        NetworkManager.singleton.spawnPrefabs.Add(go);
        NetworkServer.Spawn(go);
    }

    private void SpawnPrefabs(OsmWay way, Vector3 localOrigin, GameObject prefab, string objectName, bool isRoad = false, GameObject[] prefabArray = null, GameObject[] carArray= null, bool single = false){
        GameObject go;
        
                float spacing = 5;
                float offsetProp = 0;
                float tiltProp = 0;
            if (isRoad)
            {
                spacing = Random.Range(5, 30);
                offsetProp = Random.Range(5f, 7f);
                tiltProp = Random.Range(0f, 6f);
            }
            else
            {

                spacing = 2.5f; // adjust
            }
            if (single)
            {
                OsmNode p1 = map.nodes[way.NodeIDs[0]];
                Vector3 position = p1 - localOrigin;
                Vector3 newPos = position + (localOrigin - map.bounds.Centre);
                Vector3 raypos = newPos;
                float terrainHeightray = terrain.SampleHeight(raypos);
                raypos.y = terrainHeightray;
                Vector3 rayOrigin = raypos + Vector3.up * 10f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 20f))
                {
                    Vector3 terrainPoint = hit.point;
                    Vector3 terrainNormal = hit.normal;



                    Vector3 movedPos = newPos;
                    float terrainHeight = terrain.SampleHeight(movedPos);
                    movedPos.y = terrainHeight;

                    go = Instantiate(prefab, movedPos, Quaternion.identity);

                    NetworkServer.Spawn(go);
                    go.name = objectName;
                }
            }
            else
            {
                
                for (int i = 1; i < way.NodeIDs.Count; i++)
                {
                    Transform parent = new GameObject($"placed {way.ID}").transform;
                    
                    OsmNode p1 = map.nodes[way.NodeIDs[i - 1]];
                    OsmNode p2 = map.nodes[way.NodeIDs[i]];

                    Vector3 start = p1 - localOrigin;
                    Vector3 end = p2 - localOrigin;

                    Vector3 diff = (end - start).normalized;
                    float distance = Vector3.Distance(start, end);

                    // Define the spacing between each prefab instance along the segment

                    int numPrefabs = Mathf.FloorToInt(distance / spacing);

                    for (int j = 0; j < numPrefabs; j++)
                    {
                        // Calculate the position along the line for each prefab
                        Vector3 position = Vector3.Lerp(start, end, (float)j / numPrefabs);

                        // Calculate direction (rotation) by looking from start to end point
                        //Quaternion rotation = Quaternion.LookRotation(diff);
                        //Quaternion rotatedRotation = rotation * Quaternion.Euler(0, 90, 0); // Rotate by 90 degrees around Y-axis

                        // Instantiate the prefab at the correct position and rotation
                        
                        Vector3 newPos = position + (localOrigin - map.bounds.Centre);
                        Vector3 raypos = newPos;
                        float terrainHeightray = terrain.SampleHeight(raypos);
                        raypos.y = terrainHeightray;
                        Vector3 rayOrigin = raypos + Vector3.up * 10f;
                        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 20f))
                        {
                            Vector3 terrainPoint = hit.point;
                            Vector3 terrainNormal = hit.normal;

                            // build a rotation that respects forward & slope
                            Quaternion alignedRot = Quaternion.LookRotation(diff, terrainNormal);


                            if (isRoad)
                            {
                                if (Random.Range(0f, 1f) <= 0.2f)
                                {
                                    int randomCar = Random.Range(0, carArray.Length);

                                    Vector3 carPos = newPos;
                                    float terrainHeightCar = terrain.SampleHeight(carPos);
                                    carPos.y = terrainHeightCar;
                                    if (IsValidTreePosition(carPos))
                                    {

                                        go = Instantiate(carArray[randomCar], carPos, alignedRot, parent);

                                        NetworkServer.Spawn(go);
                                        go.name = objectName;
                                    }
                                }
                                float tiltX = Random.Range(-tiltProp, +tiltProp);
                                float tiltZ = Random.Range(-tiltProp, +tiltProp);
                                Quaternion tiltRotation = Quaternion.Euler(tiltX, 0f, tiltZ);
                                Quaternion finalRotation = alignedRot * tiltRotation;

                                int random = Random.Range(0, prefabArray.Length);
                                Vector3 offset = Vector3.Cross(diff, Vector3.up).normalized * offsetProp; // Offset 5 units to the side

                                Vector3 movedPos = newPos + offset;
                                float terrainHeight = terrain.SampleHeight(movedPos);
                                movedPos.y = terrainHeight;

                                if (IsValidTreePosition(movedPos))
                                {

                                    go = Instantiate(prefabArray[random], movedPos, finalRotation, parent);

                                    NetworkServer.Spawn(go);
                                    go.name = objectName;
                                }
                            }
                            else
                            {
                                Vector3 movedPos = newPos;
                                float terrainHeight = terrain.SampleHeight(movedPos);
                                movedPos.y = terrainHeight;
                                Quaternion offsetRot = Quaternion.Euler(0, 90, 0);
                                Quaternion finalRot = alignedRot * offsetRot;
                                go = Instantiate(prefab, movedPos, finalRot, parent);

                                NetworkServer.Spawn(go);
                                go.name = objectName;
                            }
                        }
                        
                    }
            }
            }
    }
    private bool IsValidTreePosition(Vector3 position)
    {

        // Perform an overlap check to ensure there's enough space for the tree
        if (Physics.CheckSphere(position, 2f, LayerMask.GetMask("obsticles")))
        {
            return false; // Position overlaps with an obstacle
        }

        return true; // Valid position
    }
    private void AdjustBuildingHeight(GameObject go)
    {
        Vector3 buildingPos = go.transform.position;
        float terrainHeight = terrain.SampleHeight(buildingPos);
        buildingPos.y = terrainHeight;
        go.transform.position = buildingPos;
    }

    protected abstract void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices);
}