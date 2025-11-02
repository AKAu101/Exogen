using System;
using System.Collections.Generic;
using System.Linq;
using Generals;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the inventory UI, including item display, slot management, and visibility toggling.
/// Listens to inventory events and updates the visual representation accordingly.
/// </summary>
public class InventoryUIManager : Singleton<InventoryUIManager>, IUIStateManagement
{
    //References
    [SerializeField] private GameObject wrapper;
    [SerializeField] private GameObject itemViewPrefab;
    [SerializeField] private List<GameObject> slotObjects;

    //Properties
    private readonly Dictionary<int, GameObject> slotIndexToContainer = new();
    private bool eventsSubscribed;

    //References
    private IInventoryManagement inventoryManagement;
    public Dictionary<int, InventoryItemUI> slotToView = new();

    protected override void Awake()
    {
        base.Awake();
        for (var i = 0; i < slotObjects.Count; i++) slotIndexToContainer[i] = slotObjects[i];

        // Register this instance as the IUIStateManagement service
        ServiceLocator.Instance.Register<IUIStateManagement>(this);
    }

    private void Start()
    {
        // Ensure events are subscribed after all Awake() calls have completed
        TrySubscribeToEvents();
    }

    private void OnEnable()
    {
        TrySubscribeToEvents();
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
    }

    public bool IsInventoryVisible { get; private set; }

    public event Action<bool> OnInventoryVisibilityChanged;

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
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            wrapper.SetActive(false);

            // Hide context menu when closing inventory
            var contextMenu = FindFirstObjectByType<InventoryItemContextMenu>();
            if (contextMenu != null) contextMenu.HideMenu();
        }

        OnInventoryVisibilityChanged?.Invoke(IsInventoryVisible);
    }

    public void ToggleInventory()
    {
        SetInventoryVisible(!IsInventoryVisible);
    }

    private void TrySubscribeToEvents()
    {
        // Get the inventory service from the ServiceLocator
        if (inventoryManagement == null && ServiceLocator.Instance.IsRegistered<IInventoryManagement>())
            inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();

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
        foreach (var kvp in inventoryManagement.SlotToStack)
        {
            var slot = kvp.Key;
            var stack = kvp.Value;
            HandleItemAdded(stack.ItemType, slot);
        }
    }

    private void HandleItemAdded(ItemSO itemType, int slot)
    {
        if (inventoryManagement == null)
        {
            Debug.LogError("HandleItemAdded: inventoryManagement is null!");
            return;
        }

        if (itemViewPrefab == null)
        {
            Debug.LogError("HandleItemAdded: itemViewPrefab is null! Assign it in the Inspector.");
            return;
        }

        if (!slotToView.ContainsKey(slot))
        {
            var instance = Instantiate(itemViewPrefab);
            var view = instance.GetComponent<InventoryItemUI>();
            if (view != null)
            {
                var stack = inventoryManagement.SlotToStack[slot];
                view.Setup(itemType, slot, stack.Amount);
                IntegrateView(view);
            }
            else
            {
                Debug.LogWarning("InventoryItemUI component not found on itemViewPrefab!");
            }
        }
        else
        {
            // Item already exists in this slot, just update the count
            var view = slotToView[slot];
            var stack = inventoryManagement.SlotToStack[slot];
            view.UpdateAmount(stack.Amount);
        }
    }

    private void HandleItemRemoved(ItemSO itemType, int slot)
    {
        if (inventoryManagement == null) return;

        if (slotToView.ContainsKey(slot))
        {
            // Check if the slot still has items
            if (inventoryManagement.SlotToStack.ContainsKey(slot))
            {
                // Update the count
                var view = slotToView[slot];
                var stack = inventoryManagement.SlotToStack[slot];
                view.UpdateAmount(stack.Amount);
            }
            else
            {
                // No more items in this slot, remove the view
                var view = slotToView[slot];
                if (view != null) Destroy(view.gameObject);
                slotToView.Remove(slot);
            }
        }
    }

    private void HandleItemMoved(int sourceSlot, int targetSlot)
    {
        MoveViewDict(sourceSlot, targetSlot);
        UpdateView();
    }

    private void HandleItemSwapped(int sourceSlot, int targetSlot)
    {
        SwapViewDictEntries(sourceSlot, targetSlot);
        UpdateView();
    }


    public void IntegrateView(InventoryItemUI view)
    {
        if (view == null)
        {
            Debug.LogWarning("InventoryItemUI is null in IntegrateView!");
            return;
        }

        view.transform.SetParent(slotIndexToContainer[view.CurrentSlotIndex].transform);

        // Use indexer instead of Add to handle cases where key might already exist
        if (slotToView.ContainsKey(view.CurrentSlotIndex))
        {
            Debug.LogWarning(
                $"IntegrateView: Slot {view.CurrentSlotIndex} already exists in slotToView, replacing it.");
            Destroy(slotToView[view.CurrentSlotIndex].gameObject);
        }

        slotToView[view.CurrentSlotIndex] = view;

        view.transform.position = slotIndexToContainer[view.CurrentSlotIndex].transform.position;
    }

    public InventoryItemUI GetView(int slot)
    {
        return slotToView[slot];
    }

    public void MoveViewDict(int sourceSlot, int targetSlot)
    {
        if (!slotToView.ContainsKey(sourceSlot))
        {
            Debug.LogError($"MoveViewDict: Source slot {sourceSlot} not found in slotToView dictionary!");
            return;
        }

        slotToView[targetSlot] = slotToView[sourceSlot];
        slotToView.Remove(sourceSlot);
        slotToView[targetSlot].SetReferencedSlot(targetSlot);
    }

    public bool SwapViewDictEntries(int sourceSlot, int targetSlot)
    {
        if (!slotToView.ContainsKey(sourceSlot) || !slotToView.ContainsKey(targetSlot))
        {
            Debug.LogError(
                $"SwapViewDictEntries: Source slot {sourceSlot} or target slot {targetSlot} not found in slotToView dictionary!");
            return false;
        }

        slotToView[sourceSlot].SetReferencedSlot(targetSlot);
        slotToView[targetSlot].SetReferencedSlot(sourceSlot);
        return slotToView.SwapEntries(sourceSlot, targetSlot);
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
}