using UnityEngine;
using Mirror;
public class Playernetworkingmovment : NetworkBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        if (isLocalPlayer) {  // Ensure only the local player moves

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        transform.position += move;
        }
    }
}
