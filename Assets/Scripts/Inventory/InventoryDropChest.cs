using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Represents a chest that holds items dropped from a player's inventory (e.g., on death).
///     Players can interact with it to retrieve their items.
/// </summary>
public class InventoryDropChest : MonoBehaviour
{
    [SerializeField] private List<ItemStack> storedItems = new();
    private IInventoryManagement inventoryManager;
    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            Debug.LogError("InventoryDropChest requires an Interactable component!");
        }
    }

    private void Start()
    {
        // Get the player's inventory service
        if (ServiceLocator.Instance.IsRegistered<IInventoryManagement>())
        {
            inventoryManager = ServiceLocator.Instance.Get<IInventoryManagement>();
        }

        // Set up the interaction callback
        if (interactable != null)
        {
            interactable.OnInteract.AddListener(OnChestInteract);
        }

        UpdateInteractionMessage();
    }

    /// <summary>
    ///     Initialize the chest with items from a dumped inventory.
    /// </summary>
    public void Initialize(List<ItemStack> items)
    {
        storedItems = new List<ItemStack>(items);
        Debug.Log($"InventoryDropChest initialized with {storedItems.Count} item stacks");
        UpdateInteractionMessage();
    }

    private void OnChestInteract(GameObject interactor)
    {
        // Lazy initialization if service wasn't available at Start
        if (inventoryManager == null && ServiceLocator.Instance.IsRegistered<IInventoryManagement>())
        {
            inventoryManager = ServiceLocator.Instance.Get<IInventoryManagement>();
        }

        if (inventoryManager == null)
        {
            Debug.LogError("Player inventory not found!");
            return;
        }

        //IInventory inv = interactor.GetComponent<IInventory>();
        var inv = InventoryManager.Instance.PlayerInventory;
        if (inv == null)
        {
            Debug.Log("Your mysterious try to get the interactor didnt work! greetings from death chest");
            return;
        }

        // Transfer items to player inventory
        var itemsToRemove = new List<ItemStack>();
        foreach (var itemStack in storedItems)
        {
            var amountToAdd = itemStack.Amount;
            for (var i = 0; i < amountToAdd; i++)
            {
                if (inventoryManager.AddItem(inv,itemStack.ItemType))
                {
                    itemStack.Amount--;
                }
                else
                {
                    Debug.Log("Player inventory is full!");
                    break;
                }
            }

            // Mark empty stacks for removal
            if (itemStack.Amount <= 0)
            {
                itemsToRemove.Add(itemStack);
            }
        }

        // Remove empty stacks
        foreach (var emptyStack in itemsToRemove)
        {
            storedItems.Remove(emptyStack);
        }

        UpdateInteractionMessage();

        // Destroy chest if empty
        if (storedItems.Count == 0)
        {
            Debug.Log("InventoryDropChest is empty, destroying...");
            Destroy(gameObject);
        }
    }

    private void UpdateInteractionMessage()
    {
        if (interactable != null)
        {
            var itemCount = 0;
            foreach (var stack in storedItems)
            {
                itemCount += stack.Amount;
            }

            interactable.message = storedItems.Count > 0
                ? $"Retrieve Items ({itemCount} total)"
                : "Empty";
        }
    }

    /// <summary>
    ///     Get the current number of items in the chest.
    /// </summary>
    public int GetTotalItemCount()
    {
        var count = 0;
        foreach (var stack in storedItems)
        {
            count += stack.Amount;
        }
        return count;
    }
}
