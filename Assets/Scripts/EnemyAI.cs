using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     Enhanced Enemy AI that uses NavMesh pathfinding.
///     Maintains distance from player, approaches when lantern is off, and attacks when close with light on.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LanternController lanternController;

    [Header("Distance Settings")]
    [SerializeField] private float safeDistance = 30f; // How far the enemy wants to stay away when you have light
    [SerializeField] private float attackDistance = 9f; // Distance where the enemy switches from approaching to attacking
    [SerializeField] private float approachDistance = 100f; // How far away it detects the player

    [Header("Movement Settings")]
    [SerializeField] private float normalSpeed = 3.5f; // Speed when chasing/attacking the player
    [SerializeField] private float approachSpeed = 2f; // Slow creeping speed when approaching in the dark
    [SerializeField] private float retreatSpeed = 6f; // Speed when backing away from light

    [Header("Wandering Settings")]
    [SerializeField] private float wanderRadius = 20f; // How far from current position to wander
    [SerializeField] private float wanderTimer = 8f; // Time between new wander points
    [SerializeField] private float minWanderDistance = 5f; // Minimum distance for a wander point to be valid
    [SerializeField] private float wanderSpeed = 1.5f; // Movement speed while wandering

    [Header("Stalking Settings")]
    [SerializeField] private bool enableStalking = true; // Whether enemy circles player before attacking
    [SerializeField] private float stalkCircleRadius = 8f; // Distance to maintain while circling player
    [SerializeField] private float stalkSpeed = 2f; // Movement speed during stalking
    [SerializeField] private float stalkUpdateRate = 2f; // How often to pick new stalk position (seconds)
    [SerializeField] private float stalkDuration = 15f; // How long to stalk before approaching

    [Header("Safe Zone Settings")]
    [SerializeField] private float safeZoneCheckDistance = 30f; // How far to detect safe zones
    [SerializeField] private float minDistanceFromSafeZone = 12f; // How close enemy will get to safe zones
    [SerializeField] private string safeZoneTag = "SafeZone"; // Tag for safe zone objects (stations)

    [Header("Pack Behavior Settings")]
    [SerializeField] private bool enablePackBehavior = true; // Whether enemies alert each other when spotting player
    [SerializeField] private float packCommunicationRange = 40f; // How far alerts reach other enemies
    [SerializeField] private float packAlertCooldown = 5f; // Time between alerts to prevent spam

    [Header("Behavior Settings")]
    [SerializeField] private float updateRate = 0.2f; // How often to update AI decisions (seconds)

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f; // Time between attacks (seconds)
    [SerializeField] private int attackDamage = 30; // Damage dealt per attack
    [SerializeField] private float attackReach = 2f; // How close enemy must be to hit player

    // Components
    private NavMeshAgent agent;

    // State
    private enum EnemyState
    {
        Idle,       // Too far away or at safe distance with light
        Wander,     // Patrolling/wandering the forest
        Stalk,      // Circling player, staying at distance
        Approach,   // Moving closer when light is off
        Chase,      // Chasing player (close + light on)
        Retreat     // Moving away to safe distance
    }

    private EnemyState currentState = EnemyState.Wander;
    private float updateTimer;
    private float lastAttackTime;

    // Wandering
    private float wanderCooldown;
    private Vector3 wanderTarget;
    private bool hasWanderTarget;

    // Stalking
    private float stalkAngle;
    private float stalkUpdateTimer;
    private float stalkStartTime;

    // Pack Behavior
    private static List<EnemyAI> activeHounds = new List<EnemyAI>();
    private float lastPackAlertTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (enablePackBehavior && !activeHounds.Contains(this))
        {
            activeHounds.Add(this);
        }
    }

    private void OnDisable()
    {
        if (activeHounds.Contains(this))
        {
            activeHounds.Remove(this);
        }
    }

    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("EnemyAI: Player not found! Make sure player has 'Player' tag.");
            }
        }

        // Find LanternController if not assigned
        if (lanternController == null)
        {
            lanternController = FindFirstObjectByType<LanternController>();
            if (lanternController == null)
            {
                Debug.LogWarning("EnemyAI: LanternController not found! Enemy will assume light is always off.");
            }
        }

        // Initialize wandering
        wanderCooldown = Random.Range(0f, wanderTimer);
        stalkAngle = Random.Range(0f, 360f);
    }

    private void Update()
    {
        if (player == null) return;

        // Check if agent is on NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("EnemyAI: Agent is not on NavMesh!");
            return;
        }

        // Update behavior at intervals instead of every frame
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateRate)
        {
            updateTimer = 0f;
            UpdateBehavior();
        }
    }

    private void UpdateBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool hasLight = IsPlayerLightOn();
        bool playerInSafeZone = IsNearSafeZone(player.position);

        // Determine state based on distance, light, and safe zones
        EnemyState newState = DetermineState(distanceToPlayer, hasLight, playerInSafeZone);

        // Only log state changes
        if (newState != currentState)
        {
            Debug.Log($"EnemyAI: State changed from {currentState} to {newState}");
            
            // Alert pack when detecting player
            if ((newState == EnemyState.Stalk || newState == EnemyState.Approach || newState == EnemyState.Chase) 
                && currentState == EnemyState.Wander)
            {
                AlertNearbyHounds();
            }
            
            // Initialize stalking timer when entering stalk state
            if (newState == EnemyState.Stalk && currentState != EnemyState.Stalk)
            {
                stalkStartTime = Time.time;
            }
            
            currentState = newState;
        }

        // Execute behavior based on state
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;

            case EnemyState.Wander:
                HandleWander();
                break;

            case EnemyState.Stalk:
                HandleStalk(distanceToPlayer);
                break;

            case EnemyState.Approach:
                HandleApproach(distanceToPlayer, playerInSafeZone);
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Retreat:
                HandleRetreat(distanceToPlayer);
                break;
        }
    }

    private EnemyState DetermineState(float distance, bool hasLight, bool playerInSafeZone)
    {
        // If player is in safe zone and we're aware of them, don't approach
        if (playerInSafeZone && distance <= approachDistance)
        {
            // If already chasing, retreat from safe zone
            if (currentState == EnemyState.Chase && distance <= approachDistance)
            {
                return EnemyState.Retreat;
            }
            return EnemyState.Wander;
        }

        // VICIOUS MODE: Once in Chase state, stay committed to the attack
        // Only stop if player gets too far away or enters safe zone
        if (currentState == EnemyState.Chase)
        {
            // Keep chasing if player is within approach range (don't let light stop the attack)
            if (distance <= approachDistance && !playerInSafeZone)
            {
                return EnemyState.Chase;
            }
            // Only retreat if player escapes far enough
            else if (distance > approachDistance)
            {
                return EnemyState.Wander;
            }
        }

        // If player is very close AND no light, start attacking
        if (distance <= attackDistance && !hasLight && !playerInSafeZone)
        {
            return EnemyState.Chase;
        }

        // Only retreat if NOT already chasing (light scares during approach, not during attack)
        if (distance < safeDistance && hasLight && currentState != EnemyState.Chase)
        {
            return EnemyState.Retreat;
        }

        // If within approach range and no light
        if (distance <= approachDistance && !hasLight && !playerInSafeZone)
        {
            // Use stalking behavior if enabled and haven't stalked too long
            if (enableStalking && distance > attackDistance * 1.5f)
            {
                // Check if we should transition from stalk to approach
                if (currentState == EnemyState.Stalk)
                {
                    float stalkTime = Time.time - stalkStartTime;
                    if (stalkTime >= stalkDuration)
                    {
                        return EnemyState.Approach;
                    }
                }
                return EnemyState.Stalk;
            }
            else
            {
                return EnemyState.Approach;
            }
        }

        // Default to wandering behavior
        return EnemyState.Wander;
    }

    private void HandleIdle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    private void HandleWander()
    {
        wanderCooldown -= updateRate;

        if (!hasWanderTarget || wanderCooldown <= 0f)
        {
            // Find new wander point
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y; // Keep on same height

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                // Make sure wander point is not in a safe zone
                if (!IsNearSafeZone(hit.position))
                {
                    float distanceToWanderPoint = Vector3.Distance(transform.position, hit.position);
                    
                    if (distanceToWanderPoint >= minWanderDistance)
                    {
                        wanderTarget = hit.position;
                        hasWanderTarget = true;
                        agent.isStopped = false;
                        agent.speed = wanderSpeed;
                        agent.SetDestination(wanderTarget);
                        wanderCooldown = wanderTimer;
                    }
                }
            }
        }

        // Check if reached wander target
        if (hasWanderTarget && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            hasWanderTarget = false;
            agent.isStopped = true;
            wanderCooldown = wanderTimer; // Wait before next wander
        }
    }

    private void HandleStalk(float currentDistance)
    {
        stalkUpdateTimer -= updateRate;

        if (stalkUpdateTimer <= 0f)
        {
            // Circle around player at stalking distance
            // Randomly vary the angle to make it unpredictable
            stalkAngle += Random.Range(45f, 135f);
            
            // Calculate position around player
            Vector3 directionFromPlayer = Quaternion.Euler(0, stalkAngle, 0) * Vector3.forward;
            Vector3 circlePosition = player.position + directionFromPlayer * stalkCircleRadius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(circlePosition, out hit, 5f, NavMesh.AllAreas))
            {
                // Don't stalk into safe zones
                if (!IsNearSafeZone(hit.position))
                {
                    agent.isStopped = false;
                    agent.speed = stalkSpeed;
                    agent.SetDestination(hit.position);
                }
            }

            stalkUpdateTimer = stalkUpdateRate;
        }
    }

    private void HandleApproach(float currentDistance, bool playerInSafeZone)
    {
        // Don't approach if player is in safe zone
        if (playerInSafeZone)
        {
            agent.isStopped = true;
            return;
        }

        // Check if our path would take us near a safe zone
        if (agent.hasPath)
        {
            // Sample points along the path
            Vector3[] pathCorners = agent.path.corners;
            for (int i = 0; i < pathCorners.Length; i++)
            {
                if (IsNearSafeZone(pathCorners[i]))
                {
                    // Path goes through safe zone, stop approaching
                    agent.isStopped = true;
                    return;
                }
            }
        }

        // Continue approaching until very close
        if (currentDistance > attackReach)
        {
            agent.isStopped = false;
            agent.speed = approachSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    private void HandleChase()
    {
        agent.isStopped = false;
        agent.speed = normalSpeed;
        agent.SetDestination(player.position);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackReach)
        {
            agent.isStopped = true;
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        Debug.Log($"EnemyAI: Attacking player for {attackDamage} damage!");

        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        else
        {
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void HandleRetreat(float currentDistance)
    {
        if (currentDistance < safeDistance)
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;

            Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
            Vector3 retreatPosition = transform.position + directionAwayFromPlayer * 3f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPosition, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    private bool IsPlayerLightOn()
    {
        if (lanternController == null) return false;
        return lanternController.IsLightOn;
    }

    private bool IsNearSafeZone(Vector3 position)
    {
        // Find all objects with SafeZone tag
        GameObject[] safeZones = GameObject.FindGameObjectsWithTag(safeZoneTag);
        
        foreach (GameObject safeZone in safeZones)
        {
            float distanceToSafeZone = Vector3.Distance(position, safeZone.transform.position);
            
            if (distanceToSafeZone <= minDistanceFromSafeZone)
            {
                return true;
            }
        }
        
        return false;
    }

    // ========== PACK BEHAVIOR ==========
    
    private void AlertNearbyHounds()
    {
        if (!enablePackBehavior) return;
        
        // Cooldown to prevent spam
        if (Time.time < lastPackAlertTime + packAlertCooldown)
            return;
        
        lastPackAlertTime = Time.time;
        
        foreach (var hound in activeHounds)
        {
            if (hound != this && hound != null)
            {
                float distanceToHound = Vector3.Distance(transform.position, hound.transform.position);
                
                if (distanceToHound <= packCommunicationRange)
                {
                    hound.OnPackAlert(player.position, transform.position);
                }
            }
        }
        
        Debug.Log($"EnemyAI: Alerted {activeHounds.Count - 1} nearby hounds in pack!");
    }

    public void OnPackAlert(Vector3 alertPosition, Vector3 alerterPosition)
    {
        if (!enablePackBehavior) return;
        
        // Only respond if we're wandering or idle
        if (currentState == EnemyState.Wander || currentState == EnemyState.Idle)
        {
            Debug.Log($"EnemyAI: Received pack alert! Investigating position.");
            
            // Move toward the alert position (but not directly on it)
            Vector3 investigatePosition = alertPosition + Random.insideUnitSphere * 5f;
            investigatePosition.y = transform.position.y;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(investigatePosition, out hit, 10f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.speed = approachSpeed * 1.2f; // Slightly faster when investigating
                agent.SetDestination(hit.position);
            }
        }
    }

    // ========== DEBUG VISUALIZATION ==========
    
    private void OnDrawGizmosSelected()
    {
        // Draw approach distance (detection range)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, approachDistance);

        // Draw stalking circle
        if (enableStalking)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stalkCircleRadius);
        }

        // Draw safe distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, safeDistance);

        // Draw attack distance (when to enter Chase state)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Draw attack reach
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackReach);

        // Draw wander radius
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Draw current wander target
        if (hasWanderTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawWireSphere(wanderTarget, 1f);
        }

        // Draw pack communication range
        if (enablePackBehavior)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, packCommunicationRange);
        }

        // Draw safe zone detection range
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, safeZoneCheckDistance);
    }
}