using System.Collections;
using MicahW.PointGrass;
using Mirror;
using UnityEngine;

class GrassManager : NetworkBehaviour
{
    public LoadingScreenManager loading;
    public bool isFinished = false;
    PointGrassRenderer grassRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        if(isLocalPlayer){
            GameObject playerObject = this.gameObject;
            PointGrassRenderer grassRenderer = Terrain.activeTerrain.GetComponent<PointGrassRenderer>();
            // MapzenTerrainLoader terrainLoader = FindFirstObjectByType<MapzenTerrainLoader>();

            if(grassRenderer !=null){
                Debug.Log("yey we got the grass renderer");
                grassRenderer.playerTransform = playerObject.transform;
            }

            yield return new WaitForSeconds(3f);
            
        }
        //yield return null;
        
    }
    public void spawnGrass(){   
        GameObject playerObject = this.gameObject;
        PointGrassRenderer grassRenderer = Terrain.activeTerrain.GetComponent<PointGrassRenderer>();
        // MapzenTerrainLoader terrainLoader = FindFirstObjectByType<MapzenTerrainLoader>();

        if(grassRenderer !=null){
            Debug.Log("yey we got the grass renderer");
            grassRenderer.playerTransform = playerObject.transform;
        }
        grassRenderer.terrain = Terrain.activeTerrain.terrainData;
        grassRenderer.BuildGrass();
            
        isFinished = true;
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
