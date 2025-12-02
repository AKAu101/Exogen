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
    [SerializeField] private float safeZoneCheckDistance = 50f; // How far to detect safe zones
    [SerializeField] private float minDistanceFromSafeZone = 25f; // How close enemy will get to safe zones (when not in attack mode)
    [SerializeField] private float attackModeStationDistance = 5f; // How close enemy can get to station when in attack/chase mode
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

        // Check if player or enemy is near safe zone (use different distances based on state)
        bool playerInSafeZone = IsNearSafeZone(player.position, minDistanceFromSafeZone);
        bool enemyNearStation = IsNearSafeZone(transform.position, minDistanceFromSafeZone);

        // Determine state based on distance, light, and safe zones
        EnemyState newState = DetermineState(distanceToPlayer, hasLight, playerInSafeZone, enemyNearStation);

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

    private EnemyState DetermineState(float distance, bool hasLight, bool playerInSafeZone, bool enemyNearStation)
    {
        // PRIORITY 1: If player has light on, ALWAYS retreat immediately (regardless of state)
        if (hasLight && distance <= approachDistance)
        {
            return EnemyState.Retreat;
        }

        // If enemy itself is too close to station and NOT in chase mode, move away
        if (enemyNearStation && currentState != EnemyState.Chase)
        {
            return EnemyState.Retreat;
        }

        // If player is in safe zone and we're aware of them, don't approach
        if (playerInSafeZone && distance <= approachDistance)
        {
            // If already chasing, allow closer approach to station in attack mode
            if (currentState == EnemyState.Chase && distance <= approachDistance)
            {
                // Check if we're too close to station even in attack mode
                bool tooCloseToStation = IsNearSafeZone(transform.position, attackModeStationDistance);
                if (tooCloseToStation)
                {
                    return EnemyState.Retreat;
                }
                // Otherwise keep chasing even near the station
                return EnemyState.Chase;
            }
            return EnemyState.Wander;
        }

        // VICIOUS MODE: Once in Chase state, stay committed to the attack
        // Only stop if player gets too far away or enters safe zone
        if (currentState == EnemyState.Chase)
        {
            // Check if we're too close to station even in attack mode
            bool tooCloseToStation = IsNearSafeZone(transform.position, attackModeStationDistance);
            if (tooCloseToStation)
            {
                return EnemyState.Retreat;
            }

            // Keep chasing if player is within approach range and no light
            if (distance <= approachDistance && !hasLight && !playerInSafeZone)
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
                if (!IsNearSafeZone(hit.position, minDistanceFromSafeZone))
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
                if (!IsNearSafeZone(hit.position, minDistanceFromSafeZone))
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
                if (IsNearSafeZone(pathCorners[i], minDistanceFromSafeZone))
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
        // Even in chase mode, check if we're too close to the station
        bool tooCloseToStation = IsNearSafeZone(transform.position, attackModeStationDistance);

        if (!tooCloseToStation)
        {
            agent.isStopped = false;
            agent.speed = normalSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            // Don't get closer, but can still attack if in range
            agent.isStopped = true;
        }

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
        // Find nearest safe zone to retreat away from it
        GameObject[] safeZones = GameObject.FindGameObjectsWithTag(safeZoneTag);
        Vector3 retreatDirection = Vector3.zero;
        bool shouldRetreatFromStation = false;

        // Check if near any safe zone
        foreach (GameObject safeZone in safeZones)
        {
            float distanceToStation = Vector3.Distance(transform.position, safeZone.transform.position);
            if (distanceToStation <= minDistanceFromSafeZone)
            {
                // Retreat away from station
                retreatDirection += (transform.position - safeZone.transform.position).normalized;
                shouldRetreatFromStation = true;
            }
        }

        // Also retreat from player if they have light and are too close
        if (currentDistance < safeDistance)
        {
            retreatDirection += (transform.position - player.position).normalized;
        }

        if (retreatDirection != Vector3.zero || shouldRetreatFromStation)
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;

            retreatDirection.Normalize();
            Vector3 retreatPosition = transform.position + retreatDirection * 5f;

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

    private bool IsNearSafeZone(Vector3 position, float checkDistance)
    {
        // Find all objects with SafeZone tag
        GameObject[] safeZones = GameObject.FindGameObjectsWithTag(safeZoneTag);

        foreach (GameObject safeZone in safeZones)
        {
            float distanceToSafeZone = Vector3.Distance(position, safeZone.transform.position);

            if (distanceToSafeZone <= checkDistance)
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

        // Draw safe zone distances around stations
        GameObject[] safeZones = GameObject.FindGameObjectsWithTag(safeZoneTag);
        foreach (GameObject safeZone in safeZones)
        {
            if (safeZone != null)
            {
                // Normal minimum distance from station (yellow)
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Gizmos.DrawWireSphere(safeZone.transform.position, minDistanceFromSafeZone);

                // Attack mode allowed distance (orange)
                Gizmos.color = new Color(1, 0.5f, 0, 0.7f);
                Gizmos.DrawWireSphere(safeZone.transform.position, attackModeStationDistance);
            }
        }
    }
}