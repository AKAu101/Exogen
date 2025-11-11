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
public class InventoryUI : MonoBehaviour, IUIStateManagement
{
    //References
    [SerializeField] private GameObject wrapper;
    [SerializeField] private GameObject slotRoot;
    public Transform SlotRoot => slotRoot.transform;
    [SerializeField] private GameObject itemViewPrefab;
    [SerializeField] private List<GameObject> slotObjects;

    //Properties
    private readonly Dictionary<int, GameObject> slotIndexToContainer = new();
    public Dictionary<int, GameObject> SlotIndexToContainer => slotIndexToContainer;
    private bool eventsSubscribed;
    private IInventory assignedInventory;
    public IInventory AssignedInventory => assignedInventory;
    [SerializeField] Inventory _Inventory;

    //References
    private IInventoryManagement inventoryManagement;
    public Dictionary<int, InventoryItemUI> slotToView = new();

    protected void Awake()
    {
        //base.Awake();
        for (var i = 0; i < slotObjects.Count; i++) slotIndexToContainer[i] = slotObjects[i];

        // Register this instance as the IUIStateManagement service
        ServiceLocator.Instance.Register<IUIStateManagement>(this);

        assignedInventory = _Inventory.GetComponent<IInventory>();

        InventoryUIHelper.Register(assignedInventory, this);
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
        foreach (var kvp in assignedInventory.SlotToStack)
        {
            var slot = kvp.Key;
            var stack = kvp.Value;
            HandleItemAdded(assignedInventory,stack.ItemType, slot);
        }
    }

    private void HandleItemAdded(IInventory inv,ItemSO itemType, int slot)
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

        if(inv != assignedInventory)
        {
            Debug.Log($"Not my({this}) Inventory!");
            return;
        }

        if (!slotToView.ContainsKey(slot))
        {
            var instance = Instantiate(itemViewPrefab);
            var view = instance.GetComponent<InventoryItemUI>();
            if (view != null)
            {
                var stack = inv.SlotToStack[slot];
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
            var view = slotToView[slot];
            var stack = inv.SlotToStack[slot];
            view.UpdateAmount(stack.Amount);
        }
    }

    private void HandleItemRemoved(IInventory inv,ItemSO itemType, int slot)
    {
        if (inventoryManagement == null) return;

        if (inv != assignedInventory)
        {
            Debug.Log("Not my Inventory!");
            return;
        }

        if (slotToView.ContainsKey(slot))
        {
            // Check if the slot still has items
            if (inv.SlotToStack.ContainsKey(slot))
            {
                // Update the count
                var view = slotToView[slot];
                var stack = inv.SlotToStack[slot];
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

    private void HandleItemMoved(IInventory invOne,int sourceSlot, IInventory invTwo, int targetSlot)
    {
        if (invOne != assignedInventory)
        {
            Debug.Log($"Not my{this} Inventory!");
            return;
        }
        else
        {
            Debug.Log($"Handling Item Moved! I am {this}");
        }

        MoveViewDict(invTwo, sourceSlot, invTwo,targetSlot);
        UpdateView();
    }

    private void HandleItemSwapped(IInventory invOne, int sourceSlot, IInventory invTwo,int targetSlot)
    {
        if (invOne != assignedInventory)
        {
            Debug.Log("Not my Inventory!");
            return;
        }

        InventoryUIHelper.SwapViewDictEntries(this,sourceSlot, InventoryUIHelper.GetUI(invTwo), targetSlot);
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

    public void MoveViewDict(IInventory inv,int sourceSlot, IInventory targetInv,int targetSlot)
    {
        var targetUI = InventoryUIHelper.GetUI(targetInv);
        InventoryUIHelper.MoveViewDict(this, sourceSlot, targetUI, targetSlot);
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