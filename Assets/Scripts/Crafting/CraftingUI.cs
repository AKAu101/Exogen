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

        // Check if ingredient slots contain items
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

        // Check if output slot is empty
        if (inv.AssignedInventory.SlotToStack.ContainsKey(outputSlot.SlotIndex))
        {
            Debug.LogWarning("Crafting failed: Output slot is not empty. Remove items first.");
            return;
        }

        if(craftingSystem.TryGetRecipe(first.ItemType, second.ItemType, out var recipe))
        {
            Debug.Log("Crafting item: " + recipe.Result.name);

            inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 0, 1);
            inventorySystem.RemoveItemFromSlot(inv.AssignedInventory, 1, 1);
            inventorySystem.ForceSetSlot(inv.AssignedInventory, outputSlot.SlotIndex, recipe.Result, 1);

            inv.UpdateView();
        }
        else
        {
            Debug.Log("Recipe doesn't exist. Give feedback to player");
        }
    }
}
