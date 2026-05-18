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

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;
    [SerializeField] float losCheckInterval = 0.15f;

    FirstPersonController player;
    NavMeshAgent agent;
    float waitTimer = 0f;
    float losTimer = 0f;
    bool cachedCanSeePlayer = false;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        agent.stoppingDistance = 0f;
    }

    void Update()
    {
        if (!player) return;

        losTimer += Time.deltaTime;
        if (losTimer >= losCheckInterval)
        {
            losTimer = 0f;
            cachedCanSeePlayer = CheckLineOfSight();
        }

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
        if (cachedCanSeePlayer)
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
        if (cachedCanSeePlayer)
        {
            currentState = RobotState.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer = 0f;
            currentState = RobotState.Idle;
        }
    }

    void UpdateChaseState()
    {
        if (!cachedCanSeePlayer)
        {
            waitTimer = 0f;
            currentState = RobotState.Idle;
            agent.ResetPath();
            return;
        }

        agent.SetDestination(player.transform.position);
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > visionRange) return false;

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
