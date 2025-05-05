using UnityEngine;
using MapMaker;
using System.Collections;
using System.Collections.Generic;
class DitchPlacement : InfrastructureBehaviour
{
    public Material DitchMaterial;
    public GameObject DitchPrefab;

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

        foreach (var way in map.ways.FindAll((w) => { return w.IsDitch && w.NodeIDs.Count > 1; }))
        {
            

            CreateObject(way, DitchMaterial, "Ditch", DitchPrefab);
            yield return null;
            

        }
    }
}