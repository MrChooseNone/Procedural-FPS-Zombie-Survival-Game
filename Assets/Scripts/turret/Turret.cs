using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class Turret : NetworkBehaviour
{
    [Header("Targeting")]
    public float detectionRange = 15f;
    public LayerMask targetLayer;
    public Transform rotatingPart;
    public Transform firePoint;

    [Header("Firing")]
    public GameObject bulletPrefab;
    public float fireRate = 1f;
    public float bulletSpeed = 20f;
    public float damage = 10f;
    public float accuracy = 5f; // degrees of inaccuracy (0 = perfect aim)

    [Header("Others")]
    public float turnSpeed = 5f;
    public SoundEmitter soundEmitter;

    private float fireCooldown = 0f;
    private Transform target;
    public Vector3 rotOffset;

    void Update()
    {
        if (!isServer) return; // ❗ Server handles AI logic

        FindTarget();

        if (target != null)
        {
            RotateToTarget();

            if (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown = 1f / fireRate;
            }
        }

        fireCooldown -= Time.deltaTime;
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);

        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        target = closest;
    }

    void RotateToTarget()
    {
        Vector3 direction = (target.position - rotatingPart.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotOffset);
        rotatingPart.rotation = Quaternion.Lerp(rotatingPart.rotation, lookRotation, Time.deltaTime * turnSpeed);
    }

    void Fire()
    {
        Vector3 shootDir = firePoint.forward;
        shootDir = Quaternion.Euler(Random.Range(-accuracy, accuracy), Random.Range(-accuracy, accuracy), 0) * shootDir;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDir));
        NetworkServer.Spawn(bullet);

        if (bullet.TryGetComponent(out ProjectileBullet bulletScript))
        {
            bulletScript.SetShooter(netIdentity);
        }

        RpcPlayFireSound(); 
    }

    [ClientRpc]
    void RpcPlayFireSound()
    {
        if (soundEmitter != null)
            soundEmitter.EmittSound(50);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
