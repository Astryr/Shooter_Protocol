using StarterAssets;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class FleeingRobot : MonoBehaviour
{
    public enum FleeingState
    {
        Patrol,
        Attack,
        Flee
    }

    [Header("State Machine")]
    [SerializeField] FleeingState currentState = FleeingState.Patrol;
    [SerializeField] float visionRange = 20f;
    [SerializeField] float safeDistance = 8f; // Si el jugador se acerca más que esto, huye
    [SerializeField] float patrolRadius = 15f;
    
    [Header("Attack Settings")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2f;
    [SerializeField] int damage = 1;

    [Header("Visuals")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = Color.yellow; // Soporta HDR para el brillo

    FirstPersonController player;
    NavMeshAgent agent;
    float waitTimer = 0f;
    float fireTimer = 0f;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        SetRandomPatrolDestination();

        // Crear una instancia única del material para este robot
        if (glowingSphereRenderer != null)
        {
            Material uniqueMat = glowingSphereRenderer.material;
            uniqueMat.EnableKeyword("_EMISSION");
            uniqueMat.SetColor("_EmissionColor", glowColor);
            uniqueMat.color = glowColor;
        }
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool hasLoS = CanSeePlayer();

        // Toma de decisiones (FSM basada en distancia y LoS)
        if (distanceToPlayer < safeDistance && hasLoS)
        {
            currentState = FleeingState.Flee;
        }
        else if (distanceToPlayer <= visionRange && hasLoS)
        {
            currentState = FleeingState.Attack;
        }
        else
        {
            currentState = FleeingState.Patrol;
        }

        ExecuteState();
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case FleeingState.Patrol:
                UpdatePatrolState();
                break;
            case FleeingState.Attack:
                UpdateAttackState();
                break;
            case FleeingState.Flee:
                UpdateFleeState();
                break;
        }
    }

    void UpdatePatrolState()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer > 2f)
            {
                SetRandomPatrolDestination();
                waitTimer = 0f;
            }
        }
    }

    void UpdateAttackState()
    {
        agent.ResetPath(); // Se queda quieto para disparar
        
        // Mira hacia el jugador
        Vector3 lookPos = player.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        // Lógica de disparo
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            ShootProjectile();
        }
    }

    void UpdateFleeState()
    {
        // Calcula la dirección opuesta al jugador
        Vector3 directionAwayFromPlayer = transform.position - player.transform.position;
        Vector3 fleePosition = transform.position + directionAwayFromPlayer.normalized * 5f;

        if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, 5f, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            Projectile newProjectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity).GetComponent<Projectile>();
            newProjectile.transform.LookAt(player.transform.position + Vector3.up); // Apuntar al cuerpo
            newProjectile.Init(damage);
            newProjectile.SetColor(glowColor);
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.transform.position + Vector3.up) - (transform.position + Vector3.up);
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, visionRange))
        {
            if (hit.collider.CompareTag(PLAYER_STRING))
            {
                return true;
            }
        }
        return false;
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag(PLAYER_STRING)) 
        {
            EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
            if(enemyHealth != null)
                enemyHealth.SelfDestruct();
        }
    }
}
