using UnityEngine;
using Mirror;

public class BuildingDifficult : NetworkBehaviour
{
    public MeshRenderer buildingRenderer;
    public BoxCollider colliderDiff;

    [SyncVar]
    public int difficultyLevel = 1;


    [Server]
    public void CalculateDifficulty()
    {
        if (buildingRenderer == null)
        {
            Debug.LogWarning("No renderer assigned to building.");
            return;
        }

        Vector3 size = buildingRenderer.bounds.size;
        float volume = size.x * size.y * size.z;
       
        Vector3 worldSize = buildingRenderer.bounds.size;

        // Add padding to each axis (e.g., +1 unit or +10%)
        float padding = 0.2f; // 10% extra
        Vector3 paddedSize = worldSize * (1f + padding);

        // Convert world size to local space
        Vector3 localSize = transform.InverseTransformVector(paddedSize);

        // Apply to the collider
        colliderDiff.size = localSize;

        // Optional: center the collider based on mesh center
        Vector3 worldCenter = buildingRenderer.bounds.center;
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter) - transform.localPosition;
        localCenter.x = 0; localCenter.z = 0;
        colliderDiff.center = localCenter;
        

        // You can fine-tune these thresholds
        if (volume < 3000) difficultyLevel = 1;
        else if (volume < 6000) difficultyLevel = 2;
        else if (volume < 10000) difficultyLevel = 3;
        else if (volume < 14000) difficultyLevel = 4;
        else difficultyLevel = 5;
    }

    void OnDrawGizmosSelected()
    {
        // Draw difficulty info in editor for debugging
        if (buildingRenderer != null)
        {
            UnityEditor.Handles.Label(buildingRenderer.bounds.center + Vector3.up * 2,
                $"Difficulty: {difficultyLevel}");
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("display difficulty");
        PlayerUIDiffTrigger uiTrigger = other.GetComponent<PlayerUIDiffTrigger>();
        if (uiTrigger != null && uiTrigger.isLocalPlayer)
        {
            uiTrigger.ShowDifficultyUI(difficultyLevel);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerUIDiffTrigger uiTrigger = other.GetComponent<PlayerUIDiffTrigger>();
        if (uiTrigger != null && uiTrigger.isLocalPlayer)
        {
            uiTrigger.HideDifficultyUI();
        }
    }
}
