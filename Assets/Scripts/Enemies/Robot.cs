using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Robot — FSM: Patrol | Chase
/// Integración Entrega 2:
///   Patrol → Steering Arrive + Pathfinding A* (NavMesh)
///   Chase  → Steering Pursue + Pathfinding A* (NavMesh)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Robot : MonoBehaviour
{
    public enum RobotState
    {
        Patrol,
        Chase
    }

    [Header("State Machine")]
    [SerializeField] RobotState currentState = RobotState.Patrol;
    [SerializeField] float visionRange = 15f;

    [Header("Patrol Auto-Config")]
    [Tooltip("Aplica al iniciar el perfil de patrulla rápida para mapa grande (ignora valores viejos del prefab/escena).")]
    [SerializeField] bool applyLargeMapPatrolProfile = true;

    [Header("Patrol")]
    [SerializeField] float patrolSpeed = 5.5f;
    [SerializeField] float patrolRadius = 24f;
    [SerializeField] float patrolWaitTime = 0.75f;
    [SerializeField] float minPatrolLegDistance = 6f;
    [SerializeField] float patrolAcceleration = 12f;
    [SerializeField] float patrolAngularSpeed = 180f;

    [Header("Chase")]
    [SerializeField] float chaseSpeed = 5f;
    [SerializeField] float chaseAcceleration = 10f;

    [Header("Steering — Patrol")]
    [SerializeField] float arrivalSlowingRadius = 3f;
    [SerializeField] float patrolSteerDistance = 12f;

    [Header("Steering — Chase")]
    [SerializeField] float pursueSteerDistance = 10f;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;

    [Header("Debug")]
    [SerializeField] bool showPathGizmos = true;
    [SerializeField] bool showSteeringGizmos = true;

    FirstPersonController player;
    NavMeshAgent agent;

    Vector3 patrolWaypoint;
    bool hasPatrolWaypoint = false;
    float patrolWaitTimer = 0f;
    bool canSeePlayer = false;
    Vector3 lastSteeringVelocity;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (applyLargeMapPatrolProfile)
            ApplyLargeMapPatrolProfile();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        EnemyMovement.EnsureOnNavMesh(agent);

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;
        BeginPatrol();
    }

    /// <summary>
    /// Perfil único para mapa grande: se aplica en runtime a todos los robots
    /// (colocados a mano o spawneados por Spawn Gate) sin tocar el Inspector.
    /// </summary>
    void ApplyLargeMapPatrolProfile()
    {
        patrolSpeed = 5.5f;
        patrolRadius = 24f;
        patrolWaitTime = 0.75f;
        minPatrolLegDistance = 6f;
        patrolAcceleration = 12f;
        patrolAngularSpeed = 180f;
        chaseSpeed = 5f;
        chaseAcceleration = 10f;
        arrivalSlowingRadius = 3f;
        patrolSteerDistance = 12f;
        pursueSteerDistance = 10f;

        if (agent != null)
        {
            agent.acceleration = patrolAcceleration;
            agent.angularSpeed = patrolAngularSpeed;
            agent.stoppingDistance = 0.5f;
        }
    }

    void Update()
    {
        if (!player) return;

        EnemyMovement.EnsureOnNavMesh(agent);

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        canSeePlayer = distanceToPlayer <= visionRange
            && EnemyVision.CanSeePlayer(transform.position, player.transform.position, visionRange, visionLayers);

        RobotState newState = canSeePlayer ? RobotState.Chase : RobotState.Patrol;

        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        ExecuteState();
    }

    void OnStateEnter(RobotState state)
    {
        if (state == RobotState.Patrol)
            BeginPatrol();
        else if (state == RobotState.Chase)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.acceleration = chaseAcceleration;
        }
    }

    void OnStateExit(RobotState state) { }

    void ExecuteState()
    {
        switch (currentState)
        {
            case RobotState.Patrol:
                UpdatePatrolState();
                break;
            case RobotState.Chase:
                UpdateChaseState();
                break;
        }
    }

    void BeginPatrol()
    {
        patrolWaitTimer = 0f;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;
        agent.angularSpeed = patrolAngularSpeed;
        PickNewPatrolWaypoint();
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

        // Steering Arrive → NavMesh A*
        lastSteeringVelocity = SteeringBehaviors.Arrive(
            transform.position, patrolWaypoint, agent.speed, arrivalSlowingRadius);
        EnemyMovement.NavigateWithArrive(
            agent, patrolWaypoint, agent.speed, arrivalSlowingRadius, patrolSteerDistance);
    }

    void UpdateChaseState()
    {
        // Steering Pursue → NavMesh A* (intercepta al jugador)
        lastSteeringVelocity = SteeringBehaviors.Pursue(
            transform.position,
            player.transform.position,
            EnemyMovement.GetTargetVelocity(player.transform),
            agent.speed);

        EnemyMovement.NavigateWithPursue(agent, player.transform, agent.speed, pursueSteerDistance);
    }

    void PickNewPatrolWaypoint()
    {
        hasPatrolWaypoint = false;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 randomPoint = Random.insideUnitSphere * patrolRadius;
            randomPoint.y = 0f;
            randomPoint += transform.position;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, transform.position) > minPatrolLegDistance)
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

    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            EnemyMovement.DrawNavMeshPath(agent, new Color(0.2f, 0.85f, 1f, 0.9f));

        if (showSteeringGizmos)
            EnemyMovement.DrawSteeringVector(transform.position, lastSteeringVelocity, Color.green);

        if (hasPatrolWaypoint)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(patrolWaypoint, arrivalSlowingRadius);
        }

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
