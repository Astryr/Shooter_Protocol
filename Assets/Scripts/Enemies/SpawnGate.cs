using System.Collections;
using UnityEngine;

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
            Instantiate(robotPrefab, spawnPoint.position, transform.rotation);
            currentSpawns++;
            yield return new WaitForSeconds(spawnTime);
        }
    }
}
