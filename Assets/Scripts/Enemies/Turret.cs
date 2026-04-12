using System.Collections;
using UnityEngine;

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
        turretHead.LookAt(playerTargetPoint);
    }

    IEnumerator FireRoutine()
    {
        while(player) 
        {
            yield return new WaitForSeconds(fireRate);

            if (CanSeePlayer())
            {
                Projectile newProjectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity).GetComponent<Projectile>();
                newProjectile.transform.LookAt(playerTargetPoint);
                newProjectile.Init(damage);
            }
        }
    }

    bool CanSeePlayer()
    {
        if (player == null || playerTargetPoint == null) return false;

        Vector3 directionToPlayer = playerTargetPoint.position - projectileSpawnPoint.position;

        // Lanzamos un raycast desde el cañon hacia el jugador para ver si hay obstáculos
        if (Physics.Raycast(projectileSpawnPoint.position, directionToPlayer, out RaycastHit hit))
        {
            // Verificamos si lo que golpeó el rayo pertenece al jugador
            if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
            {
                return true;
            }
        }
        return false;
    }
}
