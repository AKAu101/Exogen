using System.Collections;
using UnityEngine;

public class CraftingStation : MonoBehaviour
{
    [SerializeField] InventoryUI inv;
    [SerializeField] SlotView outputSlot;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CraftItem()
    {
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
        if (inv.AssignedInventory.SlotToStack.ContainsKey(outputSlot.Slot))
        {
            Debug.LogWarning("Crafting failed: Output slot is not empty. Remove items first.");
            return;
        }
        
        //Check if Item in Slot is the same as the crafted item, if so add amount to item in craft output

        if(RecipeBook.Instance.TryGetRecipe(first.ItemType, second.ItemType, out var recipe))
        {
            Debug.Log("Crafting item: " + recipe.Result.name);

            InventoryManager.Instance.RemoveItemFromSlot(inv.AssignedInventory, 0, 1);
            InventoryManager.Instance.RemoveItemFromSlot(inv.AssignedInventory, 1, 1);
            InventoryManager.Instance.ForceSetSlot(inv.AssignedInventory, outputSlot.Slot, recipe.Result, 1); //if item slot not empty increase item amount

            inv.UpdateView();
        }
        else
        {
            Debug.Log("Recipe doesn't exist. Give feedback to player");
        }
    }

}
