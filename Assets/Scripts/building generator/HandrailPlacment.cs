using UnityEngine;
using MapMaker;
using System.Collections;
using System.Collections.Generic;
class HandrailPlacement : InfrastructureBehaviour
{
    public Material HandrailMaterial;
    public GameObject HandrailPrefab;
    

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

        foreach (var way in map.ways.FindAll((w) => { return w.IsHandrail && w.NodeIDs.Count > 1; }))
        {
            

            CreateObject(way, HandrailMaterial, "Handrail", HandrailPrefab);
            yield return null;
            

        }
    }
}