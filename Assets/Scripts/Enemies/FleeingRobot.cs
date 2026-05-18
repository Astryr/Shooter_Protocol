using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] float safeDistance = 8f;
    [SerializeField] float patrolRadius = 15f;

    [Header("Attack Settings")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2f;
    [SerializeField] int damage = 1;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;
    [SerializeField] float losCheckInterval = 0.15f;

    [Header("Visuals")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = Color.yellow;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    NavMeshAgent agent;
    float waitTimer = 0f;
    float fireTimer = 0f;
    float losTimer = 0f;
    bool cachedCanSeePlayer = false;
    float cachedDistanceToPlayer = float.MaxValue;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        SetRandomPatrolDestination();

        if (glowingSphereRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            glowingSphereRenderer.GetPropertyBlock(block);
            block.SetColor(EmissionColorID, glowColor);
            block.SetColor(BaseColorID, glowColor);
            glowingSphereRenderer.SetPropertyBlock(block);
        }
    }

    void Update()
    {
        if (!player) return;

        losTimer += Time.deltaTime;
        if (losTimer >= losCheckInterval)
        {
            losTimer = 0f;
            cachedDistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            cachedCanSeePlayer = CheckLineOfSight();
        }

        if (cachedDistanceToPlayer < safeDistance && cachedCanSeePlayer)
        {
            currentState = FleeingState.Flee;
        }
        else if (cachedDistanceToPlayer <= visionRange && cachedCanSeePlayer)
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
        agent.ResetPath();

        Vector3 lookPos = player.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            ShootProjectile();
        }
    }

    void UpdateFleeState()
    {
        Vector3 directionAwayFromPlayer = transform.position - player.transform.position;
        Vector3 fleePosition = transform.position + directionAwayFromPlayer.normalized * 5f;

        if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, 5f, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        Projectile newProjectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity)
            .GetComponent<Projectile>();
        newProjectile.transform.LookAt(player.transform.position + Vector3.up);
        newProjectile.Init(damage);
        newProjectile.SetColor(glowColor);
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = (player.transform.position + Vector3.up) - origin;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, visionRange, visionLayers))
        {
            return hit.collider.CompareTag(PLAYER_STRING);
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
            enemyHealth?.SelfDestruct();
        }
    }
}
