
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MapLoader : MonoBehaviour
{
    public string tileURL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png"; // OSM tile server
    public int zoomLevel = 15;
    public Vector2Int tileCoords = new Vector2Int(5235, 12667); // Set your tile X and Y coordinates
    public int tileSize = 256; // pixels
    public GameObject mapParent; // assign in inspector
    public GameObject MapTexture;
    public GameObject mask;


    void Start()
    {
        // LoadTile(tileCoords.x, tileCoords.y);
    }

    public void LoadTile(int x, int y)
    {
        string url = tileURL.Replace("{z}", zoomLevel.ToString())
                            .Replace("{x}", x.ToString())
                            .Replace("{y}", y.ToString());
        StartCoroutine(FetchTile(url));
        print(url);
    }

    IEnumerator FetchTile(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.mainTexture = texture;
        }
        else
        {
            Debug.LogError($"Failed to load tile from {url}. Error: {request.error}");
        }
    }
    public IEnumerator DownloadAndPlaceTile(int x, int y, int gridX, int gridY)
    {
        string tileUrl = $"https://tile.openstreetmap.org/{zoomLevel}/{x}/{y}.png";
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(tileUrl);
        yield return request.SendWebRequest();
        print(tileUrl);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D tileTexture = DownloadHandlerTexture.GetContent(request);
            
            GameObject tileGO = new GameObject($"Tile_{x}_{y}" + Random.value);
            tileGO.transform.SetParent(mapParent.transform); // create an empty GameObject called mapParent to hold them

            // For UI (Canvas): use RawImage
            RawImage image = tileGO.AddComponent<RawImage>();
            image.texture = tileTexture;
            image.rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
            image.rectTransform.anchoredPosition = new Vector2(gridX * tileSize -128, -gridY * tileSize +128); // top-left to bottom-right

            GameObject texture = Instantiate(MapTexture);
            texture.transform.SetParent(tileGO.transform);
            texture.transform.localPosition = Vector3.zero;
            texture.transform.localScale = new Vector3(2.552302f,2.552302f,2.552302f);

            tileGO.transform.SetParent(mask.transform);

            // Optional: scale for zoom or other styling
        }
        else
        {
            Debug.LogWarning($"Failed to download tile {x}, {y}: {request.error}");
        }
    }

    public int LonToTileX(double lon)
    {
        return (int)((lon + 180.0) / 360.0 * (1 << zoomLevel));
    }

    public int LatToTileY(double lat)
    {
        double latRad = lat * Mathf.Deg2Rad;
        return (int)((1.0 - Mathf.Log((float)(Mathf.Tan((float)latRad) + 1.0 / Mathf.Cos((float)latRad))) / Mathf.PI) / 2.0 * (1 << zoomLevel));
    }


}

