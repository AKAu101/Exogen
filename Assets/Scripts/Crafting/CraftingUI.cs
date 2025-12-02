using UnityEngine;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] InventoryUI inv;
    [SerializeField] ItemSlot outputSlot;

    private CraftingSystem craftingSystem;
    private IInventorySystem inventorySystem;

    void Start()
    {
        // Get services from ServiceLocator
        craftingSystem = ServiceLocator.Instance.Get<CraftingSystem>();
        inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();
    }

    public void CraftItem()
    {
        // Lazy initialization
        if (craftingSystem == null) craftingSystem = ServiceLocator.Instance.Get<CraftingSystem>();
        if (inventorySystem == null) inventorySystem = ServiceLocator.Instance.Get<IInventorySystem>();

        if (craftingSystem == null || inventorySystem == null)
        {
            Debug.LogError("CraftingSystem or InventorySystem not found!");
            return;
        }

        // Check if ingredient slots contain items (slots 0 and 1 are required, 2 and 3 are optional)
        if (!inv.AssignedInventory.SlotToStack.TryGetValue(0, out var first))
        {
            Debug.LogWarning("Crafting failed: Slot 0 is empty");
            return;
        }

        if (!inv.AssignedInventory.SlotToStack.TryGetValue(1, out var second))
        {
            Debug.LogWarning("Crafting failed: Slot 1 is empty");
            return;
        }

        // Slots 2 and 3 are optional - use null if empty
        inv.AssignedInventory.SlotToStack.TryGetValue(2, out var third);
        inv.AssignedInventory.SlotToStack.TryGetValue(3, out var fourth);

        // Check if output slot is empty
        if (inv.AssignedInventory.SlotToStack.ContainsKey(outputSlot.SlotIndex))
        {
            Debug.LogWarning("Crafting failed: Output slot is not empty. Remove items first.");
            return;
        }

        ItemData thirdItem = third?.ItemType;
        ItemData fourthItem = fourth?.ItemType;

        if(craftingSystem.TryGetRecipe(first.ItemType, second.ItemType, thirdItem, fourthItem, out var recipe))
        {
            Debug.Log("Crafting item: " + recipe.Result.name);

            inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 0, 1);
            inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 1, 1);

            // Only remove from slots 2 and 3 if they had items
            if (third != null)
                inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 2, 1);
            if (fourth != null)
                inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 3, 1);

            inventorySystem.ForceSetSlot(inv.AssignedInventory, outputSlot.SlotIndex, recipe.Result, 1);

            inv.UpdateView();
        }
        else
        {
            Debug.Log("Recipe doesn't exist. Give feedback to player");
        }
    }
}
