using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Integra Steering Behaviors con Pathfinding (A* via NavMesh).
///
/// Flujo:
///   1. FSM del enemigo elige el estado (Patrol, Chase, Flee...).
///   2. SteeringBehaviors calcula la velocidad/dirección deseada (micromovimiento).
///   3. Se proyecta un punto en esa dirección y NavMesh.SamplePosition lo valida.
///   4. NavMeshAgent.SetDestination → Unity calcula la ruta A* alrededor de obstáculos.
/// </summary>
public static class EnemyMovement
{
    public static bool NavigateWithSeek(NavMeshAgent agent, Vector3 target, float maxSpeed, float steerDistance = 6f)
    {
        Vector3 velocity = SteeringBehaviors.Seek(agent.transform.position, target, maxSpeed);
        return ApplySteeringToNavMesh(agent, velocity, steerDistance);
    }

    public static bool NavigateWithArrive(
        NavMeshAgent agent,
        Vector3 target,
        float maxSpeed,
        float slowingRadius,
        float steerDistance = 6f)
    {
        Vector3 velocity = SteeringBehaviors.Arrive(
            agent.transform.position, target, maxSpeed, slowingRadius);
        return ApplySteeringToNavMesh(agent, velocity, steerDistance);
    }

    public static bool NavigateWithPursue(
        NavMeshAgent agent,
        Transform target,
        float maxSpeed,
        float steerDistance = 8f)
    {
        if (target == null) return false;

        Vector3 targetVelocity = GetTargetVelocity(target);
        Vector3 velocity = SteeringBehaviors.Pursue(
            agent.transform.position,
            target.position,
            targetVelocity,
            maxSpeed);

        return ApplySteeringToNavMesh(agent, velocity, steerDistance);
    }

    public static bool NavigateWithFlee(
        NavMeshAgent agent,
        Vector3 threatPosition,
        float maxSpeed,
        float fleeDistance)
    {
        Vector3 velocity = SteeringBehaviors.Flee(
            agent.transform.position, threatPosition, maxSpeed);

        return ApplySteeringToNavMesh(agent, velocity, fleeDistance);
    }

    public static bool NavigateWithEvade(
        NavMeshAgent agent,
        Transform threat,
        float maxSpeed,
        float evadeDistance)
    {
        if (threat == null) return false;

        Vector3 threatVelocity = GetTargetVelocity(threat);
        Vector3 velocity = SteeringBehaviors.Evade(
            agent.transform.position,
            threat.position,
            threatVelocity,
            maxSpeed);

        return ApplySteeringToNavMesh(agent, velocity, evadeDistance);
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

    public static void EnsureOnNavMesh(NavMeshAgent agent, float sampleRadius = 15f)
    {
        if (agent == null || agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            agent.Warp(hit.position);
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
