using System;
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;
using MapMaker;


public class OSMDataLoader : MonoBehaviour
{
    public int zoomLevel = 15;
    public int tileX = 5235;
    public int tileY = 12667;
    public string xmlFilePath = "path/to/osm_data.osm"; // Path to your .osm file
    public string myDataPath;
    
    //public string pbfFilePath = "path/to/osm_data.pbf"; // Path to save the .pbf file
    public float minLat;
    public float maxLat;
    public float minLon;
    public float maxLon;
    
    [HideInInspector]
    private OsmBounds osmBound;
    public BoundingBoxManager boundingBox;
    public MapLoader mapLoader;

    void Start()
    {

        // Ensure the save path is valid
        myDataPath = Path.Combine(Application.persistentDataPath, xmlFilePath);
       
        //osmBound = new OsmBounds(); 

        var bounds = TileToLatLon.GetTileBoundingBox(zoomLevel, tileX, tileY);
        // minLat = float.Parse(bounds.minLat.Replace('.', ','));
        // maxLat = float.Parse(bounds.maxLat.Replace('.', ','));
        // minLon = float.Parse(bounds.minLon.Replace('.', ','));
        // maxLon = float.Parse(bounds.maxLon.Replace('.', ','));
        // osmBound.SendToOsm(minLat, maxLat, minLon, maxLon);

        File.Delete((string)myDataPath);

        print(bounds.minLat);
        boundingBox = FindAnyObjectByType<BoundingBoxManager>();
        mapLoader = FindAnyObjectByType<MapLoader>();
        
        if (boundingBox != null)
        {
            StartCoroutine(CheckBoundingBoxAndCreateURL());
        }
        else
        {
            Debug.LogError("BoundingBoxManager not found.");
        }
    }

// Coroutine to check if bounding box is initialized and then create the URL
IEnumerator CheckBoundingBoxAndCreateURL()
{
    for (int i = 0; i < 20; i++)
    {
        // Wait for the bounding box to be initialized (non-zero values)
        if (boundingBox.MinLat != 0)
        {
            CreateURL();
            yield break; // Exit the coroutine once the URL is created
        }
        else
        {
            Debug.Log($"Waiting for bounding box to initialize... Attempt {i + 1}");
            yield return new WaitForSeconds(1); // Wait for 1 second before checking again
        }
    }
    Debug.LogError("Bounding box was not initialized after 20 attempts.");
}
    void CreateURL(){
        string queryUrl = $"https://overpass-api.de/api/map?bbox={boundingBox.MinLon},{boundingBox.MinLat},{boundingBox.MaxLon},{boundingBox.MaxLat}";
        print(queryUrl);
        StartCoroutine(FetchOSMData(queryUrl));
    }


    IEnumerator FetchOSMData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string xml = request.downloadHandler.text;
                Debug.Log($"OSM data fetched successfully. Size: {xml.Length} bytes");
                SaveOSMDataToFile(xml, myDataPath);
            }
            else
            {
                Debug.LogError($"Failed to load OSM data. Error: {request.error}");
            }
        }
    }

    void SaveOSMDataToFile(string data, string filePath)
    {
        try
        {
            File.WriteAllText(filePath, data, System.Text.Encoding.UTF8);
            Debug.Log($"OSM data saved to {filePath}");
            
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving OSM data to {filePath}: {e.Message}");
        }
    }


}

