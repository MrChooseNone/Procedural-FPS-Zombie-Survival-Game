using UnityEngine;
using MapMaker;
using System.Collections;
using System.Collections.Generic;
class KerbPlacement : InfrastructureBehaviour
{
    public Material kerbMaterial;
    public GameObject kerbPrefab;

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

        foreach (var way in map.ways.FindAll((w) => { return w.IsKerb && w.NodeIDs.Count > 1; }))
        {
            

            CreateObject(way, kerbMaterial, "kerb", kerbPrefab);
            yield return null;
            

        }
    }
}