using System;
using System.Collections.Generic;

/// <summary>
///     Core inventory system interface managing item operations across multiple inventories.
///     Handles adding, removing, moving items with event notifications.
/// </summary>
public interface IInventorySystem
{
    // Properties
    Dictionary<int, ItemStack> SlotToStack { get; }

    // Methods - Player Inventory shortcuts
    bool AddItem(ItemData itemType);
    bool RemoveItem(ItemData itemType);
    bool RemoveItemFromSlot(int slot, int amount = 1);
    bool TryMoveItem(int sourceSlot, int targetSlot);

    // Methods - Multi-Inventory support
    bool AddItem(IInventoryData inv, ItemData itemType);
    bool RemoveItem(IInventoryData inv, ItemData itemType);
    bool RemoveItemFromSlot(IInventoryData inv, int slot, int amount = 1);
    bool TryMoveItem(IInventoryData invOne, int sourceSlot, IInventoryData invTwo, int targetSlot);
    void ForceSetSlot(IInventoryData inv, int slot, ItemData itemType, int amount);
    void DropItem(IInventoryData inv, int slot);

    // Events
    event Action<IInventoryData, ItemData, int> OnItemAdded;
    event Action<IInventoryData, ItemData, int> OnItemRemoved;
    event Action<IInventoryData, int, IInventoryData, int> OnItemMoved;
    event Action<IInventoryData, int, IInventoryData, int> OnItemSwapped;
}