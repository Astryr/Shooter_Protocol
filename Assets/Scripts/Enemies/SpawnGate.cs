using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpawnGate : MonoBehaviour
{
    [SerializeField] GameObject robotPrefab;
    [SerializeField] float spawnTime = 5f;
    [SerializeField] Transform spawnPoint;
    [SerializeField] int maxSpawns = 2; // Cantidad máxima de enemigos a spawnear por puerta

    PlayerHealth player;
    int currentSpawns = 0;

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (player && currentSpawns < maxSpawns)
        {
            SpawnRobot();
            currentSpawns++;
            yield return new WaitForSeconds(spawnTime);
        }
    }

    void SpawnRobot()
    {
        if (!robotPrefab || !spawnPoint) return;

        GameObject robot = Instantiate(robotPrefab, spawnPoint.position, spawnPoint.rotation);

        if (robot.TryGetComponent(out NavMeshAgent agent))
            EnemyMovement.EnsureOnNavMesh(agent);
    }
}
