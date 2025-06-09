using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;

public class PlayerRangeCuller : MonoBehaviour
{
    [Tooltip("Tag to search for cullable objects")]
    public string targetTag = "CullTarget";

    [Tooltip("Distance thresholds (in meters) defining bands")]
    public float[] distanceBands = { 10f, 30f, 60f };

    private CullingGroup cullingGroup;
    private BoundingSphere[] spheres;
    public List<Transform> targets = new List<Transform>();
    public Camera playerCamera;

    IEnumerator Start()
    {


        yield return new WaitForSeconds(60);
        SetupCulling();
        yield return new WaitForSeconds(300);
        SetupCulling();
    }
    void SetupCulling()
    {
        // 1. Find all objects with the tag and cache their Transforms
        GameObject[] objs = GameObject.FindGameObjectsWithTag(targetTag);
        
        foreach (var go in objs)
            targets.Add(go.transform);

        // 2. Setup CullingGroup
        cullingGroup = new CullingGroup();
        cullingGroup.targetCamera = playerCamera;
        cullingGroup.SetDistanceReferencePoint(transform);
        cullingGroup.SetBoundingDistances(distanceBands);

        // 3. Create bounding spheres array
        spheres = new BoundingSphere[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            spheres[i] = new BoundingSphere(targets[i].position, 1f);

        cullingGroup.SetBoundingSpheres(spheres);
        cullingGroup.SetBoundingSphereCount(spheres.Length);
        cullingGroup.onStateChanged += OnStateChanged;
    }

    void Update()
    {
        // Update sphere positions and reference point each frame
        for (int i = 0; i < targets.Count; i++)
            spheres[i].position = targets[i].position;

        cullingGroup.SetDistanceReferencePoint(transform);
    }

    void OnStateChanged(CullingGroupEvent evt)
    {
        // Activate only if within the furthest band
        bool inRange = evt.currentDistance < distanceBands.Length;
        targets[evt.index].gameObject.SetActive(inRange);
    }

    void OnDestroy()
    {
        cullingGroup.Dispose();
    }
}

