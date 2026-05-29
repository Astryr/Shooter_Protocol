using System.Collections;
using UnityEngine;

/// <summary>
/// Torreta — agente estático (Entrega 1 + 2).
/// FSM implícita: Idle (apunta) → Fire (si LoS).
/// No usa steering ni pathfinding: no se desplaza.
/// Line of Sight obligatoria: paredes y obstáculos bloquean el disparo.
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

            if (HasLineOfSightToPlayer())
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

    bool HasLineOfSightToPlayer()
    {
        if (player == null || playerTargetPoint == null || projectileSpawnPoint == null)
            return false;

        Vector3 origin = projectileSpawnPoint.position;
        Vector3 target = playerTargetPoint.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance < 0.01f) return true;

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
