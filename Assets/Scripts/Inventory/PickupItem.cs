using UnityEngine;

/// <summary>
///     Represents a pickable item in the game world.
///     When picked up, adds the item to the player's inventory and destroys itself.
/// </summary>
public class PickupItem : MonoBehaviour
{
    [SerializeField] private ItemData itemType;
    private IInventorySystem inventoryManagement;

    private void Start()
    {
        // Get the inventory service from the ServiceLocator
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();
    }

    public void PickUp()
    {
        // Lazy initialization in case PickUp is called before Start
        if (inventoryManagement == null) inventoryManagement = ServiceLocator.Instance.Get<IInventorySystem>();

        if (inventoryManagement != null)
        {
            // Get InventorySystem to access PlayerInventory
            var inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>() as InventorySystem;
            if (inventorySystem != null && inventorySystem.PlayerInventory != null)
            {
                inventoryManagement.AddItem(inventorySystem.PlayerInventory, itemType);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("Player inventory not found!");
            }
        }
        else
        {
            Debug.LogError("IInventorySystem service not found! Make sure Inventory is registered.");
        }
    }
}