using System;
using System.Collections.Generic;

public interface IInventoryManagement
{
    // Properties
    Dictionary<int, ItemStack> SlotToStack { get; }

    // Methods
    bool AddItem(ItemSO itemType);
    bool RemoveItem(ItemSO itemType);
    bool RemoveItemFromSlot(int slot, int amount = 1);
    bool TryMoveItem(int sourceSlot, int targetSlot);

    // Events
    event Action<ItemSO, int> OnItemAdded;
    event Action<ItemSO, int> OnItemRemoved;
    event Action<int, int> OnItemMoved;
    event Action<int, int> OnItemSwapped;
}