using UnityEngine;

public class PlayerConsumptionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Oxygen oxygenSystem;
    [SerializeField] private FirstPersonController playerController;
    
    [Header("Consumption Settings")]
    [SerializeField] private float consumptionCooldown = 0.5f;
    
    private float lastConsumeTime = -Mathf.Infinity;
    private bool isConsuming = false;
    private ItemData currentConsumable;
    private float consumeTimer = 0f;
    
    void Start()
    {
        // Find references if not assigned
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
            
        if (oxygenSystem == null)
            oxygenSystem = GetComponent<Oxygen>();
            
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();
    }
    
    void Update()
    {
        // Handle consumption timer
        if (isConsuming)
        {
            consumeTimer += Time.deltaTime;
            
            if (consumeTimer >= currentConsumable.consumeTime)
            {
                FinishConsumption();
            }
        }
    }
    
    /// <summary>
    /// Try to consume an item
    /// </summary>
    public bool TryConsumeItem(ItemData itemData)
    {
        // Check if item is consumable
        if (itemData == null || !itemData.isConsumable)
        {
            DebugManager.LogWarning($"PlayerConsumptionManager: Item {itemData?.name} is not consumable!");
            return false;
        }
        
        // Check cooldown
        if (Time.time < lastConsumeTime + consumptionCooldown)
        {
            DebugManager.Log("PlayerConsumptionManager: Cannot consume yet - cooldown active");
            return false;
        }
        
        // Start consumption process
        StartConsumption(itemData);
        return true;
    }
    
    private void StartConsumption(ItemData itemData)
    {
        currentConsumable = itemData;
        isConsuming = true;
        consumeTimer = 0f;
        
        DebugManager.Log($"PlayerConsumptionManager: Started consuming {itemData.itemName} (takes {itemData.consumeTime}s)");
        
        // Optional: Show consumption UI or animation here
        
        // Optional: Slow down player movement while consuming
        if (playerController != null)
        {
            // You could slow down movement speed here
            // playerController.SetMovementMultiplier(0.5f);
        }
    }
    
    private void FinishConsumption()
    {
        if (currentConsumable == null) return;
        
        // Apply health restoration
        if (currentConsumable.healthRestore > 0 && playerHealth != null)
        {
            playerHealth.Heal(currentConsumable.healthRestore);
            DebugManager.Log($"PlayerConsumptionManager: Restored {currentConsumable.healthRestore} health");
        }
        
        // Apply oxygen restoration
        if (currentConsumable.oxygenRestore > 0 && oxygenSystem != null)
        {
            // Since your Oxygen script doesn't have a Heal method, we'll need to access the oxygen level directly
            // You might need to add a public method to Oxygen script like AddOxygen(float amount)
            DebugManager.Log($"PlayerConsumptionManager: Would restore {currentConsumable.oxygenRestore} oxygen");
        }
        
        // Apply stamina restoration
        if (currentConsumable.staminaRestore > 0)
        {
            // You'll need a stamina system for this
            DebugManager.Log($"PlayerConsumptionManager: Would restore {currentConsumable.staminaRestore} stamina");
        }
        
        // Reset state
        isConsuming = false;
        lastConsumeTime = Time.time;
        
        // Optional: Reset player movement speed
        if (playerController != null)
        {
            // playerController.ResetMovementMultiplier();
        }
        
        DebugManager.Log($"PlayerConsumptionManager: Finished consuming {currentConsumable.itemName}");
        currentConsumable = null;
        consumeTimer = 0f;
    }
    
    /// <summary>
    /// Cancel current consumption
    /// </summary>
    public void CancelConsumption()
    {
        if (isConsuming)
        {
            DebugManager.Log($"PlayerConsumptionManager: Cancelled consumption of {currentConsumable?.itemName}");
            isConsuming = false;
            currentConsumable = null;
            consumeTimer = 0f;
        }
    }
    
    /// <summary>
    /// Check if player is currently consuming something
    /// </summary>
    public bool IsConsuming => isConsuming;
    
    /// <summary>
    /// Get the current consumption progress (0-1)
    /// </summary>
    public float ConsumptionProgress => isConsuming ? Mathf.Clamp01(consumeTimer / currentConsumable.consumeTime) : 0f;
}