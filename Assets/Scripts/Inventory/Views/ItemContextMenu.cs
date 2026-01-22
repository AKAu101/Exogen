using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Context menu for inventory items, allowing actions like consume and drop.
/// Appears on right-click and handles user item interactions.
/// </summary>
public class ItemContextMenu : MonoBehaviour
{
    //References
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button consumeButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private LayerMask slotAreaLayer;
    private ItemData currentItemData;

    //Class Reference
    private ItemSlot currentItemView;

    //Properties
    private int currentSlotIndex;
    private IInventorySystem inventoryManagement;
    private PlayerConsumptionManager consumptionManager; // Added reference

    private void Awake()
    {
        // Hide menu by default
        if (menuPanel != null) menuPanel.SetActive(false);

        // Setup button listeners
        if (consumeButton != null) consumeButton.onClick.AddListener(OnConsumeClicked);
        if (dropButton != null) dropButton.onClick.AddListener(OnDropClicked);
    }

    private void Start()
    {
        // Get the inventory service from the ServiceLocator
        inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();
        
        // Find consumption manager in the scene
        consumptionManager = FindObjectOfType<PlayerConsumptionManager>();
        if (consumptionManager == null)
        {
            Debug.LogError("ItemContextMenu: Could not find PlayerConsumptionManager in scene!");
        }
    }

    private void Update()
    {
        // Close menu if clicking outside of it (using new Input System)
        if (menuPanel.activeSelf && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            if (!IsPointerOverMenu())
                HideMenu();
    }

    private bool IsPointerOverMenu()
    {
        if (EventSystem.current == null || menuPanel == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
            if (result.gameObject == menuPanel || result.gameObject.transform.IsChildOf(menuPanel.transform))
                return true;

        return false;
    }

    public void ShowMenu(Vector3 position, int slotIndex, ItemSlot itemView, ItemData itemData)
    {
        if (menuPanel != null)
        {
            currentSlotIndex = slotIndex;
            currentItemView = itemView;
            currentItemData = itemData;

            // Show/hide consume button based on isConsumable
            bool canConsume = itemData != null && itemData.isConsumable;
            if (consumeButton != null) 
            {
                consumeButton.gameObject.SetActive(canConsume);
                
                // Update button text based on item type
                if (canConsume)
                {
                    // Check what type of consumable it is
                    string consumeText = "Consume";
                    if (itemData.healthRestore > 0)
                        consumeText = "Eat";
                    else if (itemData.oxygenRestore > 0)
                        consumeText = "Drink";
                    else if (itemData.staminaRestore > 0)
                        consumeText = "Use";
                        
                    var text = consumeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = consumeText;
                }
            }

            // Position the menu at the cursor
            menuPanel.transform.position = position;
            menuPanel.SetActive(true);
        }
    }

    public void HideMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            currentSlotIndex = -1;
            currentItemView = null;
            currentItemData = null;
        }
    }

    private void OnConsumeClicked()
    {
        if (inventoryManagement != null && currentItemData != null && currentSlotIndex >= 0)
        {
            if (currentItemData.isConsumable)
            {
                // Try to consume the item
                if (consumptionManager != null && consumptionManager.TryConsumeItem(currentItemData))
                {
                    // Only remove from inventory if consumption started successfully
                    var inv = currentItemView.InventoryUI.AssignedInventory;
                    inventoryManagement.RemoveItemFromSlot(inv, currentSlotIndex);
                    DebugManager.Log($"Consumed {currentItemData.name} from slot {currentSlotIndex}");
                }
                else
                {
                    DebugManager.LogWarning($"Could not consume {currentItemData.name}!");
                }
            }
            else
            {
                DebugManager.LogWarning($"Item {currentItemData.name} is not consumable!");
            }
        }

        HideMenu();
    }

    private void OnDropClicked()
    {
        if (inventoryManagement != null && currentItemData != null && currentSlotIndex >= 0)
        {
            // Remove from inventory
            var slot = currentItemView.CurrentSlotIndex;

            Physics.Raycast(currentItemView.transform.position + Vector3.back, transform.forward, out var hit, 100f, slotAreaLayer);
            var slotView = hit.transform.gameObject.GetComponent<ItemSlot>();
            if(slotView == null || slotView.InventoryUI == null)
            {
                DebugManager.LogError("Could not find inventory corresponding to item you are trying to drop");
                return;
            }

            inventoryManagement.DropItem(slotView.InventoryUI.AssignedInventory, slot);
        }

        HideMenu();
    }
}