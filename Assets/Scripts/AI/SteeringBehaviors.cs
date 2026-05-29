using UnityEngine;

/// <summary>
/// Biblioteca de Steering Behaviors (Comportamientos de Dirección).
///
/// SEPARACIÓN DE RESPONSABILIDADES:
///   - Pathfinding (A* via NavMesh): resuelve POR DÓNDE ir, calculando la ruta
///     óptima alrededor de obstáculos del mapa.
///   - Steering Behaviors: resuelven CÓMO se desplaza el agente localmente,
///     generando fuerzas de movimiento orgánicas y reactivas.
///
/// Cada método recibe el estado actual del agente y retorna una velocidad
/// deseada (Vector3). EnemyMovement combina este resultado con la
/// dirección del path calculado por NavMesh.
///
/// Behaviors implementados:
///   Seek    — Se mueve directamente hacia el objetivo.
///   Flee    — Se aleja del objetivo/amenaza.
///   Arrive  — Como Seek pero desacelera suavemente al acercarse.
///   Wander  — Movimiento aleatorio continuo basado en un círculo proyectado.
///   Pursue  — Predice la posición futura del objetivo y hace Seek hacia allá.
///   Evade   — Predice la posición futura de la amenaza y hace Flee de allá.
/// </summary>
public static class SteeringBehaviors
{
    // ------------------------------------------------------------------
    // SEEK
    // Calcula velocidad deseada apuntando directo al objetivo a velocidad máxima.
    // ------------------------------------------------------------------
    public static Vector3 Seek(Vector3 position, Vector3 target, float maxSpeed)
    {
        Vector3 toTarget = target - position;
        if (toTarget.sqrMagnitude < 0.001f) return Vector3.zero;
        return toTarget.normalized * maxSpeed;
    }

    // ------------------------------------------------------------------
    // FLEE
    // Calcula velocidad deseada alejándose del origen de amenaza a velocidad máxima.
    // ------------------------------------------------------------------
    public static Vector3 Flee(Vector3 position, Vector3 threat, float maxSpeed)
    {
        Vector3 awayFromThreat = position - threat;
        if (awayFromThreat.sqrMagnitude < 0.001f) return Vector3.zero;
        return awayFromThreat.normalized * maxSpeed;
    }

    // ------------------------------------------------------------------
    // ARRIVE
    // Como Seek, pero reduce la velocidad linealmente dentro del radio de llegada.
    // Útil para patrullas: el agente no sobresobrepasa el waypoint.
    // ------------------------------------------------------------------
    public static Vector3 Arrive(Vector3 position, Vector3 target, float maxSpeed, float slowingRadius)
    {
        Vector3 toTarget = target - position;
        float distance = toTarget.magnitude;

        if (distance < 0.01f) return Vector3.zero;

        // Velocidad proporcional a la distancia dentro del radio de frenado
        float rampedSpeed = maxSpeed * (distance / Mathf.Max(slowingRadius, 0.01f));
        float clampedSpeed = Mathf.Min(rampedSpeed, maxSpeed);

        return toTarget * (clampedSpeed / distance);
    }

    // ------------------------------------------------------------------
    // WANDER
    // Genera movimiento aleatorio continuo proyectando un círculo frente al agente.
    // El ángulo del círculo se perturba levemente cada frame (wanderJitter).
    // wanderAngle: ángulo persistente pasado por referencia (estado del agente).
    // ------------------------------------------------------------------
    public static Vector3 Wander(
        Vector3 position,
        Vector3 forward,
        ref float wanderAngle,
        float wanderRadius,
        float wanderDistance,
        float wanderJitter,
        float maxSpeed)
    {
        // Perturbación aleatoria del ángulo (escalada por tiempo para framerate-independence)
        wanderAngle += Random.Range(-wanderJitter, wanderJitter) * Time.deltaTime;

        // Centro del círculo proyectado frente al agente
        Vector3 circleCenter = position + forward.normalized * wanderDistance;

        // Punto en el borde del círculo según el ángulo actual
        Vector3 displacement = new Vector3(
            Mathf.Cos(wanderAngle) * wanderRadius,
            0f,
            Mathf.Sin(wanderAngle) * wanderRadius
        );

        Vector3 wanderTarget = circleCenter + displacement;
        return Seek(position, wanderTarget, maxSpeed);
    }

    // ------------------------------------------------------------------
    // PURSUE
    // Predice dónde estará el objetivo en el futuro y hace Seek hacia allá.
    // El tiempo de predicción (lookahead) es proporcional a la distancia actual.
    // Más efectivo que Seek simple: el agente intercepta, no persigue la cola.
    // ------------------------------------------------------------------
    public static Vector3 Pursue(
        Vector3 position,
        Vector3 targetPosition,
        Vector3 targetVelocity,
        float maxSpeed)
    {
        float distance = Vector3.Distance(position, targetPosition);
        // Tiempo de anticipación: cuanto más lejos, más adelante predecimos
        float lookahead = (maxSpeed > 0f) ? distance / maxSpeed : 0f;
        Vector3 predictedPosition = targetPosition + targetVelocity * lookahead;
        return Seek(position, predictedPosition, maxSpeed);
    }

    // ------------------------------------------------------------------
    // EVADE
    // Predice dónde estará la amenaza en el futuro y hace Flee desde allá.
    // Inverso de Pursue: el agente se adelanta al movimiento del perseguidor.
    // ------------------------------------------------------------------
    public static Vector3 Evade(
        Vector3 position,
        Vector3 threatPosition,
        Vector3 threatVelocity,
        float maxSpeed)
    {
        float distance = Vector3.Distance(position, threatPosition);
        float lookahead = (maxSpeed > 0f) ? distance / maxSpeed : 0f;
        Vector3 predictedThreatPosition = threatPosition + threatVelocity * lookahead;
        return Flee(position, predictedThreatPosition, maxSpeed);
    }
}
