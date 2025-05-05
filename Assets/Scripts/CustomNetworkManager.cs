using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Steamworks;
public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private playerObjectController GamePlayerPrefab;

    public List<playerObjectController> GamePlayers {get;} = new List<playerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            Debug.Log("added a player" + conn.identity);
            if (conn.identity != null)
            {
                // Player already exists for this connection
                Debug.Log("There is already a player for this connection.");
                return;
            }
            playerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);
            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count +1;
            

            // Ensure SteamLobby Instance is valid before accessing Steam data
            if (SteamLobby.Instance != null && SteamLobby.Instance.CurrentLobbyID != 0)
            {
                GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
                    (CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count
                );
            }

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
            

            DontDestroyOnLoad(GamePlayerInstance.gameObject); // Ensure persistence across scenes
            
        }
    }

    public void StartGame()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            ServerChangeScene("Game"); // Make sure "Game" is added in Build Settings
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName == "Game")
        {
            Terrain terrain = Terrain.activeTerrain; // Get terrain (if exists)

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    GameObject player = conn.identity.gameObject;
                    player.SetActive(true); // Enable player

                    if (terrain != null)
                    {
                        Vector3 pos = player.transform.position;
                        pos.y = terrain.SampleHeight(pos) + 10f; // Ensure proper Y position
                        player.transform.position = pos;
                    }
                }
                else
                {
                    GameObject newPlayer = Instantiate(playerPrefab);
                    NetworkServer.AddPlayerForConnection(conn, newPlayer);
                }
            }

            // Move players to the game scene
            foreach (playerObjectController player in GamePlayers)
            {
                SceneManager.MoveGameObjectToScene(player.gameObject, SceneManager.GetSceneByName(sceneName));
            }
        }
    }

    // public override void OnStopServer()
    // {
    //     base.OnStopServer();

    //     // Clear the list of players when the server stops
    //     GamePlayers.Clear();

    //     // Optionally, you can also destroy any remaining game objects or do additional cleanup
    //     foreach (var player in GamePlayers)
    //     {
    //         Destroy(player.gameObject);
    //     }

    //     Debug.Log("Server stopped and players cleared.");
    // }

}
