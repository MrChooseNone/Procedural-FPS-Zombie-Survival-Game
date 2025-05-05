using System.Collections;
using MicahW.PointGrass;
using Mirror;
using UnityEngine;

public class GrassManager : NetworkBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        if(isLocalPlayer){
            GameObject playerObject = this.gameObject;
            PointGrassRenderer grassRenderer = Terrain.activeTerrain.GetComponent<PointGrassRenderer>();
            MapzenTerrainLoader terrainLoader = FindFirstObjectByType<MapzenTerrainLoader>();

            if(grassRenderer !=null){
                Debug.Log("yey we got the grass renderer");
                grassRenderer.playerTransform = playerObject.transform;
            }
            yield return new WaitForSeconds(10f);
            grassRenderer.terrain = Terrain.activeTerrain.terrainData;
            grassRenderer.BuildGrass();
        }
        //yield return null;
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
