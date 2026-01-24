using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Manages equipped items in the player's hands (slots 17 and 18).
///     Spawns item prefabs in front of the camera and makes them follow mouse look.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Hand Slot Configuration")] 
    [SerializeField] private int leftHandSlot = 16;
    [SerializeField] private int rightHandSlot = 17;
    
    private ItemData leftHandItemData;
    private ItemData rightHandItemData;

    [Header("Item Positioning")] [SerializeField]
    private Transform cameraTransform;

    [SerializeField] private Transform socketLeft; // Socket_L - left hand bone socket
    [SerializeField] private Transform socketRight; // Socket_R - right hand bone socket
    [SerializeField] private Vector3 leftHandRotation = new(0f, 0f, -75f);
    [SerializeField] private Vector3 rightHandRotation = new(0f, 0f, 75f);
    [SerializeField] private float itemScale = 1f;

    [Header("Lantern Specific Rotation")] [SerializeField]
    private Vector3 lanternLeftHandRotation = new(0f, 0f, -75f);

    [SerializeField] private Vector3 lanternRightHandRotation = new(0f, 0f, 75f);

    [Header("Mouse Follow Settings")] [SerializeField]
    private float mouseSensitivity = 80f;

    [SerializeField] private float maxRotationX = 35f;
    [SerializeField] private float maxRotationY = 35f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float maxRotationSpeed = 200f;

    [Header("Animation Coordination")] [SerializeField]
    private HandAnimationManager handAnimationManager;

    private Vector2 currentRotation;

    // Runtime state
    private IInventorySystem inventorySystem;
    private GameObject leftHandItem;
    private Vector2 lookInput;
    private IInventoryData playerInventory;
    private GameObject rightHandItem;
    private Vector2 rotationVelocity;
    private IUIStateManagement uiStateManagement;
    
    //Getters
    public ItemData LeftHandItem => leftHandItemData;
    public ItemData RightHandItem => rightHandItemData;
    public bool IsLanternEquipped => (leftHandItemData != null && IsLantern(leftHandItemData)) || 
                                     (rightHandItemData != null && IsLantern(rightHandItemData));
    public bool IsScannerEquipped => (leftHandItemData != null && IsScanner(leftHandItemData)) || 
                                     (rightHandItemData != null && IsScanner(rightHandItemData));
    public Transform CameraTransform => cameraTransform;
    
    private void Start()
    {
        DebugManager.Log("EquipmentManager: Starting initialization");

        // Find camera if not assigned
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

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
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();

        DebugManager.Log("EquipmentManager: Initialization complete");
    }

    private void Update()
    {
        // Items are parented to sockets, so they follow automatically
        // Only update rotation jiggle effect
        UpdateItemRotations();
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

    private void HandleItemAdded(IInventoryData inv, ItemData itemData, int slot)
    {
        DebugManager.Log(
            $"EquipmentManager: HandleItemAdded called - Slot: {slot}, Item: {itemData.name}, Is PlayerInv: {inv == playerInventory}");

        // Only handle player inventory
        if (inv != playerInventory)
        {
            DebugManager.Log("EquipmentManager: Not player inventory, ignoring");
            return;
        }

        if (slot == leftHandSlot)
        {
            DebugManager.Log($"EquipmentManager: Equipping to LEFT hand (slot {leftHandSlot})");
            EquipItem(itemData, ref leftHandItem, socketLeft,
                IsLantern(itemData) ? lanternLeftHandRotation : leftHandRotation);
        }
        else if (slot == rightHandSlot)
        {
            DebugManager.Log($"EquipmentManager: Equipping to RIGHT hand (slot {rightHandSlot})");
            EquipItem(itemData, ref rightHandItem, socketRight,
                IsLantern(itemData) ? lanternRightHandRotation : rightHandRotation);
        }
        else
        {
            DebugManager.Log($"EquipmentManager: Slot {slot} is not a hand slot");
        }
    }

    private void HandleItemRemoved(IInventoryData inv, ItemData itemData, int slot)
    {
        if (inv != playerInventory) return;

        if (slot == leftHandSlot)
        {
            UnequipItem(ref leftHandItem, socketLeft);
        }
        else if (slot == rightHandSlot)
        {
            UnequipItem(ref rightHandItem, socketRight);
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

        // Check if item was moved FROM a hand slot
        if (invOne == playerInventory)
        {
            if (sourceSlot == leftHandSlot)
            {
                //Debug.Log($"EquipmentManager: Item moved FROM left hand slot");
                UnequipItem(ref leftHandItem, socketLeft);
            }
            else if (sourceSlot == rightHandSlot)
            {
                //Debug.Log($"EquipmentManager: Item moved FROM right hand slot");
                UnequipItem(ref rightHandItem, socketRight);
            }
        }

        // Check if item was moved TO a hand slot
        if (invTwo == playerInventory)
        {
            if (targetSlot == leftHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved TO left hand slot");
                RefreshHandSlot(leftHandSlot, ref leftHandItem, socketLeft, true);
            }
            else if (targetSlot == rightHandSlot)
            {
                DebugManager.Log($"EquipmentManager: Item moved TO right hand slot");
                RefreshHandSlot(rightHandSlot, ref rightHandItem, socketRight, false);
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
            RefreshHandSlot(leftHandSlot, ref leftHandItem, socketLeft, true);
        }
        if (sourceSlot == rightHandSlot || targetSlot == rightHandSlot)
        {
            RefreshHandSlot(rightHandSlot, ref rightHandItem, socketRight, false);
        }
    }

    private void EquipItem(ItemData itemData, ref GameObject handItem, Transform socket, Vector3 rotation)
    {
        DebugManager.Log($"EquipmentManager: EquipItem called for {itemData?.name ?? "NULL"}");

        // Clear existing item first
        UnequipItem(ref handItem, socket);

        // Check if item has a prefab
        if (itemData.itemPrefab == null)
        {
            DebugManager.LogWarning($"EquipmentManager: Cannot equip {itemData.name}: no itemPrefab assigned!");
            return;
        }

        // Check if socket is assigned
        if (socket == null)
        {
            DebugManager.LogWarning($"EquipmentManager: Cannot equip {itemData.name}: socket not assigned!");
            return;
        }
        if (socket == socketLeft)
        {
            leftHandItemData = itemData;
        }
        else if (socket == socketRight)
        {
            rightHandItemData = itemData;
        }

        DebugManager.Log($"EquipmentManager: Instantiating prefab {itemData.itemPrefab.name}");

        // Spawn the item prefab as child of socket
        handItem = Instantiate(itemData.itemPrefab, socket);

        if (handItem == null)
        {
            DebugManager.LogError($"EquipmentManager: Failed to instantiate {itemData.itemPrefab.name}!");
            return;
        }

        // Reset local position and rotation to match socket
        handItem.transform.localPosition = Vector3.zero;
        handItem.transform.localRotation = Quaternion.Euler(rotation);

        DebugManager.Log($"EquipmentManager: Item instantiated at socket {socket.name} with rotation {rotation}");

        // Disable any pickup-related components
        var pickupComponent = handItem.GetComponent<PickupItem>();
        if (pickupComponent != null) Destroy(pickupComponent);

        // Disable physics
        var rb = handItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var collider = handItem.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // Apply item scale
        handItem.transform.localScale = Vector3.one * itemScale;

        // UPDATE ANIMATION STATE
        if (handAnimationManager != null)
        {
            // Determine which slot this is based on which socket
            int currentSlot = (socket == socketLeft) ? leftHandSlot : rightHandSlot;
            handAnimationManager.UpdateHandAnimation(currentSlot, itemData);
        }

        DebugManager.Log($"EquipmentManager: Equipped {itemData.name} in hand with scale {itemScale}");
    }

    private void UnequipItem(ref GameObject handItem, Transform socket = null)
    {
        if (handItem != null)
        {
            Destroy(handItem);
            handItem = null;
        }
        
        // Clear the item data tracking
        if (socket == socketLeft)
        {
            leftHandItemData = null;
        }
        else if (socket == socketRight)
        {
            rightHandItemData = null;
        }

        // UPDATE ANIMATION STATE
        if (handAnimationManager != null && socket != null)
        {
            int currentSlot = (socket == socketLeft) ? leftHandSlot : rightHandSlot;
            handAnimationManager.UpdateHandAnimation(currentSlot, null);
        }
    }

    // Overload for backward compatibility
    private void UnequipItem(ref GameObject handItem)
    {
        UnequipItem(ref handItem, null);
    }

    private void RefreshHandSlot(int slot, ref GameObject handItem, Transform socket, bool isLeftHand)
    {
        // Clear current item
        UnequipItem(ref handItem, socket);

        // Check if slot has an item now
        if (playerInventory.SlotToStack.TryGetValue(slot, out var stack))
        {
            // Determine the correct rotation based on item type
            Vector3 rotation;
            if (IsLantern(stack.ItemType))
            {
                rotation = isLeftHand ? lanternLeftHandRotation : lanternRightHandRotation;
            }
            else
            {
                rotation = isLeftHand ? leftHandRotation : rightHandRotation;
            }
        
            EquipItem(stack.ItemType, ref handItem, socket, rotation);
        }
    }

    private bool IsLantern(ItemData itemData)
    {
        // Check if this item is a lantern
        if (itemData == null) return false;

        // Option 1: Check by name (simple)
        if (itemData.name.ToLower().Contains("lantern"))
            return true;

        // Option 2: Check if it has the Lantern animation type (new system)
        if (itemData.animationType == ItemData.HandItemAnimation.Lantern)
            return true;

        return false;
    }
    
    private bool IsScanner(ItemData itemData)
    {
        if (itemData == null) return false;
    
        // Check by name
        if (itemData.name.ToLower().Contains("scanner") || itemData.name.ToLower().Contains("radar"))
            return true;
    
        // Check animation type
        if (itemData.animationType == ItemData.HandItemAnimation.Radar)
            return true;
        
        return false;
    }

    private void UpdateItemRotations()
    {
        // Don't update rotations if inventory is open
        if (uiStateManagement != null && uiStateManagement.IsInventoryVisible) return;

        // Convert mouse delta to velocity (frame-rate independent)
        var mouseVelocityY = Time.deltaTime > 0 ? lookInput.y / Time.deltaTime : 0;
        var mouseVelocityX = Time.deltaTime > 0 ? lookInput.x / Time.deltaTime : 0;

        // Calculate target rotation based on mouse velocity (scaled down since velocity is large)
        var targetX = mouseVelocityY * mouseSensitivity * 0.01f;
        var targetY = -mouseVelocityX * mouseSensitivity * 0.01f;

        // Clamp the target rotation
        targetX = Mathf.Clamp(targetX, -maxRotationX, maxRotationX);
        targetY = Mathf.Clamp(targetY, -maxRotationY, maxRotationY);

        // Smoothly interpolate current rotation to target using SmoothDamp (frame-rate independent)
        currentRotation.x = Mathf.SmoothDamp(currentRotation.x, targetX, ref rotationVelocity.x, rotationSmoothTime,
            maxRotationSpeed);
        currentRotation.y = Mathf.SmoothDamp(currentRotation.y, targetY, ref rotationVelocity.y, rotationSmoothTime,
            maxRotationSpeed);

        // Apply jiggle rotation as local rotation offset (preserve base hand rotation)
        if (leftHandItem != null)
        {
            // Determine base rotation based on item type
            Vector3 baseRotation;
            if (playerInventory.SlotToStack.TryGetValue(leftHandSlot, out var leftStack) &&
                IsLantern(leftStack.ItemType))
                baseRotation = lanternLeftHandRotation;
            else
                baseRotation = leftHandRotation;

            var baseRot = Quaternion.Euler(baseRotation);
            var jiggle = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
            leftHandItem.transform.localRotation = baseRot * jiggle;
        }

        if (rightHandItem != null)
        {
            // Determine base rotation based on item type
            Vector3 baseRotation;
            if (playerInventory.SlotToStack.TryGetValue(rightHandSlot, out var rightStack) &&
                IsLantern(rightStack.ItemType))
                baseRotation = lanternRightHandRotation;
            else
                baseRotation = rightHandRotation;

            var baseRot = Quaternion.Euler(baseRotation);
            var jiggle = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
            rightHandItem.transform.localRotation = baseRot * jiggle;
        }
    }

    // Input System callback - should be hooked up to the Look action
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}