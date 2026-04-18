using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class Robot : MonoBehaviour
{
    public enum RobotState
    {
        Idle,
        Patrol,
        Chase
    }

    [Header("State Machine Parameters")]
    [SerializeField] RobotState currentState = RobotState.Idle;
    [SerializeField] float visionRange = 15f;
    [SerializeField] float patrolRadius = 10f;
    [SerializeField] float idleWaitTime = 2f;

    FirstPersonController player;
    NavMeshAgent agent;
    float waitTimer = 0f;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        agent.stoppingDistance = 0f; // Evita que pare antes de tocarlo
    }

    void Update()
    {
        if (!player) return;

        // Implementación de Sistema de Toma de Decisiones: FSM (Finite State Machine)
        switch (currentState)
        {
            case RobotState.Idle:
                UpdateIdleState();
                break;
            case RobotState.Patrol:
                UpdatePatrolState();
                break;
            case RobotState.Chase:
                UpdateChaseState();
                break;
        }
    }

    void UpdateIdleState()
    {
        if (CanSeePlayer())
        {
            currentState = RobotState.Chase;
            return;
        }

        waitTimer += Time.deltaTime;
        if (waitTimer >= idleWaitTime)
        {
            SetRandomPatrolDestination();
            currentState = RobotState.Patrol;
        }
    }

    void UpdatePatrolState()
    {
        if (CanSeePlayer())
        {
            currentState = RobotState.Chase;
            return;
        }

        // Si ya llegó al destino de patrullaje
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer = 0f;
            currentState = RobotState.Idle;
        }
    }

    void UpdateChaseState()
    {
        if (!CanSeePlayer())
        {
            // Si pierde la Line of Sight (LoS) vuelve a Idle luego de un rato
            waitTimer = 0f;
            currentState = RobotState.Idle;
            agent.ResetPath(); // Detiene su avance anterior
            return;
        }

        agent.SetDestination(player.transform.position);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer <= visionRange)
        {
            // Sistema Line Of Sight (LoS): Emite un Raycast para ver obstáculos
            Vector3 directionToPlayer = (player.transform.position + Vector3.up) - (transform.position + Vector3.up); // Apuntar al centro del cuerpo
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, visionRange))
            {
                if (hit.collider.CompareTag(PLAYER_STRING))
                {
                    return true;
                }
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
