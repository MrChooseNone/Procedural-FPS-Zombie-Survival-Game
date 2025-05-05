using UnityEngine;
using Mirror;
using Steamworks;

public class playerObjectController : NetworkBehaviour
{
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;

    private CustomNetworkManager manager;
    private CustomNetworkManager Manager{
         get
        {
            if (manager == null)
                manager = NetworkManager.singleton as CustomNetworkManager;
            return manager;
        }
        
    }

    public override void OnStartAuthority(){
        //if (SteamManager.Initialized){

        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        //}
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();

    }
    public override void OnStopClient(){
        //if (Manager != null)
        //{
            Manager.GamePlayers.Remove(this);
        //}
        LobbyController.Instance.UpdatePlayerList();
    }
    public override void OnStartClient(){
    //    if (Manager != null)
    //     {
            Manager.GamePlayers.Add(this);
        //}
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }
    [Command]
    private void CmdSetPlayerName(string PlayerName){
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string OldValue, string NewValue){
        if(isServer){
            this.PlayerName = NewValue;

        }
        if(isClient){
            LobbyController.Instance.UpdatePlayerList();
        }
    }

}
