using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FleeingRobot — FSM: Patrol | Attack | Flee
/// Integración Entrega 2:
///   Patrol → Steering Arrive + Pathfinding A* (NavMesh)
///   Attack → quieto + disparo con LoS
///   Flee   → Steering Flee + Pathfinding A* (NavMesh)
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
    [SerializeField] float patrolRadius = 15f;
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Steering — Patrol")]
    [SerializeField] float arrivalSlowingRadius = 3f;
    [SerializeField] float patrolSteerDistance = 6f;

    [Header("Flee Settings")]
    [SerializeField] float fleeTriggerDistance = 10f;
    [SerializeField] float resumeAttackDistance = 12f;
    [SerializeField] float fleeSpeed = 12f;
    [SerializeField] float fleeAcceleration = 40f;
    [SerializeField] float fleeAngularSpeed = 720f;
    [SerializeField] float fleeRunDistance = 18f;

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

    [Header("Debug")]
    [SerializeField] bool showPathGizmos = true;
    [SerializeField] bool showSteeringGizmos = true;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    NavMeshAgent agent;

    Vector3 patrolWaypoint;
    bool hasPatrolWaypoint = false;
    float fireTimer = 0f;
    float patrolWaitTimer = 0f;
    bool canSeePlayer = false;
    Vector3 lastSteeringVelocity;

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
        EnemyMovement.EnsureOnNavMesh(agent);

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

        EnemyMovement.EnsureOnNavMesh(agent);

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        canSeePlayer = distanceToPlayer <= visionRange
            && EnemyVision.CanSeePlayer(transform.position, player.transform.position, visionRange, visionLayers);

        FleeingState newState;
        if (distanceToPlayer < fleeTriggerDistance)
            newState = FleeingState.Flee;
        else if (canSeePlayer && distanceToPlayer >= resumeAttackDistance)
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
                EnterFleeState();
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
        PickNewPatrolWaypoint();
    }

    void RestoreNormalMovement()
    {
        agent.speed = normalSpeed;
        agent.acceleration = normalAcceleration;
        agent.angularSpeed = normalAngularSpeed;
    }

    void UpdatePatrolState()
    {
        if (!hasPatrolWaypoint)
        {
            PickNewPatrolWaypoint();
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.25f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                PickNewPatrolWaypoint();
            }
            return;
        }

        lastSteeringVelocity = SteeringBehaviors.Arrive(
            transform.position, patrolWaypoint, agent.speed, arrivalSlowingRadius);
        EnemyMovement.NavigateWithArrive(
            agent, patrolWaypoint, agent.speed, arrivalSlowingRadius, patrolSteerDistance);
    }

    void PickNewPatrolWaypoint()
    {
        hasPatrolWaypoint = false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 randomPoint = Random.insideUnitSphere * patrolRadius;
            randomPoint.y = 0f;
            randomPoint += transform.position;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, transform.position) > 2f)
                {
                    patrolWaypoint = hit.position;
                    hasPatrolWaypoint = true;
                    EnemyMovement.NavigateWithArrive(
                        agent, patrolWaypoint, agent.speed, arrivalSlowingRadius, patrolSteerDistance);
                    return;
                }
            }
        }
    }

    void UpdateAttackState()
    {
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

    void EnterFleeState()
    {
        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.acceleration = fleeAcceleration;
        agent.angularSpeed = fleeAngularSpeed;
        agent.ResetPath();

        Vector3 awayFromPlayer = transform.position - player.transform.position;
        awayFromPlayer.y = 0f;
        if (awayFromPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(awayFromPlayer.normalized);

        ApplyFleeMovement();
    }

    void UpdateFleeState()
    {
        agent.isStopped = false;
        ApplyFleeMovement();
    }

    void ApplyFleeMovement()
    {
        lastSteeringVelocity = SteeringBehaviors.Flee(
            transform.position, player.transform.position, fleeSpeed);

        EnemyMovement.NavigateWithFlee(agent, player.transform.position, fleeSpeed, fleeRunDistance);
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

    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            EnemyMovement.DrawNavMeshPath(agent, new Color(1f, 0.85f, 0.2f, 0.9f));

        if (showSteeringGizmos)
            EnemyMovement.DrawSteeringVector(transform.position, lastSteeringVelocity, Color.red);

        if (hasPatrolWaypoint)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(patrolWaypoint, arrivalSlowingRadius);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
