using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Context menu for inventory items, allowing actions like consume and drop.
/// Appears on right-click and handles user item interactions.
/// </summary>
public class InventoryItemContextMenu : MonoBehaviour
{
    //References
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button consumeButton;
    [SerializeField] private Button dropButton;
    private ItemSO currentItemData;

    //Class Reference
    private InventoryItemUI currentItemView;

    //Properties
    private int currentSlotIndex;
    private IInventoryManagement inventoryManagement;

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
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();
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

    public void ShowMenu(Vector3 position, int slotIndex, InventoryItemUI itemView, ItemSO itemData)
    {
        if (menuPanel != null)
        {
            currentSlotIndex = slotIndex;
            currentItemView = itemView;
            currentItemData = itemData;

            // Show/hide consume button based on isConsumable
            if (consumeButton != null) consumeButton.gameObject.SetActive(itemData != null && itemData.isConsumable);

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
                inventoryManagement.RemoveItemFromSlot(currentSlotIndex);
                Debug.Log($"Consumed {currentItemData.name} from slot {currentSlotIndex}");
            }
            else
            {
                Debug.LogWarning($"Item {currentItemData.name} is not consumable!");
            }
        }

        HideMenu();
    }

    private void OnDropClicked()
    {
        if (inventoryManagement != null && currentItemData != null && currentSlotIndex >= 0)
        {
            // Remove from inventory
            inventoryManagement.RemoveItemFromSlot(currentSlotIndex);

            // Spawn in world
            if (currentItemData.itemPrefab != null && Camera.main != null)
            {
                var dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                Instantiate(currentItemData.itemPrefab, dropPosition, Quaternion.identity);
                Debug.Log($"Dropped {currentItemData.name} at position {dropPosition}");
            }
            else
            {
                Debug.LogWarning("Item prefab or camera not found!");
            }
        }

        HideMenu();
    }
}