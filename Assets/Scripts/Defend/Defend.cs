using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public enum NPCState { Stay, Follow, MoveToPosition }


public class DefendNPC : NetworkBehaviour
{
    [SyncVar]
    public NPCState currentState = NPCState.Stay;

    [SyncVar]
    public Vector3 targetPosition;

    [SyncVar]
    public NetworkIdentity followTarget;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        StartCoroutine(DelaySpawn(gameObject));


    }

    void Update()
    {
        if (!isServer) return; // Only let server control NPC behavior

        switch (currentState)
        {
            case NPCState.Stay:
                agent.isStopped = true;
                break;
            case NPCState.Follow:
                if (followTarget != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(followTarget.transform.position);
                }
                break;
            case NPCState.MoveToPosition:
                agent.isStopped = false;
                agent.SetDestination(targetPosition);
                break;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSetState(NPCState state, Vector3 position, NetworkIdentity player)
    {
        currentState = state;

        if (state == NPCState.Follow)
            followTarget = player;
        else if (state == NPCState.MoveToPosition)
            targetPosition = position;
    }

    public IEnumerator DelaySpawn(GameObject player){
        yield return new WaitForSeconds(3f);
        Terrain terrain = Terrain.activeTerrain;
        Vector3 pos = transform.position;
        pos.y = terrain.SampleHeight(pos) + terrain.GetPosition().y;
        transform.position = pos;
        player.transform.position = pos;
        

    }
}
