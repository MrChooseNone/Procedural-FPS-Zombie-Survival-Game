using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class InteriorSceneManager : NetworkBehaviour
{
    [System.Serializable]
    public class RoomCategoryPrefabs
    {
        public InteriorCategory category;
        public List<GameObject> prefabs;
    }

    class SpawnedRoom
    {
        public GameObject instance;
        public Vector3 position;
        public InteriorCategory category;
        public HashSet<NetworkIdentity> playersInside = new();
        public float lastEmptyTime = -1f;
    }

    List<SpawnedRoom> activeRooms = new();
    public int maxActiveRooms = 5;
    public float roomSpacing = 100f;  // vertical distance between rooms


    public List<RoomCategoryPrefabs> roomCategories;

    private Dictionary<InteriorCategory, List<GameObject>> prefabLookup;

    public static InteriorSceneManager Instance { get; private set; }
    public GameObject teleportBack;

    // private HashSet<string> loadedScenes = new HashSet<string>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize prefabLookup
        prefabLookup = new Dictionary<InteriorCategory, List<GameObject>>();
        foreach (var categorySet in roomCategories)
        {
            if (!prefabLookup.ContainsKey(categorySet.category))
            {
                prefabLookup[categorySet.category] = new List<GameObject>();
            }

            prefabLookup[categorySet.category].AddRange(categorySet.prefabs);
        }
    }


    [ServerCallback]
    void Update()
    {
        float now = Time.time;
        float cleanupDelay = 15f;

        for (int i = activeRooms.Count - 1; i >= 0; i--)
        {
            var room = activeRooms[i];
            if (room.playersInside.Count == 0 && room.lastEmptyTime > 0f && now - room.lastEmptyTime > cleanupDelay)
            {
                NetworkServer.Destroy(room.instance);
                activeRooms.RemoveAt(i);
            }
        }
    }


    [Server]
    public void MovePlayerToInterior(NetworkIdentity conn, LootableObject door)
    {
        StartCoroutine(LoadAndTeleport(conn, door));
    }


    [Server]
    IEnumerator LoadAndTeleport(NetworkIdentity conn, LootableObject door)
    {
        // Remove excess rooms
        if (activeRooms.Count >= maxActiveRooms)
        {
            SpawnedRoom oldest = activeRooms[0];
            NetworkServer.Destroy(oldest.instance);
            activeRooms.RemoveAt(0);
        }

        if (!prefabLookup.TryGetValue(door.category, out var prefabList) || prefabList == null || prefabList.Count == 0)
        {
            Debug.LogWarning($"No prefabs found for category {door.category}");
            yield break;
        }

        GameObject prefab = prefabList[Random.Range(0, prefabList.Count)];


        // Calculate a new Y-position below the map
        Vector3 position = new Vector3(0, -roomSpacing * (activeRooms.Count + 1), 0);

        // Spawn and position
        GameObject newRoom = Instantiate(prefab, position, Quaternion.identity);
        NetworkServer.Spawn(newRoom);
        door.SetLinkedRoom(newRoom);
        SpawnLootables(newRoom);

        yield return new WaitForSeconds(0.5f); // wait to spawn before teleporting

        // Get spawn point inside room
        Vector3 spawnPos = newRoom.GetComponentInChildren<LocationScript>().transform.position;
        conn.transform.position = spawnPos;

        // Track room
        var room = new SpawnedRoom
        {
            instance = newRoom,
            position = position,
            category = door.category
        };
        room.playersInside.Add(conn);
        activeRooms.Add(room);

    }

    [Server]
    public void SpawnLootables(GameObject newRoom)
    {
        SpawnLootables spawnLootables = newRoom.GetComponent<SpawnLootables>();
        if(spawnLootables != null){
            foreach (var item in spawnLootables.LootBoxList)
            {
                GameObject loot = Instantiate(item.prebab, item.position.position, item.position.rotation);
                NetworkServer.Spawn(loot);
            }
        }
    }

    [Server]
    public void TeleportPlayerToRoom(NetworkIdentity player, GameObject newRoom)
    {
        Vector3 spawnPos = newRoom.GetComponentInChildren<LocationScript>().transform.position;
        player.transform.position = spawnPos;
    }

    [Server]
    public void PlayerEnteredRoom(NetworkIdentity player, GameObject roomObj)
    {
        foreach (var room in activeRooms)
        {
            if (room.instance == roomObj)
            {
                room.playersInside.Add(player);
                room.lastEmptyTime = -1f;
            }
        }
    }

    [Server]
    public void PlayerExitedRoom(NetworkIdentity player, GameObject roomObj)
    {
        foreach (var room in activeRooms)
        {
            if (room.instance == roomObj)
            {
                room.playersInside.Remove(player);
                if (room.playersInside.Count == 0)
                    room.lastEmptyTime = Time.time;
            }
        }
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
