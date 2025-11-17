using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
///     Manages player health, death, and respawning.
///     Integrates with Oxygen system to trigger death when oxygen depletes.
///     Spawns a death chest with player's inventory items on death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Respawn Settings")]
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private float respawnDelay = 2f;

    [Header("References")]
    [SerializeField] private Oxygen oxygenSystem;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FirstPersonController playerController;

    [Header("UI (Optional)")]
    [SerializeField] private Slider healthSlider;

    [Header("Death Chest")]
    [SerializeField] private bool dropItemsOnDeath = true;

    private bool isDead = false;
    private IInventorySystem inventorySystem;
    private IInventoryData playerInventory;

    private void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Get references if not assigned
        if (oxygenSystem == null)
        {
            oxygenSystem = GetComponent<Oxygen>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonController>();
        }

        // Get inventory system reference
        if (ServiceLocator.Instance.IsRegistered<IInventorySystem>())
        {
            inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
            playerInventory = InventorySystem.Instance.PlayerInventory;
            DebugManager.Log("PlayerHealth: Inventory system found");
        }
        else
        {
            DebugManager.LogWarning("PlayerHealth: IInventorySystem not found in ServiceLocator!");
        }

        // Validate spawn position
        if (spawnPosition == null)
        {
            DebugManager.LogWarning("PlayerHealth: No spawn position assigned! Using player's starting position.");
            // Create a spawn point at player's starting position
            GameObject spawnObj = new GameObject("PlayerSpawnPoint");
            spawnObj.transform.position = transform.position;
            spawnObj.transform.rotation = transform.rotation;
            spawnPosition = spawnObj.transform;
        }

        DebugManager.Log("PlayerHealth: Initialized with max health: " + maxHealth);
    }

    private void Update()
    {
        // Check if oxygen has depleted
        if (oxygenSystem != null && !isDead)
        {
            if (oxygenSystem.IsOxygenDepleted())
            {
                DebugManager.Log("PlayerHealth: Oxygen depleted, setting health to zero");
                currentHealth = 0f;
                Die();
            }
        }

        // Check if health reached zero
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Apply damage to the player
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        DebugManager.Log($"PlayerHealth: Took {damage} damage. Current health: {currentHealth}");
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        DebugManager.Log($"PlayerHealth: Healed {amount}. Current health: {currentHealth}");
        UpdateHealthUI();
    }

    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        DebugManager.Log("PlayerHealth: Player died");

        // Spawn death chest with player's items
        if (dropItemsOnDeath && inventorySystem != null && playerInventory != null)
        {
            Vector3 deathPosition = transform.position;
            GameObject chest = inventorySystem.SpawnDropChest(playerInventory, deathPosition);

            if (chest != null)
            {
                DebugManager.Log($"PlayerHealth: Death chest spawned at {deathPosition}");
            }
            else
            {
                DebugManager.Log("PlayerHealth: No items to drop or chest failed to spawn");
            }
        }

        // Disable player controls
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Start respawn coroutine
        StartCoroutine(RespawnCoroutine());
    }

    /// <summary>
    /// Respawn the player after a delay
    /// </summary>
    private IEnumerator RespawnCoroutine()
    {
        DebugManager.Log($"PlayerHealth: Respawning in {respawnDelay} seconds...");

        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    /// <summary>
    /// Respawn the player at the spawn position
    /// </summary>
    private void Respawn()
    {
        DebugManager.Log("PlayerHealth: Respawning player");

        // Disable CharacterController temporarily to allow teleportation
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Move player to spawn position
        if (spawnPosition != null)
        {
            transform.position = spawnPosition.position;
            transform.rotation = spawnPosition.rotation;
            DebugManager.Log($"PlayerHealth: Player moved to spawn position: {spawnPosition.position}");
        }

        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Reset health
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Reset oxygen
        if (oxygenSystem != null)
        {
            oxygenSystem.ResetOxygen();
            DebugManager.Log("PlayerHealth: Oxygen system reset");
        }

        // Re-enable player controls
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        isDead = false;
        DebugManager.Log("PlayerHealth: Player respawned successfully");
    }

    /// <summary>
    /// Update health UI if slider is assigned
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    // Public getters
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
}
