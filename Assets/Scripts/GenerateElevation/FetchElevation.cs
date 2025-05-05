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

   


    public async void GetElevation(float centerLat, float centerLon)
    {
        
        
        // Convert Longitude/Latitude to Tile Coordinates (X, Y)
        int tileX = long2tilex(centerLon, zoomLevel);
        int tileY = lat2tiley(centerLat, zoomLevel);

        // Build the URL for the tile
        string tileUrl = string.Format(tileUrlTemplate, zoomLevel, tileX, tileY);
        Debug.Log(tileUrl);
        Texture2D heightmap = await GetHeightmap(tileUrl);
        ApplyHeightmapToTerrain(heightmap);
    }

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

    void ApplyHeightmapToTerrain(Texture2D texture)
    {
        TerrainData terrainDataNew = terrain.terrainData;
        int size = terrainDataNew.heightmapResolution;
        //Debug.Log("hight resolution " + terrainDataNew.heightmapResolution);
        float[,] heights = new float[size, size];

        float minElevation = -100;  // Adjust based on real-world data
        float maxElevation = 3000;  // Adjust based on highest terrain

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Color pixel = texture.GetPixel(x * texture.width / size, y * texture.height / size);
                float red = pixel.r * 255;
                float green = pixel.g * 255;
                float blue = pixel.b * 255;

                //Debug.Log("pixel " + pixel);
                float elevation = (red * 256 + green + (blue / 256)) - 32768;
                //Debug.Log("elevation " + elevation);
                heights[y, x] = Mathf.Clamp(elevation / 5000f, 0, 1); // Normalize for Unity
                heights[y, x] = Mathf.InverseLerp(minElevation, maxElevation, elevation);
            }
        }
        //Debug.Log("the heights are: " + heights);

        terrainDataNew.SetHeights(0, 0, heights);
        terrain.terrainData = terrainDataNew;
        IsTerrain = true;
    }
}
