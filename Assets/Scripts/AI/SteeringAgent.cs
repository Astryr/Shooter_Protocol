using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Integra Pathfinding (A* via NavMesh) con Steering Behaviors locales.
///
/// ARQUITECTURA DE INTEGRACIÓN:
///
///   [FSM en Robot/FleeingRobot]
///         |
///         | llama a SeekTo / ArriveTo / PursueTarget / EvadeTarget / Wander / FleeTo
///         ↓
///   [SteeringAgent]  ←── este componente
///         |                  |
///         |                  ├── Pathfinding: NavMesh A* calcula la ruta óptima
///         |                  |   alrededor de obstáculos del mapa.
///         |                  |   Resultado: agent.desiredVelocity (dirección al
///         |                  |   siguiente waypoint del path)
///         |                  |
///         |                  └── Steering: SteeringBehaviors.cs calcula la
///         |                      velocidad deseada según el behavior activo.
///         |
///         | Blending final:
///         |   velocity = Lerp(navDesiredVelocity, steeringVelocity, steeringBlend)
///         ↓
///   [NavMeshAgent.velocity] → mueve el agente respetando el NavMesh
///
/// El pathfinding resuelve el "macromovimiento" (qué camino tomar),
/// el steering resuelve el "micromovimiento" (cómo se mueve localmente).
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
    [Tooltip("Radio de frenado para Arrive: empieza a desacelerar dentro de esta distancia al objetivo")]
    [SerializeField] float arrivalSlowingRadius = 3f;

    [Tooltip("Radio del círculo de Wander")]
    [SerializeField] float wanderRadius = 2.5f;

    [Tooltip("Distancia a la que se proyecta el círculo de Wander frente al agente")]
    [SerializeField] float wanderDistance = 4f;

    [Tooltip("Velocidad de variación aleatoria del ángulo de Wander (rad/seg)")]
    [SerializeField] float wanderJitter = 2.5f;

    [Tooltip("Peso del steering behavior vs la dirección del path NavMesh. " +
             "0 = solo pathfinding, 1 = solo steering.")]
    [Range(0f, 1f)]
    [SerializeField] float steeringBlend = 0.45f;

    [Header("Debug / Visualización")]
    [SerializeField] bool showGizmos = true;

    NavMeshAgent navAgent;
    float wanderAngle = 0f;

    // Estado del behavior activo y referencias de target
    ActiveBehavior currentBehavior = ActiveBehavior.None;
    Vector3 targetPosition;
    Transform targetTransform;

    // Velocidad de steering computada este frame (para Gizmos)
    Vector3 debugSteeringVelocity;

    // ------------------------------------------------------------------
    // Propiedades públicas de lectura
    // ------------------------------------------------------------------

    /// <summary>Acceso directo al NavMeshAgent subyacente.</summary>
    public NavMeshAgent NavAgent => navAgent;

    /// <summary>Behavior de steering actualmente activo.</summary>
    public ActiveBehavior CurrentBehavior => currentBehavior;

    // ------------------------------------------------------------------
    // Unity Messages
    // ------------------------------------------------------------------

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        ApplySteering();
    }

    // ------------------------------------------------------------------
    // API pública — la FSM del enemigo llama estos métodos
    // ------------------------------------------------------------------

    /// <summary>
    /// Seek: se mueve directamente hacia la posición dada a velocidad máxima.
    /// NavMesh calcula la ruta A*, steering aplica la fuerza de Seek localmente.
    /// </summary>
    public void SeekTo(Vector3 position)
    {
        currentBehavior = ActiveBehavior.Seek;
        targetPosition = position;
        navAgent.SetDestination(position);
    }

    /// <summary>
    /// Flee: calcula un punto de huida y usa NavMesh para llegar a él.
    /// Steering aplica la fuerza de Flee para el movimiento local.
    /// </summary>
    public void FleeTo(Vector3 threatPosition)
    {
        currentBehavior = ActiveBehavior.Flee;
        targetPosition = threatPosition;

        // NavMesh destino: punto en la dirección opuesta a la amenaza
        Vector3 fleeDir = (transform.position - threatPosition).normalized;
        float fleeRange = navAgent.speed * 2.5f;
        Vector3 fleeTarget = transform.position + fleeDir * fleeRange;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeRange, NavMesh.AllAreas))
            navAgent.SetDestination(hit.position);
    }

    /// <summary>
    /// Arrive: se mueve hacia la posición y desacelera suavemente al llegar.
    /// Ideal para waypoints de patrulla — no sobresobrepasa el destino.
    /// NavMesh calcula la ruta, Arrive controla la velocidad local.
    /// </summary>
    public void ArriveTo(Vector3 position)
    {
        currentBehavior = ActiveBehavior.Arrive;
        targetPosition = position;
        navAgent.SetDestination(position);
    }

    /// <summary>
    /// Wander: movimiento aleatorio orgánico. El steering genera una dirección
    /// aleatoria continua; NavMesh asegura que el agente no salga del mapa.
    /// </summary>
    public void WanderAround()
    {
        currentBehavior = ActiveBehavior.Wander;
    }

    /// <summary>
    /// Pursue: predice la posición futura del objetivo y usa NavMesh para
    /// ir hacia allá (más efectivo que Seek simple: intercepta en lugar de perseguir).
    /// </summary>
    public void PursueTarget(Transform target)
    {
        currentBehavior = ActiveBehavior.Pursue;
        targetTransform = target;
    }

    /// <summary>
    /// Evade: predice la posición futura de la amenaza y huye de ese punto.
    /// Inverso de Pursue. NavMesh busca camino de escape; Evade calcula hacia dónde.
    /// </summary>
    public void EvadeTarget(Transform target)
    {
        currentBehavior = ActiveBehavior.Evade;
        targetTransform = target;
    }

    /// <summary>Detiene el agente y limpia el behavior activo.</summary>
    public void Stop()
    {
        currentBehavior = ActiveBehavior.None;
        navAgent.ResetPath();
        debugSteeringVelocity = Vector3.zero;
    }

    // ------------------------------------------------------------------
    // Lógica interna de steering + pathfinding
    // ------------------------------------------------------------------

    void ApplySteering()
    {
        if (!navAgent.isOnNavMesh) return;
        if (currentBehavior == ActiveBehavior.None) return;

        float speed = navAgent.speed;
        Vector3 steeringVelocity = Vector3.zero;

        switch (currentBehavior)
        {
            // ── Seek ──────────────────────────────────────────────────
            case ActiveBehavior.Seek:
                steeringVelocity = SteeringBehaviors.Seek(transform.position, targetPosition, speed);
                break;

            // ── Flee ──────────────────────────────────────────────────
            case ActiveBehavior.Flee:
                steeringVelocity = SteeringBehaviors.Flee(transform.position, targetPosition, speed);
                break;

            // ── Arrive ────────────────────────────────────────────────
            case ActiveBehavior.Arrive:
                steeringVelocity = SteeringBehaviors.Arrive(
                    transform.position, targetPosition, speed, arrivalSlowingRadius);
                break;

            // ── Wander ────────────────────────────────────────────────
            case ActiveBehavior.Wander:
                steeringVelocity = SteeringBehaviors.Wander(
                    transform.position, transform.forward,
                    ref wanderAngle,
                    wanderRadius, wanderDistance, wanderJitter,
                    speed);

                // Para Wander, usamos la velocidad de steering directamente
                // como destino NavMesh (el pathfinding garantiza que esté en el mapa)
                Vector3 wanderDest = transform.position + steeringVelocity.normalized * wanderDistance;
                if (NavMesh.SamplePosition(wanderDest, out NavMeshHit wanderHit, wanderDistance * 1.5f, NavMesh.AllAreas))
                    navAgent.SetDestination(wanderHit.position);

                debugSteeringVelocity = steeringVelocity;
                // En Wander dejamos que NavMesh controle la velocidad final
                return;

            // ── Pursue ────────────────────────────────────────────────
            case ActiveBehavior.Pursue:
                if (targetTransform != null)
                {
                    Vector3 targetVel = GetVelocityOf(targetTransform);
                    steeringVelocity = SteeringBehaviors.Pursue(
                        transform.position, targetTransform.position, targetVel, speed);

                    // NavMesh destino: posición predicha del objetivo
                    float lookahead = Vector3.Distance(transform.position, targetTransform.position) / Mathf.Max(speed, 0.01f);
                    Vector3 predicted = targetTransform.position + targetVel * lookahead;
                    navAgent.SetDestination(predicted);
                }
                break;

            // ── Evade ─────────────────────────────────────────────────
            case ActiveBehavior.Evade:
                if (targetTransform != null)
                {
                    Vector3 threatVel = GetVelocityOf(targetTransform);
                    steeringVelocity = SteeringBehaviors.Evade(
                        transform.position, targetTransform.position, threatVel, speed);

                    // NavMesh destino: punto opuesto a la posición predicha de la amenaza
                    float lookahead = Vector3.Distance(transform.position, targetTransform.position) / Mathf.Max(speed, 0.01f);
                    Vector3 predictedThreat = targetTransform.position + threatVel * lookahead;
                    Vector3 evadeTarget = transform.position + (transform.position - predictedThreat).normalized * speed * 2f;

                    if (NavMesh.SamplePosition(evadeTarget, out NavMeshHit evadeHit, speed * 2.5f, NavMesh.AllAreas))
                        navAgent.SetDestination(evadeHit.position);
                }
                break;
        }

        debugSteeringVelocity = steeringVelocity;

        // ------------------------------------------------------------------
        // BLENDING: Combina la dirección del path NavMesh con el steering
        //
        //   navVelocity    = desiredVelocity del NavMeshAgent
        //                    (apunta al siguiente waypoint del path A*)
        //   steeringVelocity = velocidad deseada según el behavior activo
        //
        //   finalVelocity  = Lerp(navVelocity, steeringVelocity, steeringBlend)
        //
        // steeringBlend = 0 → movimiento puro por path (sin steering visible)
        // steeringBlend = 1 → movimiento puro por steering (puede ignorar path)
        // steeringBlend = 0.45 → equilibrio: path guía el macromovimiento,
        //                         steering refina el micromovimiento.
        // ------------------------------------------------------------------
        if (navAgent.hasPath || navAgent.pathPending)
        {
            Vector3 navVelocity = navAgent.desiredVelocity.sqrMagnitude > 0.01f
                ? navAgent.desiredVelocity
                : steeringVelocity;

            Vector3 blended = Vector3.Lerp(navVelocity, steeringVelocity, steeringBlend);
            navAgent.velocity = Vector3.ClampMagnitude(blended, speed);
        }
    }

    /// <summary>Obtiene la velocidad actual del transform objetivo.</summary>
    Vector3 GetVelocityOf(Transform target)
    {
        if (target.TryGetComponent(out NavMeshAgent targetAgent))
            return targetAgent.velocity;
        if (target.TryGetComponent(out CharacterController cc))
            return cc.velocity;
        return Vector3.zero;
    }

    // ------------------------------------------------------------------
    // Debug Gizmos
    // ------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || navAgent == null) return;

        // Path A* computado por NavMesh
        if (navAgent.hasPath)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);

            for (int i = 0; i < corners.Length; i++)
                Gizmos.DrawWireSphere(corners[i], 0.15f);
        }

        // Vector de steering actual
        if (debugSteeringVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                           debugSteeringVelocity.normalized * 2f);
        }

        // Círculo de Wander
        if (currentBehavior == ActiveBehavior.Wander)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f);
            Gizmos.DrawWireSphere(
                transform.position + transform.forward * wanderDistance,
                wanderRadius);
        }

        // Radio de Arrive
        if (currentBehavior == ActiveBehavior.Arrive)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
            Gizmos.DrawWireSphere(targetPosition, arrivalSlowingRadius);
        }
    }
}
