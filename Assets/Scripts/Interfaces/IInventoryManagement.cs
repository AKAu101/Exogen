using NUnit;
using System;
using System.Collections.Generic;

public interface IInventoryManagement
{
    // Properties
    //Dictionary<int, ItemStack> SlotToStack { get; }

    // Methods
    bool AddItem(IInventory inv,ItemSO itemType);
    bool RemoveItem(IInventory inv, ItemSO itemType);
    bool RemoveItemFromSlot(IInventory inv, int slot, int amount = 1);
    bool TryMoveItem(IInventory invOne, int sourceSlot, IInventory invTwo,int targetSlot);

    // Events
    event Action<IInventory,ItemSO, int> OnItemAdded;
    event Action<IInventory,ItemSO, int> OnItemRemoved;
    //Moved item from inventory one in slot x to inventory two in slot y
    event Action<IInventory,int, IInventory,int> OnItemMoved;
    //swapped item from inventory one in slot x to inventory two in slot y
    event Action<IInventory, int, IInventory,int> OnItemSwapped;
}