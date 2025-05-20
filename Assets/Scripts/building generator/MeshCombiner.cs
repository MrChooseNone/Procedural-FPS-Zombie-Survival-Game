using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

class MeshChunkCombiner : NetworkBehaviour
{
    public BuildingMaker buildingMaker;
    public RoadMaker roadMaker;
    public TreePlacement treePlacement;
    public WallPlacement wallPlacement;
    public Placement fencePlacement;

    
    public int chunkSize = 500;           // Objects per combined mesh
    IEnumerator Start()
    {
       
        // yield return new WaitForSeconds(60f);
        while (!buildingMaker.isFinished && !roadMaker.isFinished && !treePlacement.isFinished && !wallPlacement.isFinished && !fencePlacement.isFinished)
        {
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(20f);
        CombineByTag("Building", "BuildingChunk");
        CombineByTag("Road", "RoadChunk");
        CombineByTag("Bush", "BushChunk");
        CombineByTag("Fence", "FenceChunk");
        CombineByTag("Tree", "TreeChunk");
        //CombineByTag("Lamp", "LampChunk");
        CombineByTag("City", "CityChunk");
        yield return new WaitForSeconds(20f);
        GrassManager localGrassManager = NetworkClient.localPlayer?.GetComponent<GrassManager>();
        
        if (localGrassManager != null)
        {
            localGrassManager.spawnGrass();
        }
        
        yield return new WaitForSeconds(10f);
        LoadingScreenManager localPlayer = NetworkClient.localPlayer?.GetComponent<LoadingScreenManager>();
        Debug.Log("hide loading screen");
        if (localPlayer != null)
        {
            localPlayer.HideLoadingScreen();
        }
    }

    

    public void CombineByTag(string tagName, string chunkPrefix)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tagName);
        int total = taggedObjects.Length;
        int current = 0;
        int chunkIndex = 0;

        while (current < total)
        {
            int count = Mathf.Min(chunkSize, total - current);
            List<CombineInstance> combines = new List<CombineInstance>();

            for (int i = 0; i < count; i++)
            {
                var obj = taggedObjects[current + i];
                MeshFilter mf = obj.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                ci.transform = mf.transform.localToWorldMatrix;
                combines.Add(ci);

                obj.SetActive(false); // Hide the original
            }

            GameObject chunk = new GameObject($"{chunkPrefix}_Chunk_{chunkIndex}");
            chunk.transform.parent = this.transform;
            chunk.layer = 16;

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combines.ToArray(), true, true);

            MeshFilter chunkMF = chunk.AddComponent<MeshFilter>();
            chunkMF.mesh = combinedMesh;

            MeshRenderer chunkMR = chunk.AddComponent<MeshRenderer>();
            chunkMR.sharedMaterial = taggedObjects[current].GetComponent<MeshRenderer>().sharedMaterial;

            MeshCollider collider = chunk.AddComponent<MeshCollider>();
            collider.sharedMesh = combinedMesh;



            current += count;
            chunkIndex++;
        }
    }
}
