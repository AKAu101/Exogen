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
        var first = inv.AssignedInventory.SlotToStack[0];
        var second = inv.AssignedInventory.SlotToStack[1];

        //Guards still missing!
        //e.g. is output slot empty? etc etc

        if(RecipeBook.Instance.TryGetRecipe(first.ItemType,second.ItemType, out var r))
        {
            Debug.Log("Should craft an Item now");

            InventoryManager.Instance.RemoveItemFromSlot(inv.AssignedInventory, 0, 1);
            InventoryManager.Instance.RemoveItemFromSlot(inv.AssignedInventory, 1, 1);
            InventoryManager.Instance.ForceSetSlot(inv.AssignedInventory,outputSlot.Slot, r.Result, 1);


            inv.UpdateView();

        }
        else
        {
            Debug.Log("Recipe doesnt exist. Give Feedback to palyer");
        }

        
    }

}
