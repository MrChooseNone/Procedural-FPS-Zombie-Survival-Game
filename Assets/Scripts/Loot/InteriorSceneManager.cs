using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class InteriorSceneManager : NetworkBehaviour
{
    public static InteriorSceneManager Instance { get; private set; }
    public GameObject teleportBack;

    private HashSet<string> loadedScenes = new HashSet<string>();

    private void Awake()
    {
        // if another instance already exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Server]
    public void MovePlayerToInterior(NetworkIdentity conn, string sceneName)
    {
        StartCoroutine(LoadAndTeleport(conn, sceneName));
    }

    
    [Server]
    IEnumerator LoadAndTeleport(NetworkIdentity conn, string sceneName)
    {
        if (!loadedScenes.Contains(sceneName))
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadOp.isDone)
                yield return null;

            loadedScenes.Add(sceneName);
        }
        RpcLoadScene(conn.connectionToClient, conn, sceneName);
        yield return new WaitForSeconds(0.5f);
        RpcTeleportPlayer(conn.connectionToClient, conn);

        // Vector3 spawnPos = GameObject.FindWithTag("InteriorSpawn").transform.position;

        // // teleport on server
        // conn.transform.position = spawnPos;
        // NetworkTransform will replicate new position to that client

    }
    [TargetRpc]
    void RpcLoadScene(NetworkConnectionToClient target, NetworkIdentity networkIdentity, string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

    }

    [TargetRpc]
    void RpcTeleportPlayer(NetworkConnectionToClient target, NetworkIdentity networkIdentity)
    {
        Vector3 spawnPos = GameObject.FindWithTag("InteriorSpawn").transform.position;

        // teleport on server
        networkIdentity.transform.position = spawnPos;

    }

    [Server]
    public void MovePlayerBack(NetworkIdentity conn)
    {
        RpcTeleportPlayerBack(conn.connectionToClient, conn);
    }

    [TargetRpc]
    void RpcTeleportPlayerBack(NetworkConnectionToClient target, NetworkIdentity networkIdentity)
    {
        // teleport on server
        networkIdentity.transform.position = teleportBack.transform.position;

    }

}
