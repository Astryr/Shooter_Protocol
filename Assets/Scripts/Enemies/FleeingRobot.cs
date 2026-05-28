using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FleeingRobot — FSM: Patrol | Attack | Flee
/// Patrulla como el Robot. Si te ve, dispara hasta perderte de vista.
/// Si te acercás demasiado, huye rápido (sin importar LoS).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
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
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Flee Settings")]
    [SerializeField] float fleeSpeed = 8f;
    [SerializeField] float fleeAcceleration = 20f;
    [SerializeField] float fleeAngularSpeed = 300f;
    [SerializeField] float fleeDistance = 14f;

    [Header("Attack Settings")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2f;
    [SerializeField] int damage = 1;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;

    [Header("Visuals")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = Color.yellow;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    NavMeshAgent agent;

    float fireTimer = 0f;
    float patrolWaitTimer = 0f;
    bool canSeePlayer = false;

    float normalSpeed;
    float normalAcceleration;
    float normalAngularSpeed;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        ResolveMissingReferences();
        EnsureOnNavMesh();

        normalSpeed = agent.speed;
        normalAcceleration = agent.acceleration;
        normalAngularSpeed = agent.angularSpeed;

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        BeginPatrol();
        ApplyVisuals();
    }

    void Update()
    {
        if (!player) return;

        if (!agent.isOnNavMesh)
            EnsureOnNavMesh();

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        canSeePlayer = distanceToPlayer <= visionRange
            && EnemyVision.CanSeePlayer(transform.position, player.transform.position, visionRange, visionLayers);

        // Prioridad: Flee (cerca, sin LoS) > Attack (ve al jugador) > Patrol
        FleeingState newState;
        if (distanceToPlayer < safeDistance)
            newState = FleeingState.Flee;
        else if (canSeePlayer)
            newState = FleeingState.Attack;
        else
            newState = FleeingState.Patrol;

        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        ExecuteState();
    }

    void OnStateEnter(FleeingState state)
    {
        switch (state)
        {
            case FleeingState.Patrol:
                BeginPatrol();
                break;

            case FleeingState.Attack:
                agent.isStopped = true;
                agent.ResetPath();
                fireTimer = 0f;
                break;

            case FleeingState.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                agent.acceleration = fleeAcceleration;
                agent.angularSpeed = fleeAngularSpeed;
                break;
        }
    }

    void OnStateExit(FleeingState state)
    {
        if (state == FleeingState.Flee)
            RestoreNormalMovement();
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

    void BeginPatrol()
    {
        RestoreNormalMovement();
        patrolWaitTimer = 0f;
        agent.isStopped = false;
        SetRandomPatrolDestination();
    }

    void RestoreNormalMovement()
    {
        agent.speed = normalSpeed;
        agent.acceleration = normalAcceleration;
        agent.angularSpeed = normalAngularSpeed;
    }

    void UpdatePatrolState()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.25f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                SetRandomPatrolDestination();
            }
        }
        else if (!agent.hasPath && !agent.pathPending)
        {
            SetRandomPatrolDestination();
        }
    }

    void SetRandomPatrolDestination()
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 randomPoint = Random.insideUnitSphere * patrolRadius;
            randomPoint.y = 0f;
            randomPoint += transform.position;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, transform.position) > 2f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }

    void UpdateAttackState()
    {
        // Dispara solo mientras tiene LoS; al perderlo la FSM pasa a Patrol
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
        agent.isStopped = false;

        Vector3 playerVelocity = Vector3.zero;
        if (player.TryGetComponent(out CharacterController cc))
            playerVelocity = cc.velocity;

        Vector3 evadeVelocity = SteeringBehaviors.Evade(
            transform.position,
            player.transform.position,
            playerVelocity,
            fleeSpeed);

        Vector3 fleeTarget = transform.position + evadeVelocity.normalized * fleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance * 1.5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        Projectile newProjectile = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.identity
        ).GetComponent<Projectile>();

        newProjectile.transform.LookAt(player.transform.position + Vector3.up);
        newProjectile.Init(damage);
        newProjectile.SetColor(glowColor);
    }

    void EnsureOnNavMesh()
    {
        if (agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    void ResolveMissingReferences()
    {
        if (projectileSpawnPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Projectile Spawn Fleeing Robot")
                {
                    projectileSpawnPoint = child;
                    break;
                }
            }
        }

        if (glowingSphereRenderer == null)
            glowingSphereRenderer = GetComponentInChildren<Renderer>();
    }

    void ApplyVisuals()
    {
        if (glowingSphereRenderer == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        glowingSphereRenderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorID, glowColor);
        block.SetColor(BaseColorID, glowColor);
        glowingSphereRenderer.SetPropertyBlock(block);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
