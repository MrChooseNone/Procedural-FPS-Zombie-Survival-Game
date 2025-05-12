using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Collections;
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
            Vector3 pos = Vector3.zero;
            GameObject player = null;

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    player = conn.identity.gameObject;
                    player.SetActive(true); // Enable player
                    LoadingScreenManager loading = player.GetComponent<LoadingScreenManager>();
                    if(loading != null){
                        loading.ShowLoadingScreen();
                    }
                    
                    if (terrain != null)
                    {
                        pos = player.transform.position;
                        pos.y = terrain.SampleHeight(pos) + 500f; // Ensure proper Y position
                        player.transform.position = pos;  
                    }
                }
                else
                {
                    GameObject newPlayer = Instantiate(playerPrefab);
                    NetworkServer.AddPlayerForConnection(conn, newPlayer);
                    LoadingScreenManager loading = newPlayer.GetComponent<LoadingScreenManager>();
                    if(loading != null){
                        loading.ShowLoadingScreen();
                    }
                    
                }
            }

            // Move players to the game scene
            foreach (playerObjectController players in GamePlayers)
            {
                SceneManager.MoveGameObjectToScene(players.gameObject, SceneManager.GetSceneByName(sceneName));
            }
            //StartCoroutine(PositionPlayersOnTerrain(sceneName));
        }
    }

    // public IEnumerator DelaySpawn(GameObject player, Vector3 pos){
    //     yield return new WaitForSeconds(3f);
    //     player.transform.position = pos;
    //     

    // }

    // private IEnumerator PositionPlayersOnTerrain(string sceneName)
    // {
    //     // Wait until Terrain is loaded (check each frame)
    //     Terrain terrain = null;
    //     while (terrain == null)
    //     {
    //         terrain = Terrain.activeTerrain;
    //         yield return null; // wait 1 frame
    //     }

    //     // Optional: wait an extra frame to ensure stability
    //     yield return new WaitForSeconds(3f);

    //     // Position players
    //     foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
    //     {
    //         if (conn.identity != null)
    //         {
    //             Debug.Log("existing player");
    //             GameObject player = conn.identity.gameObject;
    //             player.SetActive(true); // Enable player

    //             Vector3 pos = player.transform.position;
    //             pos.y = terrain.SampleHeight(pos) + 1f; // add small offset above ground
    //             player.transform.position = pos;
    //             Animator[] animators = player.GetComponentsInChildren<Animator>();
    //             foreach(Animator animator in animators){

    //                 if (animator != null)
    //                 {
    //                     animator.Rebind();
    //                     animator.Update(0f);
    //                 }
    //             }

    //             // Move to correct scene (important for additive scene support)
    //             SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByName(sceneName));
    //         }
    //         else
    //         {
    //             GameObject newPlayer = Instantiate(playerPrefab);
    //             newPlayer.SetActive(true); // Enable player
    //             Debug.Log("new player");
    //             Vector3 pos = newPlayer.transform.position;
    //             pos.y = terrain.SampleHeight(pos) + 1f;
    //             newPlayer.transform.position = pos;
    //             Animator[] animators = newPlayer.GetComponentsInChildren<Animator>();
    //             foreach(Animator animator in animators){
    //                 if (animator != null)
    //                 {
    //                     animator.Rebind();
    //                     animator.Update(0f);
    //                 }
    //             }

    //             NetworkServer.AddPlayerForConnection(conn, newPlayer);
    //         }
    //     }
    // }


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
