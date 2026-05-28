using System.Collections;
using UnityEngine;

/// <summary>
/// Torreta — dispara solo si tiene línea de visión directa al jugador (LoS).
/// Obstáculos y paredes bloquean el disparo.
/// </summary>
public class Turret : MonoBehaviour
{
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform turretHead;
    [SerializeField] Transform playerTargetPoint;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2f;
    [SerializeField] int damage = 2;

    PlayerHealth player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        StartCoroutine(FireRoutine());
    }

    void Update()
    {
        if (playerTargetPoint != null)
            turretHead.LookAt(playerTargetPoint);
    }

    IEnumerator FireRoutine()
    {
        while (player)
        {
            yield return new WaitForSeconds(fireRate);

            if (CanSeePlayer())
            {
                Projectile newProjectile = Instantiate(
                    projectilePrefab,
                    projectileSpawnPoint.position,
                    Quaternion.identity
                ).GetComponent<Projectile>();

                newProjectile.transform.LookAt(playerTargetPoint);
                newProjectile.Init(damage);
            }
        }
    }

    bool CanSeePlayer()
    {
        if (player == null || playerTargetPoint == null || projectileSpawnPoint == null)
            return false;

        Vector3 origin = projectileSpawnPoint.position;
        Vector3 target = playerTargetPoint.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance < 0.01f) return true;

        // LoS: el primer obstáculo en el camino debe ser el jugador
        if (Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PlayerHealth>() != null;
        }

        return false;
    }
}
