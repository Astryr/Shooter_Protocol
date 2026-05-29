using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Entrega 2 — Robot Cargador (enemigo complementario).
/// No reemplaza a Robot / FleeingRobot / Turret (Entrega 1).
///
/// FSM: Patrol | Charge | Recover
///   Patrol  → Steering Wander + Pathfinding A* (NavMesh)
///   Charge  → Steering Seek hacia el jugador + A*
///   Recover → pausa tras perder al jugador, luego vuelve a Wander
///
/// Combate: sin proyectiles; al tocar al jugador inflige daño y se autodestruye.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ChargerRobot : MonoBehaviour
{
    public enum ChargerState
    {
        Patrol,
        Charge,
        Recover
    }

    [Header("Entrega 2 — State Machine")]
    [SerializeField] ChargerState currentState = ChargerState.Patrol;
    [SerializeField] float visionRange = 18f;

    [Header("Patrol — Wander")]
    [SerializeField] float patrolSpeed = 3.5f;
    [SerializeField] float patrolAcceleration = 10f;
    [SerializeField] float patrolAngularSpeed = 200f;
    [SerializeField] float wanderRadius = 2f;
    [SerializeField] float wanderDistance = 4f;
    [SerializeField] float wanderJitter = 25f;
    [SerializeField] float wanderSteerDistance = 5f;

    [Header("Charge — Seek")]
    [SerializeField] float chargeSpeed = 9f;
    [SerializeField] float chargeAcceleration = 24f;
    [SerializeField] float chargeAngularSpeed = 360f;
    [SerializeField] float seekSteerDistance = 12f;
    [SerializeField] float maxChargeDuration = 8f;

    [Header("Recover")]
    [SerializeField] float recoverDuration = 2f;

    [Header("Contact")]
    [SerializeField] int contactDamage = 2;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;

    [Header("Visuals (Entrega 2)")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = new Color(1f, 0.25f, 0.1f, 1f);

    [Header("Debug")]
    [SerializeField] bool showPathGizmos = true;
    [SerializeField] bool showSteeringGizmos = true;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    PlayerHealth playerHealth;
    NavMeshAgent agent;

    float wanderAngle;
    float recoverTimer;
    float chargeTimer;
    bool canSeePlayer;
    Vector3 lastSteeringVelocity;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ApplyEntrega2Defaults();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        ResolveGlowRenderer();
        EnemyMovement.EnsureOnNavMesh(agent);

        agent.stoppingDistance = 0.35f;
        agent.isStopped = false;
        BeginPatrol();
        ApplyVisuals();
    }

    void ApplyEntrega2Defaults()
    {
        agent.acceleration = patrolAcceleration;
        agent.angularSpeed = patrolAngularSpeed;
        agent.speed = patrolSpeed;
    }

    void ResolveGlowRenderer()
    {
        if (glowingSphereRenderer != null) return;

        Transform glow = transform.Find("Model/HoveringRobot02/Sphere Glow");
        if (glow == null)
            glow = transform.Find("Sphere Glow");

        if (glow != null)
            glowingSphereRenderer = glow.GetComponent<Renderer>();
    }

    void Update()
    {
        if (!player) return;

        EnemyMovement.EnsureOnNavMesh(agent);

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        canSeePlayer = distanceToPlayer <= visionRange
            && EnemyVision.CanSeePlayer(transform.position, player.transform.position, visionRange, visionLayers);

        ChargerState newState = currentState;

        switch (currentState)
        {
            case ChargerState.Patrol:
                if (canSeePlayer)
                    newState = ChargerState.Charge;
                break;

            case ChargerState.Charge:
                chargeTimer += Time.deltaTime;
                if (!canSeePlayer || chargeTimer >= maxChargeDuration)
                    newState = ChargerState.Recover;
                break;

            case ChargerState.Recover:
                recoverTimer += Time.deltaTime;
                if (recoverTimer >= recoverDuration)
                    newState = ChargerState.Patrol;
                else if (canSeePlayer)
                    newState = ChargerState.Charge;
                break;
        }

        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        ExecuteState();
    }

    void OnStateEnter(ChargerState state)
    {
        switch (state)
        {
            case ChargerState.Patrol:
                BeginPatrol();
                break;
            case ChargerState.Charge:
                chargeTimer = 0f;
                agent.isStopped = false;
                agent.speed = chargeSpeed;
                agent.acceleration = chargeAcceleration;
                agent.angularSpeed = chargeAngularSpeed;
                break;
            case ChargerState.Recover:
                recoverTimer = 0f;
                agent.isStopped = true;
                agent.ResetPath();
                break;
        }
    }

    void OnStateExit(ChargerState state) { }

    void ExecuteState()
    {
        switch (currentState)
        {
            case ChargerState.Patrol:
                UpdatePatrolWander();
                break;
            case ChargerState.Charge:
                UpdateChargeSeek();
                break;
            case ChargerState.Recover:
                lastSteeringVelocity = Vector3.zero;
                break;
        }
    }

    void BeginPatrol()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;
        agent.angularSpeed = patrolAngularSpeed;
    }

    void UpdatePatrolWander()
    {
        lastSteeringVelocity = SteeringBehaviors.Wander(
            transform.position,
            transform.forward,
            ref wanderAngle,
            wanderRadius,
            wanderDistance,
            wanderJitter,
            agent.speed);

        EnemyMovement.NavigateWithWander(
            agent,
            ref wanderAngle,
            wanderRadius,
            wanderDistance,
            wanderJitter,
            agent.speed,
            wanderSteerDistance);
    }

    void UpdateChargeSeek()
    {
        lastSteeringVelocity = SteeringBehaviors.Seek(
            transform.position,
            player.transform.position,
            agent.speed);

        EnemyMovement.NavigateWithSeek(
            agent,
            player.transform.position,
            agent.speed,
            seekSteerDistance);
    }

    void ApplyVisuals()
    {
        if (glowingSphereRenderer == null) return;

        Material mat = glowingSphereRenderer.material;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor(EmissionColorID, glowColor);
        mat.SetColor(BaseColorID, glowColor);
    }

    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            EnemyMovement.DrawNavMeshPath(agent, new Color(1f, 0.35f, 0.1f, 0.9f));

        if (showSteeringGizmos)
        {
            Color steerColor = currentState == ChargerState.Charge ? Color.red : new Color(1f, 0.6f, 0.1f);
            EnemyMovement.DrawSteeringVector(transform.position, lastSteeringVelocity, steerColor);
        }

        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_STRING)) return;

        playerHealth?.TakeDamage(contactDamage);
        GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
