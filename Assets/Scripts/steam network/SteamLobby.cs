using UnityEngine;
using Mirror;
using Steamworks;

using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;
    
    private CustomNetworkManager networkManager;
    


    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    public ulong CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";

    private void Start(){


        if(!SteamManager.Initialized) {return ;}
        if(Instance == null) {Instance = this;}
        Debug.Log("Steam App ID: " + SteamUtils.GetAppID());

        networkManager = GetComponent<CustomNetworkManager>();

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby(){
        

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        Debug.Log("Joined lobby: " + callback.m_ulSteamIDLobby);
        
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        if (NetworkServer.active) {
            Debug.Log("You are the host.");
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        
        if (string.IsNullOrEmpty(hostAddress)) {
            Debug.LogError("Host address is empty! Something went wrong with lobby creation.");
            return;
        }

        Debug.Log("Connecting to host at: " + hostAddress);
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
    }


    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) {
            Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            return;
        }

        Debug.Log("Lobby successfully created with ID: " + callback.m_ulSteamIDLobby);

        networkManager.StartHost(); 

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        
        // Set the host's Steam ID in the lobby so others can join
        SteamMatchmaking.SetLobbyData(lobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(lobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
    }

}
