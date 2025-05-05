using Mirror;
using UnityEngine;

public class BuildingInteriorLink : NetworkBehaviour
{
    [SyncVar]
    public string assignedInteriorScene = "";

    private static readonly string[] interiorOptions = new string[]
    {
        "Interior_1"
    };

    private bool initialized = false;

    [Server]
    public void AssignInteriorIfNeeded()
    {
        if (initialized) return;

        assignedInteriorScene = interiorOptions[Random.Range(0, interiorOptions.Length)];
        initialized = true;
    }
}
