using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BoundingBoxManager : MonoBehaviour
{
    [Serializable]
    public class NominatimResult
    {
        public string display_name;
        public string[] boundingbox; // Array of strings: [minLat, maxLat, minLon, maxLon]
    }

    [Serializable]
    public class NominatimResultWrapper
    {
        public NominatimResult[] results;
    }

    public float areaSize = 0.000000001f; // Desired half-size of the bounding box in degrees
    // the bounding box cords
    public float MinLat;
    public float MaxLat;
    public float MinLon;
    public float MaxLon;
    public float latCenter;
    public float lonCenter;
    public MapzenTerrainLoader terrain;

    public float targetWidthInMeters = 10000f;  // Desired width in meters
    public float targetHeightInMeters = 10000f; // Desired height in meters
    public MapLoader mapLoader;

    private const string BaseUrl = "https://nominatim.openstreetmap.org/search";

    void Start()
    {
       
        string names = CityData.street + ", " + CityData.city;
        Debug.Log(names);
        // Search for a city (example: "New York")
        SearchCity(CityData.street + ", " + CityData.city);

        
    }

    public void SearchCity(string cityName)
    {
        string url = $"{BaseUrl}?q={UnityWebRequest.EscapeURL(cityName)}&format=json&polygon=1";
        Debug.Log(url);
        StartCoroutine(FetchCityData(url));
    }

    private IEnumerator FetchCityData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("User-Agent", "UnityApp/1.0 (contact@example.com)");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ParseCityData(jsonResponse);
                
            }
            else
            {
                Debug.LogError($"Error fetching data: {request.error}");
            }
        }
    }

    private void ParseCityData(string json)
    {
        try
        {
            // Wrap the array to parse it
            NominatimResult[] results = JsonUtility.FromJson<NominatimResultWrapper>($"{{\"results\":{json}}}").results;

            if (results.Length > 0)
            {
                NominatimResult firstResult = results[0];
                Debug.Log($"City: {firstResult.display_name}");

                // Extract the bounding box
                float minLat = float.Parse(firstResult.boundingbox[0]);
                float maxLat = float.Parse(firstResult.boundingbox[1]);
                float minLon = float.Parse(firstResult.boundingbox[2]);
                float maxLon = float.Parse(firstResult.boundingbox[3]);

                

                Debug.Log($"Original Bounding Box: Lat({minLat} - {maxLat}), Lon({minLon} - {maxLon})");

                // Center and resize the bounding box
                var adjustedBox = AdjustBoundingBox(minLat, maxLat, minLon, maxLon, targetWidthInMeters, targetHeightInMeters);
                Debug.Log($"Adjusted Bounding Box: Lat({adjustedBox.minLat} - {adjustedBox.maxLat}), Lon({adjustedBox.minLon} - {adjustedBox.maxLon})");
                MinLat = adjustedBox.minLat;
                MaxLat = adjustedBox.maxLat;
                MinLon = adjustedBox.minLon;
                MaxLon = adjustedBox.maxLon;
                

                int xMin = mapLoader.LonToTileX(MinLon);
                int xMax = mapLoader.LonToTileX(MaxLon);
                int yMin = mapLoader.LatToTileY(MaxLat); // top
                int yMax = mapLoader.LatToTileY(MinLat); // bottom

                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        Debug.Log("find tile ");
                        StartCoroutine(mapLoader.DownloadAndPlaceTile(x, y, x - xMin, y - yMin));
                    }
                }
            }
            else
            {
                Debug.LogError("No results found!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
        }
    }

private BoundingBox AdjustBoundingBox(float minLat, float maxLat, float minLon, float maxLon, float targetWidthInMeters, float targetHeightInMeters)
{
    latCenter = (minLat + maxLat) / 2;
    lonCenter = (minLon + maxLon) / 2;
    if(terrain != null){
        terrain.GetElevation(latCenter, lonCenter);
    }

    // Calculate the degree change for the desired width and height in meters
    float latDegreeSize = targetHeightInMeters / 111000f; // 1 degree latitude ≈ 111 km
    float lonDegreeSize = targetWidthInMeters / (111000f * Mathf.Cos(latCenter * Mathf.Deg2Rad)); // Longitude size varies with latitude
    
    // Adjust the bounding box using the calculated degree sizes
    return new BoundingBox
    {
        minLat = latCenter - latDegreeSize / 2f,
        maxLat = latCenter + latDegreeSize / 2f,
        minLon = lonCenter - lonDegreeSize / 2f,
        maxLon = lonCenter + lonDegreeSize / 2f
    };
}

public void SendMapCords(double lat, double lon, int zoom){
   
    // Convert longitude to tile X
    int x = (int)Math.Floor((lon + 180.0) / 360.0 * (1 << zoom));
        
    // Convert latitude to tile Y
    double latRad = lat * Math.PI / 180.0; // Convert latitude to radians
    int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom));
    
}



    public class BoundingBox
    {
        public float minLat;
        public float maxLat;
        public float minLon;
        public float maxLon;
    }
}
