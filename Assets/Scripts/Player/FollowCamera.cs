using UnityEngine;

public class WeaponIKFollowCamera : MonoBehaviour
{
    public Transform cameraTransform;  // Assign your camera (e.g., Camera.main.transform)
    public Transform ikTarget;         // Assign the IK target (holder)

    public Vector3 positionOffset = new Vector3(0.2f, -0.2f, 0.5f); // Adjust this to position the weapon correctly
    public Vector3 rotationOffset = new Vector3(0, 0, 0); // Adjust to match hand rotation

    void Update()
    {
        if (cameraTransform && ikTarget)
        {
            // Set IK Target position relative to the camera
            ikTarget.position = cameraTransform.position + cameraTransform.rotation * positionOffset;

            // Set IK Target rotation relative to the camera
            ikTarget.rotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}



