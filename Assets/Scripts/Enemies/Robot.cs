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
    [SerializeField] float patrolRadius = 10f;
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Steering — Patrol")]
    [SerializeField] float arrivalSlowingRadius = 3f;
    [SerializeField] float patrolSteerDistance = 6f;

    [Header("Steering — Chase")]
    [SerializeField] float pursueSteerDistance = 8f;

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
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        EnemyMovement.EnsureOnNavMesh(agent);

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;
        BeginPatrol();
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
            agent.isStopped = false;
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
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
