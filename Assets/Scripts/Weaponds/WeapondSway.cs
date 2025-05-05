using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using Mirror;

public class WeaponSway : NetworkBehaviour
{
    public float intensity = 100f; // Control how far the weapon moves
    public float smooth = 6f;     // Smooth factor (higher value = slower, smoother)
    public float sensitivity = 1f; // Sensitivity factor to control how much mouse movement affects sway
    private Quaternion targetRotation;
    private Quaternion originRotation;
    

    void Start()
    {
        originRotation = transform.localRotation;
    }

    void Update()
    {
        if(!isOwned) return;
        
        if (Mouse.current == null) return;

        // Get mouse movement using the new Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Adjust mouse movement by sensitivity to control sway intensity
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        // Calculate rotation adjustments for the weapon sway
        Quaternion xAdj = Quaternion.AngleAxis(-intensity * mouseX, Vector3.up);
        Quaternion yAdj = Quaternion.AngleAxis(intensity * mouseY, Vector3.right);
        

        // Apply the target rotation with adjustments
        targetRotation = originRotation * xAdj * yAdj;

        // Smooth the transition between current rotation and target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
    }
}
