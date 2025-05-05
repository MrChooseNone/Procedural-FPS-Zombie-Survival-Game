using UnityEngine;
using Mirror;

public class Pushable : NetworkBehaviour
{
    public float pushForce = 5f;
    private RagdollController ragdollController;
    public bool isNpc;

    void Start()
    {
        ragdollController = GetComponent<RagdollController>();
    }

    [Command(requiresAuthority = false )]
    public void CmdApplyPush(Vector3 force)
    {
        RpcApplyPush(force);
    }

    
    void RpcApplyPush(Vector3 force)
    {
        if (ragdollController != null)
        {
            Debug.Log("cmd pushable enable ragdoll");
            ragdollController.CmdEnableRagdoll(force);
        }

        
    }
}
