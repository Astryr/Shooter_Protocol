using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Integra Pathfinding (A* via NavMesh) con Steering Behaviors locales.
/// El NavMeshAgent mueve al agente; los steering behaviors definen el destino y la velocidad.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SteeringAgent : MonoBehaviour
{
    public enum ActiveBehavior
    {
        None,
        Seek,
        Flee,
        Arrive,
        Wander,
        Pursue,
        Evade
    }

    [Header("Steering Parameters")]
    [SerializeField] float arrivalSlowingRadius = 3f;
    [SerializeField] float wanderRadius = 2.5f;
    [SerializeField] float wanderDistance = 4f;
    [SerializeField] float wanderJitter = 2.5f;

    [Header("Debug / Visualización")]
    [SerializeField] bool showGizmos = true;

    NavMeshAgent navAgent;
    float defaultMaxSpeed;
    float wanderAngle = 0f;

    ActiveBehavior currentBehavior = ActiveBehavior.None;
    Vector3 targetPosition;
    Transform targetTransform;
    Vector3 debugSteeringVelocity;

    public NavMeshAgent NavAgent => navAgent;
    public ActiveBehavior CurrentBehavior => currentBehavior;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        defaultMaxSpeed = navAgent.speed;
    }

    void Start()
    {
        EnsureOnNavMesh();
    }

    void Update()
    {
        ApplySteering();
    }

    void EnsureOnNavMesh()
    {
        if (navAgent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            navAgent.Warp(hit.position);
    }

    public void SeekTo(Vector3 position)
    {
        currentBehavior = ActiveBehavior.Seek;
        targetPosition = position;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;
        navAgent.SetDestination(position);
    }

    public void FleeTo(Vector3 threatPosition)
    {
        currentBehavior = ActiveBehavior.Flee;
        targetPosition = threatPosition;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;

        Vector3 fleeDir = (transform.position - threatPosition).normalized;
        float fleeRange = navAgent.speed * 2.5f;
        Vector3 fleeTarget = transform.position + fleeDir * fleeRange;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeRange, NavMesh.AllAreas))
            navAgent.SetDestination(hit.position);
    }

    public void ArriveTo(Vector3 position)
    {
        currentBehavior = ActiveBehavior.Arrive;
        targetPosition = position;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;
        navAgent.SetDestination(position);
    }

    public void WanderAround()
    {
        currentBehavior = ActiveBehavior.Wander;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;
    }

    public void PursueTarget(Transform target)
    {
        currentBehavior = ActiveBehavior.Pursue;
        targetTransform = target;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;
    }

    public void EvadeTarget(Transform target)
    {
        currentBehavior = ActiveBehavior.Evade;
        targetTransform = target;
        navAgent.isStopped = false;
        navAgent.speed = defaultMaxSpeed;
    }

    public void Stop()
    {
        currentBehavior = ActiveBehavior.None;
        navAgent.isStopped = true;
        navAgent.ResetPath();
        navAgent.speed = defaultMaxSpeed;
        debugSteeringVelocity = Vector3.zero;
    }

    void ApplySteering()
    {
        if (!navAgent.isOnNavMesh) return;
        if (currentBehavior == ActiveBehavior.None) return;

        float speed = defaultMaxSpeed;
        Vector3 steeringVelocity = Vector3.zero;

        switch (currentBehavior)
        {
            case ActiveBehavior.Seek:
                steeringVelocity = SteeringBehaviors.Seek(transform.position, targetPosition, speed);
                navAgent.SetDestination(targetPosition);
                break;

            case ActiveBehavior.Flee:
                steeringVelocity = SteeringBehaviors.Flee(transform.position, targetPosition, speed);
                break;

            case ActiveBehavior.Arrive:
                steeringVelocity = SteeringBehaviors.Arrive(
                    transform.position, targetPosition, speed, arrivalSlowingRadius);

                float distance = Vector3.Distance(transform.position, targetPosition);
                navAgent.speed = Mathf.Clamp(
                    distance / Mathf.Max(arrivalSlowingRadius, 0.01f) * speed,
                    speed * 0.25f,
                    speed);

                navAgent.SetDestination(targetPosition);
                break;

            case ActiveBehavior.Wander:
                steeringVelocity = SteeringBehaviors.Wander(
                    transform.position, transform.forward,
                    ref wanderAngle,
                    wanderRadius, wanderDistance, wanderJitter,
                    speed);

                Vector3 wanderDest = transform.position + steeringVelocity.normalized * wanderDistance;
                if (NavMesh.SamplePosition(wanderDest, out NavMeshHit wanderHit, wanderDistance * 1.5f, NavMesh.AllAreas))
                    navAgent.SetDestination(wanderHit.position);

                debugSteeringVelocity = steeringVelocity;
                return;

            case ActiveBehavior.Pursue:
                if (targetTransform != null)
                {
                    Vector3 targetVel = GetVelocityOf(targetTransform);
                    steeringVelocity = SteeringBehaviors.Pursue(
                        transform.position, targetTransform.position, targetVel, speed);

                    float lookahead = Vector3.Distance(transform.position, targetTransform.position) / Mathf.Max(speed, 0.01f);
                    Vector3 predicted = targetTransform.position + targetVel * lookahead;
                    navAgent.SetDestination(predicted);
                }
                break;

            case ActiveBehavior.Evade:
                if (targetTransform != null)
                {
                    Vector3 threatVel = GetVelocityOf(targetTransform);
                    steeringVelocity = SteeringBehaviors.Evade(
                        transform.position, targetTransform.position, threatVel, speed);

                    float lookahead = Vector3.Distance(transform.position, targetTransform.position) / Mathf.Max(speed, 0.01f);
                    Vector3 predictedThreat = targetTransform.position + threatVel * lookahead;
                    Vector3 evadeTarget = transform.position + (transform.position - predictedThreat).normalized * speed * 2f;

                    if (NavMesh.SamplePosition(evadeTarget, out NavMeshHit evadeHit, speed * 2.5f, NavMesh.AllAreas))
                        navAgent.SetDestination(evadeHit.position);
                }
                break;
        }

        debugSteeringVelocity = steeringVelocity;
    }

    Vector3 GetVelocityOf(Transform target)
    {
        if (target.TryGetComponent(out NavMeshAgent targetAgent))
            return targetAgent.velocity;
        if (target.TryGetComponent(out CharacterController cc))
            return cc.velocity;
        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || navAgent == null) return;

        if (navAgent.hasPath)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        if (debugSteeringVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                           debugSteeringVelocity.normalized * 2f);
        }

        if (currentBehavior == ActiveBehavior.Wander)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f);
            Gizmos.DrawWireSphere(
                transform.position + transform.forward * wanderDistance,
                wanderRadius);
        }

        if (currentBehavior == ActiveBehavior.Arrive)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
            Gizmos.DrawWireSphere(targetPosition, arrivalSlowingRadius);
        }
    }
}
