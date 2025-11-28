using UnityEngine;

/// <summary>
///     Controls the lantern light based on whether a lantern is equipped in hand slots.
///     Automatically enables/disables the light when lantern is equipped/unequipped.
/// </summary>
public class LanternController : MonoBehaviour
{
    [Header("Lantern Configuration")]
    [SerializeField] private Light lanternLight; // Light component
    [SerializeField] private Transform cameraTransform; // Camera transform reference
    [SerializeField] private string lanternItemName = "Lantern"; // Name of the lantern item
    [SerializeField] private int leftHandSlot = 17;
    [SerializeField] private int rightHandSlot = 18;

    [Header("Timer Settings")]
    [SerializeField] private float maxLanternTime = 30f; // Maximum time the lantern can be lit
    [SerializeField] private bool startWithCharge = false; // Whether the lantern starts with charge

    [Header("Light Positions")]
    [SerializeField] private Vector3 leftHandPosition = new Vector3(-0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 leftHandRotation = Vector3.zero;
    [SerializeField] private Vector3 rightHandPosition = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 rightHandRotation = Vector3.zero;

    private IInventorySystem inventorySystem;
    private IInventoryData playerInventory;
    private bool isLeftHand; // Track which hand has the lantern
    private float currentLanternTime; // Current remaining time for the lantern
    private bool hasLanternEquipped; // Whether a lantern is currently equipped

    // Public accessor for other systems (like enemy AI)
    public bool IsLightOn => lanternLight != null && lanternLight.enabled;

    // Properties for external access
    public float CurrentLanternTime => currentLanternTime;
    public float MaxLanternTime => maxLanternTime;
    public bool IsLanternLit => hasLanternEquipped && currentLanternTime > 0f;

    private void Start()
    {
        DebugManager.Log("LanternController: Starting initialization");

        // Initialize timer
        currentLanternTime = startWithCharge ? maxLanternTime : 0f;

        // Find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Ensure light is parented to camera
        if (lanternLight != null && cameraTransform != null)
        {
            lanternLight.transform.SetParent(cameraTransform);
        }

        // Get inventory system from ServiceLocator
        if (ServiceLocator.Instance.IsRegistered<IInventorySystem>())
        {
            inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
            playerInventory = InventorySystem.Instance.PlayerInventory;

            DebugManager.Log($"LanternController: Inventory system found");

            // Subscribe to inventory events
            inventorySystem.OnItemAdded += HandleInventoryChanged;
            inventorySystem.OnItemRemoved += HandleInventoryChanged;
            inventorySystem.OnItemMoved += HandleItemMoved;
            inventorySystem.OnItemSwapped += HandleItemSwapped;

            DebugManager.Log("LanternController: Subscribed to inventory events");
        }
        else
        {
            DebugManager.LogError("LanternController: IInventorySystem not found in ServiceLocator!");
        }

        // Ensure lantern light is off at start
        if (lanternLight != null)
        {
            lanternLight.enabled = false;
            DebugManager.Log("LanternController: Lantern light initialized and disabled");
        }
        else
        {
            DebugManager.LogWarning("LanternController: Lantern light not assigned!");
        }

        DebugManager.Log("LanternController: Initialization complete");
    }

    private void Update()
    {
        // Countdown the timer if lantern is equipped and has charge
        if (hasLanternEquipped && currentLanternTime > 0f)
        {
            currentLanternTime -= Time.deltaTime;
            if (currentLanternTime <= 0f)
            {
                currentLanternTime = 0f;
                DebugManager.Log("LanternController: Lantern timer expired");
                UpdateLanternLight(); // Update to turn off the light
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

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= HandleInventoryChanged;
            inventorySystem.OnItemRemoved -= HandleInventoryChanged;
            inventorySystem.OnItemMoved -= HandleItemMoved;
            inventorySystem.OnItemSwapped -= HandleItemSwapped;
        }
    }

    private void HandleInventoryChanged(IInventoryData inv, ItemData itemData, int slot)
    {
        // Only handle player inventory and hand slots
        if (inv != playerInventory) return;
        if (slot != leftHandSlot && slot != rightHandSlot) return;

        DebugManager.Log($"LanternController: Inventory changed in hand slot {slot}");
        UpdateLanternLight();
    }

    private void HandleItemMoved(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        // Check if move involves player inventory and hand slots
        bool involvesHands = false;

        if (invOne == playerInventory && (sourceSlot == leftHandSlot || sourceSlot == rightHandSlot))
        {
            involvesHands = true;
        }

        if (invTwo == playerInventory && (targetSlot == leftHandSlot || targetSlot == rightHandSlot))
        {
            involvesHands = true;
        }

        if (involvesHands)
        {
            DebugManager.Log($"LanternController: Item moved involving hand slots");
            UpdateLanternLight();
        }
    }

    private void HandleItemSwapped(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        // Check if swap involves player inventory and hand slots
        bool involvesHands = false;

        if (invOne == playerInventory && (sourceSlot == leftHandSlot || sourceSlot == rightHandSlot))
        {
            involvesHands = true;
        }

        if (invTwo == playerInventory && (targetSlot == leftHandSlot || targetSlot == rightHandSlot))
        {
            involvesHands = true;
        }

        if (involvesHands)
        {
            DebugManager.Log($"LanternController: Item swapped involving hand slots");
            UpdateLanternLight();
        }
    }

    private void UpdateLanternLight()
    {
        if (lanternLight == null || playerInventory == null) return;

        // Check if either hand has a lantern
        hasLanternEquipped = false;

        // Check left hand first
        if (playerInventory.SlotToStack.TryGetValue(leftHandSlot, out var leftStack))
        {
            if (leftStack.ItemType.name.Equals(lanternItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                hasLanternEquipped = true;
                isLeftHand = true;
                DebugManager.Log($"LanternController: Lantern found in LEFT hand");
            }
        }

        // Check right hand (only if not already found in left)
        if (!hasLanternEquipped && playerInventory.SlotToStack.TryGetValue(rightHandSlot, out var rightStack))
        {
            if (rightStack.ItemType.name.Equals(lanternItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                hasLanternEquipped = true;
                isLeftHand = false;
                DebugManager.Log($"LanternController: Lantern found in RIGHT hand");
            }
        }

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
    /// <param name="time">Amount of time to add to the lantern timer</param>
    public void RechargeLantern(float time)
    {
        currentLanternTime = Mathf.Min(currentLanternTime + time, maxLanternTime);
        DebugManager.Log($"LanternController: Lantern recharged! Current time: {currentLanternTime:F1}s");

        // Update the light state
        UpdateLanternLight();
    }
}
