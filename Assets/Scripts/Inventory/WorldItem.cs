using UnityEngine;

/// <summary>
///     Represents a pickable item in the game world.
///     When picked up, adds the item to the player's inventory and destroys itself.
/// </summary>
public class WorldItem : MonoBehaviour
{
    [SerializeField] private ItemSO itemType;
    private IInventoryManagement inventoryManagement;

    private void Start()
    {
        // Get the inventory service from the ServiceLocator
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();
    }

    public void PickUp()
    {
        // Lazy initialization in case PickUp is called before Start
        if (inventoryManagement == null) inventoryManagement = ServiceLocator.Instance.Get<IInventoryManagement>();

        if (inventoryManagement != null)
        {
            inventoryManagement.AddItem(InventoryManager.Instance.PlayerInventory,itemType);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("IInventoryManagement service not found! Make sure Inventory is registered.");
        }
    }
}