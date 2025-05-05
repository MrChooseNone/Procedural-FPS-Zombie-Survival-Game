using UnityEngine;
using MapMaker;
using System.Collections;
using System.Collections.Generic;
class Placement : InfrastructureBehaviour
{
    public Material fenceMaterial;
    public GameObject fencePrefab;
    
   

    protected override void OnObjectCreated(OsmWay way, Vector3 origin, List<Vector3> vectors, List<Vector3> normals, List<Vector2> uvs, List<int> indices)
    {
        
    }


    IEnumerator Start()
    {
        // Wait until the map is ready
        while (!map.IsReady)
        {
            yield return null;
        }

        foreach (var way in map.ways.FindAll((w) => { return w.IsFence && w.NodeIDs.Count > 1; }))
        {
            

            CreateObject(way, fenceMaterial, "fence", fencePrefab);
            yield return null;
            

        }
    }
}
