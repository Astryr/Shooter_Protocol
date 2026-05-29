using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Entrega 2 — Robot Francotirador (enemigo complementario).
/// No reemplaza a FleeingRobot (E1): este mantiene distancia y usa Evade, no Flee.
///
/// FSM: Hold | Snipe | Evade
///   Hold  → Arrive al puesto de guardia + A*
///   Snipe → quieto, dispara con LoS a media/larga distancia
///   Evade → Steering Evade (predice al jugador) + A* si te acercás
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SniperRobot : MonoBehaviour
{
    public enum SniperState
    {
        Hold,
        Snipe,
        Evade
    }

    [Header("Entrega 2 — State Machine")]
    [SerializeField] SniperState currentState = SniperState.Hold;
    [SerializeField] float visionRange = 28f;
    [SerializeField] float minSnipeRange = 8f;

    [Header("Hold — Arrive")]
    [SerializeField] float holdSpeed = 2.5f;
    [SerializeField] float holdAcceleration = 8f;
    [SerializeField] float holdArrivalRadius = 2f;
    [SerializeField] float holdSteerDistance = 6f;

    [Header("Evade")]
    [SerializeField] float evadeTriggerDistance = 9f;
    [SerializeField] float resumeSnipeDistance = 13f;
    [SerializeField] float evadeSpeed = 7.5f;
    [SerializeField] float evadeAcceleration = 18f;
    [SerializeField] float evadeAngularSpeed = 300f;
    [SerializeField] float evadeRunDistance = 14f;

    [Header("Snipe")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] float fireRate = 2.5f;
    [SerializeField] int damage = 1;

    [Header("Line of Sight")]
    [SerializeField] LayerMask visionLayers;

    [Header("Visuals (Entrega 2)")]
    [SerializeField] Renderer glowingSphereRenderer;
    [SerializeField] [ColorUsage(true, true)] Color glowColor = new Color(0.45f, 0.2f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] bool showPathGizmos = true;
    [SerializeField] bool showSteeringGizmos = true;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    FirstPersonController player;
    NavMeshAgent agent;

    Vector3 guardPosition;
    float fireTimer;
    bool canSeePlayer;
    Vector3 lastSteeringVelocity;

    float normalSpeed;
    float normalAcceleration;
    float normalAngularSpeed;

    const string PLAYER_STRING = "Player";
    const string SPAWN_POINT_NAME = "Projectile Spawn Sniper Robot";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        player = FindFirstObjectByType<FirstPersonController>();
        guardPosition = transform.position;

        ResolveMissingReferences();
        EnemyMovement.EnsureOnNavMesh(agent);

        normalSpeed = agent.speed;
        normalAcceleration = agent.acceleration;
        normalAngularSpeed = agent.angularSpeed;

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        BeginHold();
        ApplyVisuals();
    }

    void Update()
    {
        if (!player) return;

        EnemyMovement.EnsureOnNavMesh(agent);

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        canSeePlayer = distanceToPlayer <= visionRange
            && EnemyVision.CanSeePlayer(transform.position, player.transform.position, visionRange, visionLayers);

        SniperState newState = currentState;

        if (distanceToPlayer < evadeTriggerDistance)
        {
            newState = SniperState.Evade;
        }
        else if (canSeePlayer
                 && distanceToPlayer >= minSnipeRange
                 && distanceToPlayer <= visionRange)
        {
            newState = SniperState.Snipe;
        }
        else if (currentState == SniperState.Evade && distanceToPlayer >= resumeSnipeDistance)
        {
            newState = canSeePlayer && distanceToPlayer >= minSnipeRange
                ? SniperState.Snipe
                : SniperState.Hold;
        }
        else if (currentState != SniperState.Evade)
        {
            newState = SniperState.Hold;
        }

        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        ExecuteState();
    }

    void OnStateEnter(SniperState state)
    {
        switch (state)
        {
            case SniperState.Hold:
                BeginHold();
                break;
            case SniperState.Snipe:
                agent.isStopped = true;
                agent.ResetPath();
                fireTimer = 0f;
                break;
            case SniperState.Evade:
                EnterEvadeState();
                break;
        }
    }

    void OnStateExit(SniperState state)
    {
        if (state == SniperState.Evade)
            RestoreNormalMovement();
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case SniperState.Hold:
                UpdateHoldState();
                break;
            case SniperState.Snipe:
                UpdateSnipeState();
                break;
            case SniperState.Evade:
                UpdateEvadeState();
                break;
        }
    }

    void BeginHold()
    {
        RestoreNormalMovement();
        agent.isStopped = false;
        agent.speed = holdSpeed;
        agent.acceleration = holdAcceleration;
    }

    void RestoreNormalMovement()
    {
        agent.speed = normalSpeed;
        agent.acceleration = normalAcceleration;
        agent.angularSpeed = normalAngularSpeed;
    }

    void UpdateHoldState()
    {
        float distToPost = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(guardPosition.x, 0f, guardPosition.z));

        if (distToPost <= holdArrivalRadius + 0.25f)
        {
            agent.isStopped = true;
            lastSteeringVelocity = Vector3.zero;
            return;
        }

        agent.isStopped = false;
        lastSteeringVelocity = SteeringBehaviors.Arrive(
            transform.position, guardPosition, agent.speed, holdArrivalRadius);

        EnemyMovement.NavigateWithArrive(
            agent, guardPosition, agent.speed, holdArrivalRadius, holdSteerDistance);
    }

    void UpdateSnipeState()
    {
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

    void EnterEvadeState()
    {
        agent.isStopped = false;
        agent.speed = evadeSpeed;
        agent.acceleration = evadeAcceleration;
        agent.angularSpeed = evadeAngularSpeed;
        agent.ResetPath();
        ApplyEvadeMovement();
    }

    void UpdateEvadeState()
    {
        agent.isStopped = false;
        ApplyEvadeMovement();
    }

    void ApplyEvadeMovement()
    {
        lastSteeringVelocity = SteeringBehaviors.Evade(
            transform.position,
            player.transform.position,
            EnemyMovement.GetTargetVelocity(player.transform),
            agent.speed);

        EnemyMovement.NavigateWithEvade(
            agent,
            player.transform,
            agent.speed,
            evadeRunDistance);
    }

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

    void ResolveMissingReferences()
    {
        if (projectileSpawnPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == SPAWN_POINT_NAME
                    || child.name == "Projectile Spawn Fleeing Robot")
                {
                    projectileSpawnPoint = child;
                    break;
                }
            }
        }

        if (glowingSphereRenderer == null)
            glowingSphereRenderer = GetComponentInChildren<Renderer>();
    }

    void ApplyVisuals()
    {
        if (glowingSphereRenderer == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        glowingSphereRenderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorID, glowColor);
        block.SetColor(BaseColorID, glowColor);
        glowingSphereRenderer.SetPropertyBlock(block);
    }

    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            EnemyMovement.DrawNavMeshPath(agent, new Color(0.55f, 0.25f, 1f, 0.9f));

        if (showSteeringGizmos)
        {
            Color steerColor = currentState == SniperState.Evade
                ? new Color(0.8f, 0.2f, 1f)
                : new Color(0.4f, 0.7f, 1f);
            EnemyMovement.DrawSteeringVector(transform.position, lastSteeringVelocity, steerColor);
        }

        Gizmos.color = new Color(0.45f, 0.2f, 1f, 0.25f);
        Gizmos.DrawWireSphere(guardPosition, holdArrivalRadius);
        Gizmos.color = new Color(0.45f, 0.2f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = new Color(1f, 0.2f, 0.5f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, evadeTriggerDistance);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_STRING))
            GetComponent<EnemyHealth>()?.SelfDestruct();
    }
}
