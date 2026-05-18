using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Robot: enemigo de melee con FSM de 3 estados.
///
/// INTEGRACIÓN PATHFINDING + STEERING:
///   • Pathfinding: NavMesh (A*) — calcula la ruta óptima alrededor de obstáculos.
///   • Steering:    Arrive (patrulla) + Pursue (persecución) via SteeringAgent.
///
/// Flujo de decisión:
///   FSM (Idle/Patrol/Chase)
///     → SteeringAgent.ArriveTo() o PursueTarget()
///       → SteeringBehaviors calcula velocidad local
///       → NavMesh.desiredVelocity (próximo waypoint del path A*)
///       → Blending → NavMeshAgent.velocity
/// </summary>
[RequireComponent(typeof(SteeringAgent))]
public class Robot : MonoBehaviour
{
    public enum RobotState
    {
        Idle,
        Patrol,
        Chase
    }

    [Header("State Machine")]
    [SerializeField] RobotState currentState = RobotState.Idle;
    [SerializeField] float visionRange = 15f;
    [SerializeField] float patrolRadius = 10f;
    [SerializeField] float idleWaitTime = 2f;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;
    [SerializeField] float losCheckInterval = 0.15f;

    FirstPersonController player;
    SteeringAgent steeringAgent;

    float waitTimer = 0f;
    float losTimer = 0f;
    bool cachedCanSeePlayer = false;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        steeringAgent = GetComponent<SteeringAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        steeringAgent.NavAgent.stoppingDistance = 0f;
    }

    void Update()
    {
        if (!player) return;

        // Throttle del Raycast de LoS (no raycastear cada frame)
        losTimer += Time.deltaTime;
        if (losTimer >= losCheckInterval)
        {
            losTimer = 0f;
            cachedCanSeePlayer = CheckLineOfSight();
        }

        // FSM — Toma de Decisiones
        switch (currentState)
        {
            case RobotState.Idle:   UpdateIdleState();   break;
            case RobotState.Patrol: UpdatePatrolState(); break;
            case RobotState.Chase:  UpdateChaseState();  break;
        }
    }

    // ------------------------------------------------------------------
    // Estados FSM
    // ------------------------------------------------------------------

    void UpdateIdleState()
    {
        if (cachedCanSeePlayer)
        {
            currentState = RobotState.Chase;
            return;
        }

        waitTimer += Time.deltaTime;
        if (waitTimer >= idleWaitTime)
        {
            waitTimer = 0f;
            Vector3 patrolDest = SampleRandomPatrolPoint();
            if (patrolDest != Vector3.zero)
            {
                // STEERING: Arrive — se mueve al waypoint y desacelera al llegar
                steeringAgent.ArriveTo(patrolDest);
                currentState = RobotState.Patrol;
            }
        }
    }

    void UpdatePatrolState()
    {
        if (cachedCanSeePlayer)
        {
            currentState = RobotState.Chase;
            return;
        }

        NavMeshAgent nav = steeringAgent.NavAgent;
        if (!nav.pathPending && nav.remainingDistance < 0.5f)
        {
            steeringAgent.Stop();
            waitTimer = 0f;
            currentState = RobotState.Idle;
        }
    }

    void UpdateChaseState()
    {
        if (!cachedCanSeePlayer)
        {
            steeringAgent.Stop();
            waitTimer = 0f;
            currentState = RobotState.Idle;
            return;
        }

        // STEERING: Pursue — predice la posición futura del jugador e intercepta
        // Más efectivo que Seek simple: el robot "adelanta" al jugador.
        steeringAgent.PursueTarget(player.transform);
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > visionRange) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = (player.transform.position + Vector3.up) - origin;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, visionRange, visionLayers))
            return hit.collider.CompareTag(PLAYER_STRING);

        return false;
    }

    Vector3 SampleRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, 1))
            return hit.position;
        return Vector3.zero;
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
