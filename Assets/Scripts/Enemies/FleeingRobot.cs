using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FleeingRobot: enemigo a distancia con FSM de 3 estados.
///
/// INTEGRACIÓN PATHFINDING + STEERING:
///   • Pathfinding: NavMesh (A*) — calcula la ruta óptima alrededor de obstáculos.
///   • Steering:    Wander (patrulla) + Evade (huida) via SteeringAgent.
///
/// Flujo de decisión:
///   FSM (Patrol/Attack/Flee)
///     → SteeringAgent.WanderAround() / Stop() / EvadeTarget()
///       → SteeringBehaviors calcula velocidad local
///       → NavMesh.desiredVelocity (próximo waypoint del path A*)
///       → Blending → NavMeshAgent.velocity
/// </summary>
[RequireComponent(typeof(SteeringAgent))]
public class FleeingRobot : MonoBehaviour
{
    public enum FleeingState
    {
        Patrol,
        Attack,
        Flee
    }

    [Header("State Machine")]
    [SerializeField] FleeingState currentState = FleeingState.Patrol;
    [SerializeField] float visionRange = 20f;
    [SerializeField] float safeDistance = 8f;
    [SerializeField] float patrolRadius = 15f;

    [Header("Attack Settings")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2f;
    [SerializeField] int damage = 1;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;
    [SerializeField] float losCheckInterval = 0.15f;

    [Header("Visuals")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = Color.yellow;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    SteeringAgent steeringAgent;

    float fireTimer = 0f;
    float losTimer = 0f;
    bool cachedCanSeePlayer = false;
    float cachedDistanceToPlayer = float.MaxValue;

    FleeingState previousState = FleeingState.Patrol;

    const string PLAYER_STRING = "Player";

    void Awake()
    {
        steeringAgent = GetComponent<SteeringAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();

        // Inicia con Wander para que el movimiento de patrulla sea orgánico
        steeringAgent.WanderAround();

        if (glowingSphereRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            glowingSphereRenderer.GetPropertyBlock(block);
            block.SetColor(EmissionColorID, glowColor);
            block.SetColor(BaseColorID, glowColor);
            glowingSphereRenderer.SetPropertyBlock(block);
        }
    }

    void Update()
    {
        if (!player) return;

        // Throttle del LoS check (no raycastear cada frame)
        losTimer += Time.deltaTime;
        if (losTimer >= losCheckInterval)
        {
            losTimer = 0f;
            cachedDistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            cachedCanSeePlayer = CheckLineOfSight();
        }

        // Toma de Decisiones (FSM)
        FleeingState newState = DetermineState();
        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        ExecuteState();
    }

    // ------------------------------------------------------------------
    // Transiciones de estado
    // ------------------------------------------------------------------

    FleeingState DetermineState()
    {
        if (cachedDistanceToPlayer < safeDistance && cachedCanSeePlayer)
            return FleeingState.Flee;
        if (cachedDistanceToPlayer <= visionRange && cachedCanSeePlayer)
            return FleeingState.Attack;
        return FleeingState.Patrol;
    }

    void OnStateEnter(FleeingState state)
    {
        switch (state)
        {
            case FleeingState.Patrol:
                // STEERING: Wander — movimiento aleatorio orgánico en patrulla
                steeringAgent.WanderAround();
                break;

            case FleeingState.Attack:
                // Se queda quieto para disparar
                steeringAgent.Stop();
                fireTimer = fireRate; // Dispara inmediatamente al detectar al jugador
                break;

            case FleeingState.Flee:
                // STEERING: Evade — predice hacia dónde va el jugador y escapa
                steeringAgent.EvadeTarget(player.transform);
                break;
        }
    }

    void OnStateExit(FleeingState state)
    {
        // Limpieza al salir de un estado si es necesario
    }

    // ------------------------------------------------------------------
    // Ejecución de estados
    // ------------------------------------------------------------------

    void ExecuteState()
    {
        switch (currentState)
        {
            case FleeingState.Patrol:
                // El Wander se maneja internamente por SteeringAgent
                break;

            case FleeingState.Attack:
                UpdateAttackState();
                break;

            case FleeingState.Flee:
                // El Evade se actualiza continuamente — re-apunta cada frame
                // para que la predicción de posición del jugador sea fresca
                steeringAgent.EvadeTarget(player.transform);
                break;
        }
    }

    void UpdateAttackState()
    {
        // Rota para mirar al jugador (sin moverse)
        Vector3 lookPos = player.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            ShootProjectile();
        }
    }

    // ------------------------------------------------------------------
    // Disparo
    // ------------------------------------------------------------------

    void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        Projectile newProjectile = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.identity
        ).GetComponent<Projectile>();

        newProjectile.transform.LookAt(player.transform.position + Vector3.up);
        newProjectile.Init(damage);
        newProjectile.SetColor(glowColor);
    }

    // ------------------------------------------------------------------
    // Line of Sight
    // ------------------------------------------------------------------

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = (player.transform.position + Vector3.up) - origin;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, visionRange, visionLayers))
            return hit.collider.CompareTag(PLAYER_STRING);

        return false;
    }

    // ------------------------------------------------------------------
    // Colisión
    // ------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
        {
            EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
            enemyHealth?.SelfDestruct();
        }
    }
}
