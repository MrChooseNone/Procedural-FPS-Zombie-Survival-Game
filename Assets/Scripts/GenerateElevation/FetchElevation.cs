using UnityEngine;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using System;

public class MapzenTerrainLoader : MonoBehaviour
{
    //string tileUrl = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/14/8710/5515.png";

    public Terrain terrain;
    
    // Coordinates for which to fetch terrain tile (Longitude, Latitude)
    public float longitude = 18.6997161f; 
    public float latitude = 59.7610124f;

    public int zoomLevel = 12;  // Adjust the zoom level (typically 14 to 16 for detailed terrain)

    // URL template for Mapzen (or other provider)
    private string tileUrlTemplate = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png";
    public bool IsTerrain = false;
    TerrainData terrainDataNew;
    float minElevation = -100;  // Adjust based on real-world data
    float maxElevation = 3000;  // Adjust based on highest terrain
    public TerrainLayer[] groundTexture;

   


    // public async void GetElevation(float centerLat, float centerLon)
    // {
        
        
    //     // Convert Longitude/Latitude to Tile Coordinates (X, Y)
    //     int tileX = long2tilex(centerLon, zoomLevel);
    //     int tileY = lat2tiley(centerLat, zoomLevel);

    //     // Build the URL for the tile
    //     string tileUrl = string.Format(tileUrlTemplate, zoomLevel, tileX, tileY);
    //     Debug.Log(tileUrl);
    //     Texture2D heightmap = await GetHeightmap(tileUrl);
    //     ApplyHeightmapToTerrain(heightmap);
    // }

    async Task<Texture2D> GetHeightmap(string tileURL)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(tileURL);
            if (response.IsSuccessStatusCode)
            {
                //Debug.Log("Elevation was succesfull: " + response);
                byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                Texture2D texture = new Texture2D(256, 256);
                texture.LoadImage(imageData);
                return texture;
            }
        }
        return null;
    }
    // Convert longitude to tile X coordinate at a given zoom level
    int long2tilex(double lon, int z)
    {
        return (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
    }

    int lat2tiley(double lat, int z)
    {
        var latRad = lat / 180 * Math.PI;
        return (int)Math.Floor((1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2 * (1 << z));
    }

    double tilex2long(int x, int z)
    {
        return x / (double)(1 << z) * 360.0 - 180;
    }

    double tiley2lat(int y, int z)
    {
        double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
        return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    }

    // void ApplyHeightmapToTerrain(Texture2D texture)
    // {
    //     TerrainData terrainDataNew = terrain.terrainData;
    //     int size = terrainDataNew.heightmapResolution;
    //     Debug.Log("hight resolution " + terrainDataNew.heightmapResolution);
    //     float[,] heights = new float[size, size];
    //     Debug.Log("size: " + size);
    //     Debug.Log("size existing: " + terrain.terrainData.heightmapResolution);

    //     float minElevation = -100;  // Adjust based on real-world data
    //     float maxElevation = 3000;  // Adjust based on highest terrain

    //     for (int x = 0; x < size; x++)
    //     {
    //         for (int y = 0; y < size; y++)
    //         {
    //             Color pixel = texture.GetPixel(x * texture.width / size, y * texture.height / size);
    //             float red = pixel.r * 255;
    //             float green = pixel.g * 255;
    //             float blue = pixel.b * 255;

    //             //Debug.Log("pixel " + pixel);
    //             float elevation = (red * 256 + green + (blue / 256)) - 32768;
    //             //Debug.Log("elevation " + elevation);
    //             heights[y, x] = Mathf.Clamp(elevation / 5000f, 0, 1); // Normalize for Unity
    //             //heights[y, x] = Mathf.InverseLerp(minElevation, maxElevation, elevation);
    //         }
    //     }
    //     //Debug.Log("the heights are: " + heights);
        
    //     terrainDataNew.SetHeights(0, 0, heights);
    //     Debug.Log(terrainDataNew.size);

    //     terrain.terrainData = terrainDataNew;
    //     IsTerrain = true;
    // }

    public async void GetElevation(float centerLat, float centerLon)
    {
        int tileX = long2tilex(centerLon, zoomLevel);
        int tileY = lat2tiley(centerLat, zoomLevel);

        // compute corner lats/lons
        double lonL = tilex2long(tileX,   zoomLevel);
        double lonR = tilex2long(tileX+1, zoomLevel);
        double latT = tiley2lat(tileY,   zoomLevel);
        double latB = tiley2lat(tileY+1, zoomLevel);

        float terrainWidth  = (float)HaversineDistanceMeters(latT, lonL, latT, lonR);
        float terrainLength = (float)HaversineDistanceMeters(latT, lonL, latB, lonL);
        float terrainHeight = maxElevation - minElevation;  // e.g. 3100 if your range is –100…3000
        string tileUrl = string.Format(tileUrlTemplate, zoomLevel, tileX, tileY);
        Debug.Log(tileUrl);
        Texture2D heightmap = await GetHeightmap(tileUrl);
        ApplyHeightmapToTerrain(heightmap, terrainWidth, terrainHeight, terrainLength);
    }

void ApplyHeightmapToTerrain(Texture2D texture, float terrainWidth, float terrainHeight, float terrainLength)
{
    TerrainData td = new TerrainData();
    // copy the resolution settings from the existing terrain
    td.heightmapResolution = terrain.terrainData.heightmapResolution;
    td.alphamapResolution = terrain.terrainData.alphamapResolution;
    td.baseMapResolution = terrain.terrainData.baseMapResolution;
    td.SetDetailResolution(terrain.terrainData.detailResolution, terrain.terrainData.detailPatchCount);

    // **set the real‐world size** before you set heights
    td.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
    

    // fill heights exactly as before
    int size = td.heightmapResolution;
    float[,] heights = new float[size, size];
    for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            Color p = texture.GetPixel(x * texture.width / size, y * texture.height / size);
            float elev = (p.r*255*256 + p.g*255 + p.b) - 32768;
            heights[y, x] = Mathf.InverseLerp(minElevation, maxElevation, elev);
        }
    td.SetHeights(0, 0, heights);

    // now assign the brand‑new TerrainData
    terrain.terrainData = td;
    var tc = terrain.GetComponent<TerrainCollider>();
    
    terrain.terrainData.terrainLayers = groundTexture;
    tc.terrainData = td;         // collider now uses the same new data

    Debug.Log($"Terrain size now = {td.size} (m)");
    IsTerrain = true;
}

    static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
{
    const double R = 6371000; // earth radius in m
    double dLat = (lat2 - lat1) * Math.PI/180;
    double dLon = (lon2 - lon1) * Math.PI/180;
    double a = Math.Sin(dLat/2)*Math.Sin(dLat/2) +
               Math.Cos(lat1*Math.PI/180)*Math.Cos(lat2*Math.PI/180) *
               Math.Sin(dLon/2)*Math.Sin(dLon/2);
    double c = 2*Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
    return R * c;
}

}
