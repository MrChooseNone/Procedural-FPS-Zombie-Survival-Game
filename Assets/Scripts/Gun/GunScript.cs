using UnityEngine;

public class GunScript : MonoBehaviour
{
    
    public GameObject bulletPrefab;   // Prefab for the bullet
    public Transform firePoint;      // The point where the bullet is spawned
    public float bulletSpeed = 20f;  // Speed of the bullet
    public float fireRate = 0.5f;    // Time between shots
    private float nextFireTime = 0f; // Tracks when you can fire next

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = firePoint.forward * bulletSpeed;
        Destroy(bullet, 2f); // Destroy bullet after 2 seconds to save memory
    }


}
