using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Integra Steering Behaviors con Pathfinding (A* via NavMesh).
///
/// Flujo:
///   1. FSM del enemigo elige el estado (Patrol, Chase, Flee...).
///   2. SteeringBehaviors calcula la velocidad/dirección deseada (micromovimiento).
///   3. Seek / Arrive / Pursue / Flee / Evade proyectan un destino en NavMesh (sin recalcular cada frame).
///   4. Wander sigue usando un punto de sondeo adelante en la dirección del steering.
///   5. NavMeshAgent.SetDestination → Unity calcula la ruta A* alrededor de obstáculos.
/// </summary>
public static class EnemyMovement
{
    public static bool NavigateWithSeek(NavMeshAgent agent, Vector3 target, float maxSpeed, float steerDistance = 6f)
    {
        return NavigateToPosition(agent, target, steerDistance * 0.5f, 1.5f);
    }

    public static bool NavigateWithArrive(
        NavMeshAgent agent,
        Vector3 target,
        float maxSpeed,
        float slowingRadius,
        float steerDistance = 6f)
    {
        return NavigateToPosition(agent, target, Mathf.Max(slowingRadius, 2f), 1.5f);
    }

    public static bool NavigateWithPursue(
        NavMeshAgent agent,
        Transform target,
        float maxSpeed,
        float steerDistance = 8f)
    {
        if (target == null) return false;

        Vector3 targetVelocity = GetTargetVelocity(target);
        Vector3 predictedPosition = GetPursuePosition(
            agent.transform.position, target.position, targetVelocity, maxSpeed);

        return NavigateToPosition(agent, predictedPosition, steerDistance * 0.5f, 2f);
    }

    public static bool NavigateWithFlee(
        NavMeshAgent agent,
        Vector3 threatPosition,
        float maxSpeed,
        float fleeDistance)
    {
        Vector3 fleeTarget = GetFleePosition(agent.transform.position, threatPosition, fleeDistance);
        return NavigateToPosition(agent, fleeTarget, fleeDistance * 0.5f, fleeDistance * 0.3f);
    }

    public static bool NavigateWithEvade(
        NavMeshAgent agent,
        Transform threat,
        float maxSpeed,
        float evadeDistance)
    {
        if (threat == null) return false;

        Vector3 threatVelocity = GetTargetVelocity(threat);
        Vector3 evadeTarget = GetEvadePosition(
            agent.transform.position, threat.position, threatVelocity, maxSpeed, evadeDistance);

        return NavigateToPosition(agent, evadeTarget, evadeDistance * 0.5f, evadeDistance * 0.3f);
    }

    public static bool NavigateWithWander(
        NavMeshAgent agent,
        ref float wanderAngle,
        float wanderRadius,
        float wanderDistance,
        float wanderJitter,
        float maxSpeed,
        float steerDistance = 6f)
    {
        Vector3 velocity = SteeringBehaviors.Wander(
            agent.transform.position,
            agent.transform.forward,
            ref wanderAngle,
            wanderRadius,
            wanderDistance,
            wanderJitter,
            maxSpeed);

        return ApplySteeringToNavMesh(agent, velocity, steerDistance);
    }

    /// <summary>
    /// Aplica la dirección del steering y delega el camino al NavMesh (A*).
    /// </summary>
    public static bool ApplySteeringToNavMesh(NavMeshAgent agent, Vector3 steeringVelocity, float distance)
    {
        if (agent == null || !agent.isOnNavMesh) return false;
        if (steeringVelocity.sqrMagnitude < 0.01f) return false;

        Vector3 probe = agent.transform.position + steeringVelocity.normalized * distance;

        if (NavMesh.SamplePosition(probe, out NavMeshHit hit, distance * 1.5f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            return true;
        }

        return false;
    }

    public static bool NavigateToPosition(
        NavMeshAgent agent,
        Vector3 worldPosition,
        float sampleRadius = 2f,
        float refreshDistance = 1.5f)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        if (!agent.isStopped && !agent.pathPending && agent.hasPath)
        {
            if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
            {
                float destinationShift = Vector3.Distance(agent.destination, worldPosition);
                if (destinationShift < refreshDistance)
                    return true;
            }
        }

        return TrySetNavMeshDestination(agent, worldPosition, sampleRadius);
    }

    public static bool TrySetNavMeshDestination(NavMeshAgent agent, Vector3 worldPosition, float sampleRadius = 2f)
    {
        if (agent == null) return false;

        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            return true;
        }

        return false;
    }

    public static bool HasReachedDestination(NavMeshAgent agent, float extraTolerance = 0.25f)
    {
        if (agent == null || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;

        return agent.remainingDistance <= agent.stoppingDistance + extraTolerance;
    }

    public static void StopAgent(NavMeshAgent agent)
    {
        if (agent == null) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    public static void EnsureOnNavMesh(NavMeshAgent agent, float sampleRadius = 20f)
    {
        if (agent == null || agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    public static Vector3 GetPursuePosition(
        Vector3 position,
        Vector3 targetPosition,
        Vector3 targetVelocity,
        float maxSpeed)
    {
        float distance = Vector3.Distance(position, targetPosition);
        float lookahead = maxSpeed > 0f ? distance / maxSpeed : 0f;
        return targetPosition + targetVelocity * lookahead;
    }

    static Vector3 GetFleePosition(Vector3 position, Vector3 threatPosition, float fleeDistance)
    {
        Vector3 away = position - threatPosition;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);

        return position + away.normalized * fleeDistance;
    }

    static Vector3 GetEvadePosition(
        Vector3 position,
        Vector3 threatPosition,
        Vector3 threatVelocity,
        float maxSpeed,
        float evadeDistance)
    {
        float distance = Vector3.Distance(position, threatPosition);
        float lookahead = maxSpeed > 0f ? distance / maxSpeed : 0f;
        Vector3 predictedThreatPosition = threatPosition + threatVelocity * lookahead;
        return GetFleePosition(position, predictedThreatPosition, evadeDistance);
    }

    public static Vector3 GetTargetVelocity(Transform target)
    {
        if (target.TryGetComponent(out NavMeshAgent navAgent))
            return navAgent.velocity;
        if (target.TryGetComponent(out CharacterController cc))
            return cc.velocity;
        return Vector3.zero;
    }

    public static void DrawNavMeshPath(NavMeshAgent agent, Color pathColor)
    {
        if (agent == null || !agent.hasPath) return;

        Gizmos.color = pathColor;
        Vector3[] corners = agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
            Gizmos.DrawLine(corners[i], corners[i + 1]);

        for (int i = 0; i < corners.Length; i++)
            Gizmos.DrawWireSphere(corners[i], 0.12f);
    }

    public static void DrawSteeringVector(Vector3 origin, Vector3 steeringVelocity, Color color)
    {
        if (steeringVelocity.sqrMagnitude < 0.01f) return;

        Gizmos.color = color;
        Gizmos.DrawRay(origin + Vector3.up * 0.5f, steeringVelocity.normalized * 2.5f);
    }
}
