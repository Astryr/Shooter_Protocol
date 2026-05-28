using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Robot — FSM: Patrol | Chase
/// Patrulla puntos aleatorios del NavMesh. Si detecta al jugador (rango + LoS), persigue.
/// Si lo pierde de vista o sale del rango, vuelve a patrullar.
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

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;

    FirstPersonController player;
    NavMeshAgent agent;

    float patrolWaitTimer = 0f;
    bool canSeePlayer = false;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        EnsureOnNavMesh();

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;
        BeginPatrol();
    }

    void Update()
    {
        if (!player) return;

        if (!agent.isOnNavMesh)
            EnsureOnNavMesh();

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // LoS: solo puede "ver" si está en rango Y no hay obstáculo
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
        SetRandomPatrolDestination();
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

    void UpdateChaseState()
    {
        agent.isStopped = false;
        agent.SetDestination(player.transform.position);
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

    void EnsureOnNavMesh()
    {
        if (agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
