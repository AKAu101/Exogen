using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Manages equipped items in the player's hands (slots 17 and 18).
///     Spawns item prefabs in front of the camera and makes them follow mouse look.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Hand Slot Configuration")]
    [SerializeField] private int leftHandSlot = 17;
    [SerializeField] private int rightHandSlot = 18;

    [Header("Item Positioning")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 leftHandOffset = new Vector3(-0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 rightHandOffset = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private float itemScale = 0.5f;

    [Header("Mouse Follow Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxRotationX = 45f;
    [SerializeField] private float maxRotationY = 45f;
    [SerializeField] private float rotationSmoothness = 10f;

    // Runtime state
    private IInventorySystem inventorySystem;
    private IInventoryData playerInventory;
    private GameObject leftHandItem;
    private GameObject rightHandItem;
    private Vector2 lookInput;
    private Vector2 currentRotation;
    private IUIStateManagement uiStateManagement;

    private void Start()
    {
        DebugManager.Log("EquipmentManager: Starting initialization");

        // Find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        DebugManager.Log($"EquipmentManager: Camera found: {cameraTransform != null}");

        // Get inventory system from ServiceLocator
        if (ServiceLocator.Instance.IsRegistered<IInventorySystem>())
        {
            inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();

            // Get player inventory reference
            playerInventory = InventorySystem.Instance.PlayerInventory;

            DebugManager.Log($"EquipmentManager: Inventory system found. PlayerInventory: {playerInventory != null}");
            DebugManager.Log($"EquipmentManager: Hand slots configured - Left: {leftHandSlot}, Right: {rightHandSlot}");

            // Subscribe to inventory events
            inventorySystem.OnItemAdded += HandleItemAdded;
            inventorySystem.OnItemRemoved += HandleItemRemoved;
            inventorySystem.OnItemMoved += HandleItemMoved;
            inventorySystem.OnItemSwapped += HandleItemSwapped;

            DebugManager.Log("EquipmentManager: Subscribed to inventory events");
        }
        else
        {
            DebugManager.LogError("EquipmentManager: IInventorySystem not found in ServiceLocator!");
        }

        // Get UI state management
        if (ServiceLocator.Instance.IsRegistered<IUIStateManagement>())
        {
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();
        }

        DebugManager.Log("EquipmentManager: Initialization complete");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= HandleItemAdded;
            inventorySystem.OnItemRemoved -= HandleItemRemoved;
            inventorySystem.OnItemMoved -= HandleItemMoved;
            inventorySystem.OnItemSwapped -= HandleItemSwapped;
        }
    }

    private void Update()
    {
        UpdateItemPositions();
        UpdateItemRotations();
    }

    private void HandleItemAdded(IInventoryData inv, ItemData itemData, int slot)
    {
        DebugManager.Log($"EquipmentManager: HandleItemAdded called - Slot: {slot}, Item: {itemData.name}, Is PlayerInv: {inv == playerInventory}");

        // Only handle player inventory
        if (inv != playerInventory)
        {
            DebugManager.Log($"EquipmentManager: Not player inventory, ignoring");
            return;
        }

        if (slot == leftHandSlot)
        {
            DebugManager.Log($"EquipmentManager: Equipping to LEFT hand (slot {leftHandSlot})");
            EquipItem(itemData, ref leftHandItem, leftHandOffset);
        }
        else if (slot == rightHandSlot)
        {
            DebugManager.Log($"EquipmentManager: Equipping to RIGHT hand (slot {rightHandSlot})");
            EquipItem(itemData, ref rightHandItem, rightHandOffset);
        }
        else
        {
            DebugManager.Log($"EquipmentManager: Slot {slot} is not a hand slot");
        }
    }

    private void HandleItemRemoved(IInventoryData inv, ItemData itemData, int slot)
    {
        // Only handle player inventory
        if (inv != playerInventory) return;

        if (slot == leftHandSlot)
        {
            UnequipItem(ref leftHandItem);
        }
        else if (slot == rightHandSlot)
        {
            UnequipItem(ref rightHandItem);
        }
    }

    private void HandleItemMoved(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        DebugManager.Log($"EquipmentManager: HandleItemMoved - From slot {sourceSlot} to slot {targetSlot}");

        // Only handle player inventory
        if (invOne != playerInventory && invTwo != playerInventory)
        {
            DebugManager.Log("EquipmentManager: Not player inventory in HandleItemMoved");
            return;
        }

        // Check if item was moved TO a hand slot
        if (invTwo == playerInventory)
        {
            if (targetSlot == leftHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved TO left hand slot");
                RefreshHandSlot(leftHandSlot, ref leftHandItem, leftHandOffset);
            }
            else if (targetSlot == rightHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved TO right hand slot");
                RefreshHandSlot(rightHandSlot, ref rightHandItem, rightHandOffset);
            }
        }

        // Check if item was moved FROM a hand slot
        if (invOne == playerInventory)
        {
            if (sourceSlot == leftHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved FROM left hand slot");
                UnequipItem(ref leftHandItem);
            }
            else if (sourceSlot == rightHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved FROM right hand slot");
                UnequipItem(ref rightHandItem);
            }
        }
    }

    private void HandleItemSwapped(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        // Only handle player inventory
        if (invOne != playerInventory && invTwo != playerInventory) return;

        // Check if swap involves hand slots
        if (sourceSlot == leftHandSlot || targetSlot == leftHandSlot)
        {
            RefreshHandSlot(leftHandSlot, ref leftHandItem, leftHandOffset);
        }
        if (sourceSlot == rightHandSlot || targetSlot == rightHandSlot)
        {
            RefreshHandSlot(rightHandSlot, ref rightHandItem, rightHandOffset);
        }
    }

    private void EquipItem(ItemData itemData, ref GameObject handItem, Vector3 offset)
    {
        DebugManager.Log($"EquipmentManager: EquipItem called for {itemData.name}");

        // Clear existing item first
        UnequipItem(ref handItem);

        // Check if item has a prefab
        if (itemData.itemPrefab == null)
        {
            DebugManager.LogWarning($"EquipmentManager: Cannot equip {itemData.name}: no itemPrefab assigned!");
            return;
        }

        DebugManager.Log($"EquipmentManager: Instantiating prefab {itemData.itemPrefab.name}");

        // Spawn the item prefab
        handItem = Instantiate(itemData.itemPrefab);

        if (handItem == null)
        {
            DebugManager.LogError($"EquipmentManager: Failed to instantiate {itemData.itemPrefab.name}!");
            return;
        }

        DebugManager.Log($"EquipmentManager: Item instantiated successfully at position {handItem.transform.position}");

        // Disable any pickup-related components
        var pickupComponent = handItem.GetComponent<PickupItem>();
        if (pickupComponent != null)
        {
            Destroy(pickupComponent);
        }

        // Disable physics
        var rb = handItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var collider = handItem.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Apply item scale
        handItem.transform.localScale = Vector3.one * itemScale;

        DebugManager.Log($"EquipmentManager: Equipped {itemData.name} in hand with scale {itemScale}");
    }

    private void UnequipItem(ref GameObject handItem)
    {
        if (handItem != null)
        {
            Destroy(handItem);
            handItem = null;
        }
    }

    private void RefreshHandSlot(int slot, ref GameObject handItem, Vector3 offset)
    {
        // Clear current item
        UnequipItem(ref handItem);

        // Check if slot has an item now
        if (playerInventory.SlotToStack.TryGetValue(slot, out var stack))
        {
            EquipItem(stack.ItemType, ref handItem, offset);
        }
    }

    private void UpdateItemPositions()
    {
        if (cameraTransform == null) return;

        // Position left hand item
        if (leftHandItem != null)
        {
            Vector3 worldOffset = cameraTransform.TransformDirection(leftHandOffset);
            leftHandItem.transform.position = cameraTransform.position + worldOffset;
        }

        // Position right hand item
        if (rightHandItem != null)
        {
            Vector3 worldOffset = cameraTransform.TransformDirection(rightHandOffset);
            rightHandItem.transform.position = cameraTransform.position + worldOffset;
        }
    }

    private void UpdateItemRotations()
    {
        // Don't update rotations if inventory is open
        if (uiStateManagement != null && uiStateManagement.IsInventoryVisible) return;

        // Calculate target rotation based on mouse input
        float targetX = lookInput.y * mouseSensitivity;
        float targetY = -lookInput.x * mouseSensitivity;

        // Clamp the rotation
        targetX = Mathf.Clamp(targetX, -maxRotationX, maxRotationX);
        targetY = Mathf.Clamp(targetY, -maxRotationY, maxRotationY);

        // Smoothly interpolate to target rotation
        currentRotation.x = Mathf.Lerp(currentRotation.x, targetX, Time.deltaTime * rotationSmoothness);
        currentRotation.y = Mathf.Lerp(currentRotation.y, targetY, Time.deltaTime * rotationSmoothness);

        // Apply rotation relative to camera
        if (leftHandItem != null)
        {
            leftHandItem.transform.rotation = cameraTransform.rotation * Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
        }

        if (rightHandItem != null)
        {
            rightHandItem.transform.rotation = cameraTransform.rotation * Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
        }
    }

    // Input System callback - should be hooked up to the Look action
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}
