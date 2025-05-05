using Mirror;
using UnityEngine;


public class PlayerCommander : NetworkBehaviour
{
    public KeyCode stayKey = KeyCode.Alpha1;
    public KeyCode followKey = KeyCode.Alpha2;
    public KeyCode moveKey = KeyCode.Alpha3;

    public DefendNPC targetNPC;
    public Camera playerCamera;

    void Start()
    {
        targetNPC = GameObject.FindGameObjectWithTag("Defend").GetComponent<DefendNPC>();
    }

    void Update()
    {
        
        if (!isLocalPlayer || targetNPC == null) return;

        if (Input.GetKeyDown(stayKey))
        {
            targetNPC.CmdSetState(NPCState.Stay, Vector3.zero, null);
        }

        if (Input.GetKeyDown(followKey))
        {
            targetNPC.CmdSetState(NPCState.Follow, Vector3.zero, netIdentity);
        }

        if (Input.GetKeyDown(moveKey))
        {
            Vector3 point;
            if (GetPointOnGround(out point))
            {
                targetNPC.CmdSetState(NPCState.MoveToPosition, point, null);
            }
        }
    }

    bool GetPointOnGround(out Vector3 point)
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            point = hit.point;
            return true;
        }

        point = Vector3.zero;
        return false;
    }
}
