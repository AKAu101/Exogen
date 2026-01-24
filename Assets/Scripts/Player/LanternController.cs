using UnityEngine;

/// <summary>
///     Controls the lantern light based on whether a lantern is equipped in hand slots.
///     Gets information from EquipmentManager instead of checking inventory directly.
/// </summary>
public class LanternController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private Light lanternLight;
    
    [Header("Timer Settings")]
    [SerializeField] private float maxLanternTime = 30f;
    [SerializeField] private bool startWithCharge = false;
    
    [Header("Light Positions")]
    [SerializeField] private Vector3 leftHandPosition = new Vector3(-0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 leftHandRotation = Vector3.zero;
    [SerializeField] private Vector3 rightHandPosition = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 rightHandRotation = Vector3.zero;

    private float currentLanternTime;
    private bool isLeftHand;
    private bool hasLanternEquipped;

    // Public accessor for other systems
    public bool IsLightOn => lanternLight != null && lanternLight.enabled;
    public bool HasLanternEquipped => hasLanternEquipped;

    // Properties
    public float CurrentLanternTime => currentLanternTime;
    public float MaxLanternTime => maxLanternTime;
    public bool IsLanternLit => hasLanternEquipped && currentLanternTime > 0f;

    private void Start()
    {
        DebugManager.Log("LanternController: Starting initialization");

        // Initialize timer
        currentLanternTime = startWithCharge ? maxLanternTime : 0f;

        // Find EquipmentManager if not assigned
        if (equipmentManager == null)
        {
            equipmentManager = FindObjectOfType<EquipmentManager>();
            if (equipmentManager == null)
            {
                DebugManager.LogError("LanternController: EquipmentManager not found!");
                return;
            }
        }

        // Ensure light is parented to camera
        if (lanternLight != null && equipmentManager.CameraTransform != null)
        {
            lanternLight.transform.SetParent(equipmentManager.CameraTransform);
            lanternLight.transform.localPosition = Vector3.zero;
            lanternLight.transform.localRotation = Quaternion.identity;
        }

        // Subscribe to EquipmentManager events
        if (equipmentManager != null)
        {
            // We'll create events in EquipmentManager (see below)
            // For now, we'll update periodically
        }

        // Ensure lantern light is off at start
        if (lanternLight != null)
        {
            lanternLight.enabled = false;
            DebugManager.Log("LanternController: Lantern light initialized and disabled");
        }

        DebugManager.Log("LanternController: Initialization complete");
    }

    private void Update()
    {
        // Update equipped state from EquipmentManager
        UpdateEquippedState();
        
        // Countdown the timer if lantern is equipped and has charge
        if (hasLanternEquipped && currentLanternTime > 0f)
        {
            currentLanternTime -= Time.deltaTime;
            if (currentLanternTime <= 0f)
            {
                currentLanternTime = 0f;
                DebugManager.Log("LanternController: Lantern timer expired");
                UpdateLanternLight();
            }
        }

        // Update light position based on which hand has the lantern
        if (lanternLight != null && lanternLight.enabled)
        {
            if (isLeftHand)
            {
                lanternLight.transform.localPosition = leftHandPosition;
                lanternLight.transform.localRotation = Quaternion.Euler(leftHandRotation);
            }
            else
            {
                lanternLight.transform.localPosition = rightHandPosition;
                lanternLight.transform.localRotation = Quaternion.Euler(rightHandRotation);
            }
        }
    }

    private void UpdateEquippedState()
    {
        if (equipmentManager == null) return;
        
        // Check which hand has the lantern
        bool leftHasLantern = equipmentManager.LeftHandItem != null && 
                             IsLanternItem(equipmentManager.LeftHandItem);
        bool rightHasLantern = equipmentManager.RightHandItem != null && 
                              IsLanternItem(equipmentManager.RightHandItem);
        
        bool wasEquipped = hasLanternEquipped;
        hasLanternEquipped = leftHasLantern || rightHasLantern;
        
        // Determine which hand
        if (hasLanternEquipped)
        {
            isLeftHand = leftHasLantern; // Prefer left hand if both have lanterns
        }
        
        // Only update light if state changed
        if (wasEquipped != hasLanternEquipped)
        {
            DebugManager.Log($"LanternController: Equipped state changed to {hasLanternEquipped} (left: {leftHasLantern}, right: {rightHasLantern})");
            UpdateLanternLight();
        }
    }

    private bool IsLanternItem(ItemData itemData)
    {
        if (itemData == null) return false;
        
        // Use the same logic as EquipmentManager
        if (itemData.name.ToLower().Contains("lantern"))
            return true;
            
        if (itemData.animationType == ItemData.HandItemAnimation.Lantern)
            return true;
            
        return false;
    }

    private void UpdateLanternLight()
    {
        if (lanternLight == null) return;

        // Enable light only if we have a lantern equipped AND the timer has charge
        bool shouldLightBeOn = hasLanternEquipped && currentLanternTime > 0f;
        lanternLight.enabled = shouldLightBeOn;

        if (hasLanternEquipped)
        {
            if (shouldLightBeOn)
            {
                DebugManager.Log($"LanternController: Lantern equipped in {(isLeftHand ? "LEFT" : "RIGHT")} hand, light ENABLED (Time: {currentLanternTime:F1}s)");
            }
            else
            {
                DebugManager.Log($"LanternController: Lantern equipped but no charge remaining, light DISABLED");
            }
        }
        else
        {
            DebugManager.Log("LanternController: No lantern equipped, light DISABLED");
        }
    }

    /// <summary>
    /// Recharges the lantern by adding time to the timer.
    /// Can be called by external scripts (like LuminiPickup).
    /// </summary>
    public void RechargeLantern(float time)
    {
        // Only recharge if we have a lantern equipped
        if (!hasLanternEquipped)
        {
            DebugManager.LogWarning("LanternController: Cannot recharge - no lantern equipped!");
            return;
        }
        
        currentLanternTime = Mathf.Min(currentLanternTime + time, maxLanternTime);
        DebugManager.Log($"LanternController: Lantern recharged! Current time: {currentLanternTime:F1}s");

        // Update the light state
        UpdateLanternLight();
    }
    
    // Helper method for LuminiPickup to check if it should recharge
    public bool CanRecharge()
    {
        return hasLanternEquipped && currentLanternTime < maxLanternTime;
    }
}