using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class SpawnLootables : NetworkBehaviour
{
    [System.Serializable]
    public class SpawnLoot
    {
        public GameObject prebab;
        public Transform position;
    }

    public List<SpawnLoot> LootBoxList;
    void Start()
    {
        
    }
}