using Generals;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public struct InventoryUISyncPackage
{
    public InventoryUI inventoryUI;
    public bool isOpen;
}


[System.Serializable]
public class InventoryUISyncEntry
{
    public InventoryUI inventory;
    [SerializeField] bool open;
    [SerializeField] bool close;
    public bool Open => open;
    public bool Close => close;
}

/// <summary>
/// Manages the inventory UI, including item display, slot management, and visibility toggling.
/// Listens to inventory events and updates the visual representation accordingly.
/// Includes static registry for multi-inventory support (integrated from InventoryUIHelper).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // ==================== STATIC REGISTRY (from InventoryUIHelper) ====================
    private static Dictionary<IInventoryData, InventoryUI> inventoryRegistry = new();

    public static void RegisterInventory(IInventoryData inv, InventoryUI ui)
    {
        if (inventoryRegistry.ContainsKey(inv))
        {
            DebugManager.LogWarning($"Inventory {inv} already registered, overwriting.");
        }
        inventoryRegistry[inv] = ui;
        DebugManager.Log($"Registered {inv} - {ui}");
    }

    public static InventoryUI GetUI(IInventoryData inv)
    {
        if (inventoryRegistry.TryGetValue(inv, out var ui))
        {
            return ui;
        }
        DebugManager.LogError($"No UI registered for inventory {inv}");
        return null;
    }

    public static bool SwapViewsBetweenInventories(InventoryUI invOne, int sourceSlot, InventoryUI invTwo, int targetSlot)
    {
        if (!invOne.slotToView.ContainsKey(sourceSlot) || !invTwo.slotToView.ContainsKey(targetSlot))
        {
            DebugManager.LogError($"SwapViews: Source slot {sourceSlot} or target slot {targetSlot} not found!");
            return false;
        }

        invOne.slotToView[sourceSlot].SetReferencedSlot(targetSlot);
        invTwo.slotToView[targetSlot].SetReferencedSlot(sourceSlot);

        return invOne.slotToView.SwapEntries(sourceSlot, targetSlot, invTwo.slotToView);
    }

    public static void MoveViewBetweenInventories(InventoryUI sourceUI, int sourceSlot, InventoryUI targetUI, int targetSlot)
    {
        DebugManager.Log($"Moving {sourceSlot} from {sourceUI} to {targetUI} at {targetSlot}");
        DebugManager.Log($"Source SlotView Ref: {sourceUI.slotToView[sourceSlot].CurrentSlotIndex}");

        targetUI.slotToView[targetSlot] = sourceUI.slotToView[sourceSlot];
        sourceUI.slotToView.Remove(sourceSlot);
        targetUI.slotToView[targetSlot].SetReferencedSlot(targetSlot);
        DebugManager.Log($"Target SlotView Ref: {targetUI.slotToView[targetSlot].CurrentSlotIndex}");

        var view = targetUI.slotToView[targetSlot];
        view.transform.SetParent(targetUI.SlotIndexToContainer[view.CurrentSlotIndex].transform);

        sourceUI.UpdateView();
        targetUI.UpdateView();
    }

    // ==================== INSTANCE MEMBERS ====================

    //References
    [SerializeField] private GameObject wrapper;
    [SerializeField] private GameObject slotRoot;
    public Transform SlotRoot => slotRoot.transform;
    [SerializeField] private List<GameObject> slotObjects;

    //Properties
    private readonly Dictionary<int, GameObject> slotIndexToContainer = new();
    public Dictionary<int, GameObject> SlotIndexToContainer => slotIndexToContainer;
    private bool eventsSubscribed;
    private IInventoryData assignedInventory;
    public IInventoryData AssignedInventory => assignedInventory;
    [SerializeField] InventoryData _Inventory;

    //References
    private IInventorySystem inventoryManagement;
    public Dictionary<int, ItemSlot> slotToView = new();
    [SerializeField] List<InventoryUISyncEntry> syncOpenWith;

    protected void Awake()
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            slotIndexToContainer[i] = slotObjects[i];

            // Ensure slot has an Image component for raycasting
            var slotImage = slotObjects[i].GetComponent<Image>();
            if (slotImage == null)
            {
                slotImage = slotObjects[i].AddComponent<Image>();
                slotImage.color = new Color(1, 1, 1, 0.01f); // Nearly transparent but still receives raycasts
            }
            // Make sure the image is a raycast target
            slotImage.raycastTarget = true;

            // Get or add ItemSlot component to each slot object
            var itemSlot = slotObjects[i].GetComponent<ItemSlot>();
            if (itemSlot == null)
            {
                itemSlot = slotObjects[i].AddComponent<ItemSlot>();
            }

            // Initialize the ItemSlot with index and UI reference
            itemSlot.Initialize(i, this);

            // Get or add ItemDragHandler component to each slot object
            var dragHandler = slotObjects[i].GetComponent<ItemDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = slotObjects[i].AddComponent<ItemDragHandler>();
            }

        }

        assignedInventory = _Inventory.GetComponent<IInventoryData>();

        // Register in static registry
        RegisterInventory(assignedInventory, this);
    }

    private void Start()
    {
        // Ensure events are subscribed after all Awake() calls have completed
        TrySubscribeToEvents();
    }

    private void OnEnable()
    {
        TrySubscribeToEvents();

        foreach(var InvUI in syncOpenWith)
        {
            InvUI.inventory.OnInventoryVisibilitySync += OnSyncCalled;
        }
    }

    private void OnDisable()
    {
        if (inventoryManagement != null && eventsSubscribed)
        {
            inventoryManagement.OnItemAdded -= HandleItemAdded;
            inventoryManagement.OnItemRemoved -= HandleItemRemoved;
            inventoryManagement.OnItemMoved -= HandleItemMoved;
            inventoryManagement.OnItemSwapped -= HandleItemSwapped;
            eventsSubscribed = false;
        }

        foreach (var InvUI in syncOpenWith)
        {
            InvUI.inventory.OnInventoryVisibilitySync -= OnSyncCalled; //kann sein dass das beim play mode exit die nullref throwt
        }
    }

    public bool IsInventoryVisible { get; private set; }

    public event Action<bool> OnInventoryVisibilityChanged;
    public event Action<InventoryUISyncPackage> OnInventoryVisibilitySync;

    public void SetInventoryVisible(bool visible)
    {
        if (IsInventoryVisible == visible) return;

        IsInventoryVisible = visible;

        if (IsInventoryVisible)
        {
            // Try to subscribe to events before showing inventory
            TrySubscribeToEvents();

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            wrapper.SetActive(true);

            // Notify centralized UI state manager
            UIStateManager.EnsureInstance().RegisterInventoryOpened(this);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            wrapper.SetActive(false);

            // Hide context menu when closing inventory
            var contextMenu = FindFirstObjectByType<ItemContextMenu>();
            if (contextMenu != null) contextMenu.HideMenu();

            // Notify centralized UI state manager
            UIStateManager.EnsureInstance().RegisterInventoryClosed(this);
        }

        OnInventoryVisibilityChanged?.Invoke(IsInventoryVisible);
        OnInventoryVisibilitySync?.Invoke(new InventoryUISyncPackage{ inventoryUI = this,isOpen = visible});
    }

    public void ToggleInventory()
    {
        SetInventoryVisible(!IsInventoryVisible);
    }

    private void TrySubscribeToEvents()
    {
        // Get the inventory service from the ServiceLocator
        if (inventoryManagement == null && ServiceLocator.Instance.IsRegistered<IInventorySystem>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();

        // Subscribe to events if we have the service and haven't subscribed yet
        if (inventoryManagement != null && !eventsSubscribed)
        {
            inventoryManagement.OnItemAdded += HandleItemAdded;
            inventoryManagement.OnItemRemoved += HandleItemRemoved;
            inventoryManagement.OnItemMoved += HandleItemMoved;
            inventoryManagement.OnItemSwapped += HandleItemSwapped;
            eventsSubscribed = true;

            // Rebuild any items that were already in the inventory before we subscribed
            RebuildInventoryViews();
        }
        // Service not available yet - this is normal during initialization
        // Will retry in Start() or when inventory opens
    }

    private void RebuildInventoryViews()
    {
        if (inventoryManagement == null) return;

        // Don't rebuild if we already have views (avoid double rebuild)
        if (slotToView.Count > 0) return;

        // Recreate views for all items currently in inventory
        foreach (var kvp in assignedInventory.SlotToStack)
        {
            var slot = kvp.Key;
            var stack = kvp.Value;
            HandleItemAdded(assignedInventory, stack.ItemType, slot);
        }
    }

    private void HandleItemAdded(IInventoryData inv, ItemData itemType, int slot)
    {
        if (inventoryManagement == null)
        {
            DebugManager.LogError("HandleItemAdded: inventoryManagement is null!");
            return;
        }

        if (inv != assignedInventory)
        {
            DebugManager.Log($"Not my({this}) Inventory!");
            return;
        }

        // Use existing slot GameObject from slotIndexToContainer
        if (!slotIndexToContainer.ContainsKey(slot))
        {
            DebugManager.LogError($"Slot {slot} not found in slotIndexToContainer! Max slots: {slotIndexToContainer.Count}");
            return;
        }

        var slotObject = slotIndexToContainer[slot];
        var itemSlot = slotObject.GetComponent<ItemSlot>();

        if (itemSlot == null)
        {
            DebugManager.LogError($"ItemSlot component not found on slot {slot}!");
            return;
        }

        var stack = inv.SlotToStack[slot];

        if (!slotToView.ContainsKey(slot))
        {
            // First time adding to this slot
            itemSlot.Setup(itemType, slot, stack.Amount);
            slotToView[slot] = itemSlot;
        }
        else
        {
            // Updating existing slot
            itemSlot.UpdateAmount(stack.Amount);
        }
    }

    private void HandleItemRemoved(IInventoryData inv, ItemData itemType, int slot)
    {
        if (inventoryManagement == null) return;

        if (inv != assignedInventory)
        {
            DebugManager.Log("Not my Inventory!");
            return;
        }

        if (slotToView.ContainsKey(slot))
        {
            var view = slotToView[slot];

            // Check if the slot still has items
            if (inv.SlotToStack.ContainsKey(slot))
            {
                // Update the count
                var stack = inv.SlotToStack[slot];
                view.UpdateAmount(stack.Amount);
            }
            else
            {
                // No more items in this slot, clear the display
                if (view != null)
                {
                    view.ClearDisplay();
                }
                slotToView.Remove(slot);
            }
        }
    }

    private void HandleItemMoved(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        if (invOne != assignedInventory && invTwo != assignedInventory)
        {
            DebugManager.Log($"Not my({this}) Inventory!");
            return;
        }

        DebugManager.Log($"Handling Item Moved! I am {this}");

        // Update source slot (clear it if item was moved away)
        if (invOne == assignedInventory && slotToView.ContainsKey(sourceSlot))
        {
            if (!invOne.SlotToStack.ContainsKey(sourceSlot))
            {
                // Item moved away, clear the source slot
                slotToView[sourceSlot].ClearDisplay();
                slotToView.Remove(sourceSlot);
            }
            else
            {
                // Partial move (stack split), update amount
                var stack = invOne.SlotToStack[sourceSlot];
                slotToView[sourceSlot].UpdateAmount(stack.Amount);
            }
        }

        // Update target slot (show the item there)
        if (invTwo == assignedInventory && invTwo.SlotToStack.ContainsKey(targetSlot))
        {
            var stack = invTwo.SlotToStack[targetSlot];
            var targetSlotView = slotIndexToContainer[targetSlot].GetComponent<ItemSlot>();

            if (!slotToView.ContainsKey(targetSlot))
            {
                slotToView[targetSlot] = targetSlotView;
            }

            targetSlotView.Setup(stack.ItemType, targetSlot, stack.Amount);
        }
    }

    private void HandleItemSwapped(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot)
    {
        if (invOne != assignedInventory && invTwo != assignedInventory)
        {
            DebugManager.Log("Not my Inventory!");
            return;
        }

        // Refresh both slots after swap
        if (invOne == assignedInventory)
        {
            RefreshSlot(invOne, sourceSlot);
        }

        if (invTwo == assignedInventory)
        {
            RefreshSlot(invTwo, targetSlot);
        }
    }

    private void RefreshSlot(IInventoryData inv, int slot)
    {
        var slotView = slotIndexToContainer[slot].GetComponent<ItemSlot>();

        if (inv.SlotToStack.ContainsKey(slot))
        {
            var stack = inv.SlotToStack[slot];
            slotView.Setup(stack.ItemType, slot, stack.Amount);

            if (!slotToView.ContainsKey(slot))
            {
                slotToView[slot] = slotView;
            }
        }
        else
        {
            slotView.ClearDisplay();
            slotToView.Remove(slot);
        }
    }


    public void IntegrateView(ItemSlot view)
    {
        if (view == null)
        {
            DebugManager.LogWarning("ItemSlot is null in IntegrateView!");
            return;
        }

        view.transform.SetParent(slotIndexToContainer[view.CurrentSlotIndex].transform);

        // Use indexer instead of Add to handle cases where key might already exist
        if (slotToView.ContainsKey(view.CurrentSlotIndex))
        {
            DebugManager.LogWarning(
                $"IntegrateView: Slot {view.CurrentSlotIndex} already exists in slotToView, replacing it.");
            Destroy(slotToView[view.CurrentSlotIndex].gameObject);
        }

        slotToView[view.CurrentSlotIndex] = view;

        view.transform.position = slotIndexToContainer[view.CurrentSlotIndex].transform.position;
    }

    public ItemSlot GetView(int slot)
    {
        return slotToView[slot];
    }

    public void MoveViewDict(IInventoryData inv, int sourceSlot, IInventoryData targetInv, int targetSlot)
    {
        var targetUI = GetUI(targetInv);
        MoveViewBetweenInventories(this, sourceSlot, targetUI, targetSlot);
    }



    public void UpdateView()
    {
        foreach (var view in slotToView.Values.ToList())
        {
            view.transform.position = slotIndexToContainer[view.CurrentSlotIndex].transform.position;
            view.transform.SetParent(slotIndexToContainer[view.CurrentSlotIndex].transform);
        }
    }

    public void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (context.performed) ToggleInventory();
    }

    void OnSyncCalled(InventoryUISyncPackage pkg)
    {
        var entry = syncOpenWith.Find(e => e.inventory == pkg.inventoryUI);
        if (entry == null)
            return;

        if (entry.Close && !pkg.isOpen)
        {
            SetInventoryVisible(false);
        }
        else if (entry.Open && pkg.isOpen)
        {
            SetInventoryVisible(true);
        }
    }

}
