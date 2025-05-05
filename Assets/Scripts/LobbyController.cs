using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using Edgegap.Editor.Api.Models.Requests;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    public TextMeshProUGUI LobbyNameText;
    public TextMeshProUGUI startButtonText;

    public GameObject PlayerViewContent;
    public GameObject PlayerListPrefab;
    public GameObject LocalPlayerObject;

    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    private List<playerListItem> PlayerListItem = new List<playerListItem>();
    public playerObjectController LocalPlayerController;

    private CustomNetworkManager manager;

    private CustomNetworkManager Manager{

        get{
            if(manager != null){
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Awake(){
        if(Instance == null){Instance = this;} else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public void UpdateLobbyName(){
        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
    }

    public void UpdatePlayerList(){
        Debug.Log("the number of players: " + Manager.GamePlayers.Count);
        if(!PlayerItemCreated) {CreatHostPlayerItem();}
        if(PlayerListItem.Count < Manager.GamePlayers.Count) {CreateClientPlayerItem();}
        if(PlayerListItem.Count > Manager.GamePlayers.Count) {RemovePlayerItem();}
        if(PlayerListItem.Count == Manager.GamePlayers.Count) {UpdatePlayerItem();}
    }

    public void FindLocalPlayer(){
        if(SceneManager.GetActiveScene().name == "Game"){

            LocalPlayerObject = GameObject.Find("LocalGamePlayer");
            
            LocalPlayerController = LocalPlayerObject.GetComponent<playerObjectController>();
        }
    }

    private void UpdatePlayerItem()
    {
        foreach (playerObjectController player in Manager.GamePlayers){
            foreach(playerListItem PlayerListItemScript in PlayerListItem){
                if(PlayerListItemScript.ConnectionID == player.ConnectionID){
                    PlayerListItemScript.PlayerName = player.PlayerName;
                    PlayerListItemScript.SetPlayerValues();
                }
            }
        }
    }

    private void RemovePlayerItem()
    {
        List<playerListItem> playerListItemToRemove = new List<playerListItem>();

        foreach ( playerListItem playeritemlist in PlayerListItem){
             if(!Manager.GamePlayers.Any(b => b.ConnectionID == playeritemlist.ConnectionID)){ 
                playerListItemToRemove.Add(playeritemlist);
             }
        }
        if(playerListItemToRemove.Count > 0){
            foreach(playerListItem playerlistItemToRemove in playerListItemToRemove){
                GameObject ObjectToRemove = playerlistItemToRemove.gameObject;
                PlayerListItem.Remove(playerlistItemToRemove);
                Destroy(ObjectToRemove);
                ObjectToRemove = null;
            }
        }
    }

    private void CreatHostPlayerItem()
    {
        Debug.Log("cretaed host");
        foreach (playerObjectController player in Manager.GamePlayers){

            GameObject NewPlayerItem = Instantiate(PlayerListPrefab) as GameObject;
            playerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<playerListItem>();

            NewPlayerItemScript.PlayerName = player.PlayerName;
            NewPlayerItemScript.ConnectionID = player.ConnectionID;
            NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
            NewPlayerItemScript.SetPlayerValues();

            NewPlayerItem.transform.SetParent(PlayerViewContent.transform);
            NewPlayerItem.transform.localScale = Vector3.one;

            PlayerListItem.Add(NewPlayerItemScript);
            player.gameObject.SetActive(false);
        }
        PlayerItemCreated = true;
    }
    public void CreateClientPlayerItem(){
        Debug.Log("cretaed client");
        foreach (playerObjectController player in Manager.GamePlayers){
            if(!PlayerListItem.Any(b => b.ConnectionID == player.ConnectionID)){
                GameObject NewPlayerItem = Instantiate(PlayerListPrefab) as GameObject;
                playerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<playerListItem>();

                NewPlayerItemScript.PlayerName = player.PlayerName;
                NewPlayerItemScript.ConnectionID = player.ConnectionID;
                NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
                NewPlayerItemScript.SetPlayerValues();

                NewPlayerItem.transform.SetParent(PlayerViewContent.transform);
                NewPlayerItem.transform.localScale = Vector3.one;

                PlayerListItem.Add(NewPlayerItemScript);
                player.gameObject.SetActive(false);
            }
        }
    }
    
}
