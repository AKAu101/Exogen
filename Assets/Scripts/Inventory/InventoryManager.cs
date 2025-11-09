using Generals;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

/// <summary>
///     Represents a stack of items in the inventory with a type and quantity.
/// </summary>
public class ItemStack
{
    public ItemStack(ItemSO itemType, int amount)
    {
        ItemType = itemType;
        Amount = amount;
    }

    public ItemSO ItemType { get; set; }
    public int Amount { get; set; }
}

/// <summary>
///     Core inventory system managing item storage, stacking, and movement.
///     Handles adding, removing, and organizing items across slots with event notifications.
/// </summary>
public class InventoryManager : Singleton<InventoryManager>, IInventoryManagement
{
    public int maxInventorySize = 20;
    public int maxStackSize = 99;
    [SerializeField] private GameObject inventoryDropChestPrefab;

    [SerializeField] Inventory playerInventory;
    public Inventory PlayerInventory => playerInventory;

    protected override void Awake()
    {
        base.Awake();
        // Register this instance as the IInventoryManagement service
        ServiceLocator.Instance.Register<IInventoryManagement>(this);
    }

    // Interface property - returns player inventory's SlotToStack
    public Dictionary<int, ItemStack> SlotToStack => (playerInventory as IInventory)?.SlotToStack;

    public event Action<IInventory,ItemSO, int> OnItemAdded;
    public event Action<IInventory,ItemSO, int> OnItemRemoved;
    public event Action<IInventory,int, IInventory,int> OnItemMoved;
    public event Action<IInventory,int, IInventory,int> OnItemSwapped;

    // Interface methods without IInventory parameter - operate on playerInventory
    public bool AddItem(ItemSO itemType)
    {
        return AddItem(playerInventory, itemType);
    }

    public bool RemoveItem(ItemSO itemType)
    {
        return RemoveItem(playerInventory, itemType);
    }

    public bool RemoveItemFromSlot(int slot, int amount = 1)
    {
        return RemoveItemFromSlot(playerInventory, slot, amount);
    }

    public bool TryMoveItem(int sourceSlot, int targetSlot)
    {
        return TryMoveItem(playerInventory, sourceSlot, playerInventory, targetSlot);
    }

    // Interface methods with IInventory parameter
    public bool AddItem(IInventory inv, ItemSO itemType)
    {
        var slotToStack = inv.SlotToStack;
        
        if (slotToStack.Count >= maxInventorySize)
        {
            Debug.Log("Inventory is full!");
            return false;
        }

        var slot = FindStackableSlot(inv, itemType);

        if (slot != -1)
        {
            slotToStack[slot].Amount += 1;
        }
        else
        {
            var emptySlot = GetFirstEmptySlot(inv);

            if (emptySlot >= maxInventorySize || emptySlot == -1) return false;

            var newStack = new ItemStack(itemType, 1);
            slotToStack.Add(emptySlot, newStack);
            slot = emptySlot;
        }

        if (OnItemAdded != null) OnItemAdded.Invoke(inv,itemType, slot);
        Debug.Log($"Added {itemType.name} to inventory at slot {slot}. Stack amount: {slotToStack[slot].Amount}");
        return true;
    }

    public bool RemoveItem(IInventory inv,ItemSO itemType)
    {
        var slotToStack = inv.SlotToStack;
        var slot = FindSlotWithItem(inv, itemType);

        if (slot != -1)
        {
            slotToStack[slot].Amount -= 1;

            if (slotToStack[slot].Amount <= 0) slotToStack.Remove(slot);
            if (OnItemRemoved != null) OnItemRemoved.Invoke(inv, itemType, slot);
            Debug.Log($"Removed {itemType.name} from inventory. Items remaining: {slotToStack.Count}");
            return true;
        }

        Debug.Log($"{itemType.name} not found in inventory.");
        return false;
    }

    public bool RemoveItemFromSlot(IInventory inv,int slot, int amount = 1)
    {
        var slotToStack = inv.SlotToStack;

        if (!slotToStack.ContainsKey(slot))
        {
            Debug.LogWarning($"No item found in slot {slot}");
            return false;
        }

        var stack = slotToStack[slot];
        var itemType = stack.ItemType;

        stack.Amount -= amount;
        Debug.Log($"Consumed {itemType.name} from slot {slot}. Amount remaining: {stack.Amount}");

        if (stack.Amount <= 0) slotToStack.Remove(slot);

        if (OnItemRemoved != null) OnItemRemoved.Invoke(inv, itemType, slot);

        return true;
    }

    public void DropItem(IInventory inv, int slot)
    {
        Debug.Log("Trying to drop item");
        var currentItemData = inv.SlotToStack[slot].ItemType;

        // Spawn in world
        if (currentItemData.itemPrefab != null && Camera.main != null)
        {
            var dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            Instantiate(currentItemData.itemPrefab, dropPosition, Quaternion.identity);
            RemoveItemFromSlot(inv, slot); //remove from inv after dropping in world
            Debug.Log($"Dropped {currentItemData.name} at position {dropPosition}");
        }
        else
        {
            Debug.LogWarning("Item prefab or camera not found!");
        }
        InventoryUIHelper.GetUI(inv).UpdateView();
    }

    public void ForceSetSlot(IInventory inv, int slot, ItemSO itemType, int amount)
    {
        var slotToStack = inv.SlotToStack;

        if (amount <= 0)
        {
            // clearing the slot
            if (slotToStack.TryGetValue(slot, out var old))
            {
                slotToStack.Remove(slot);
                OnItemRemoved?.Invoke(inv, old.ItemType, slot);
            }
            return;
        }

        // set/overwrite the slot first
        if (slotToStack.TryGetValue(slot, out var stack))
        {
            stack.ItemType = itemType;
            stack.Amount = amount;
        }
        else
        {
            slotToStack[slot] = new ItemStack(itemType, amount);
        }

        // now notify
        OnItemAdded?.Invoke(inv, itemType, slot);
    }

    public bool TryMoveItem(IInventory invOne,int sourceSlot, IInventory invTwo,int targetSlot)
    {
        Debug.Log("TryMoveItem");
        Debug.Log($"InvOne: {invOne}  -  InvTwo: {invTwo}");


        var firstSlotToStack = invOne.SlotToStack;
        var secondSlotToStack = invTwo.SlotToStack;

        if (firstSlotToStack == secondSlotToStack && sourceSlot == targetSlot)
        {
            InventoryUIHelper.GetUI(invOne).UpdateView();
            return true;
        }

        if (!firstSlotToStack.ContainsKey(sourceSlot))
        {
            Debug.LogError($"Source slot {sourceSlot} does not contain an item!");
            return false;
        }

        if (!secondSlotToStack.ContainsKey(targetSlot))
        {
            var stack = firstSlotToStack[sourceSlot];
            secondSlotToStack.Add(targetSlot, stack);
            firstSlotToStack.Remove(sourceSlot);

            if (OnItemMoved != null) OnItemMoved.Invoke(invOne, sourceSlot, invTwo, targetSlot);

            return true;
        }
        else
        {
            //there is an item at secondSlot so look if they are the same item type, then combine the amount, if it exceeds put rest to new slot if no empty slot leave the rest in the old slot
            var sourceStack = firstSlotToStack[sourceSlot];
            var targetStack = secondSlotToStack[targetSlot];

            if (sourceStack.ItemType == targetStack.ItemType)
            {
                int spaceLeft = maxStackSize - targetStack.Amount;

                if (spaceLeft > 0)
                {
                    // move as much as we can into the target stack
                    int toMove = Math.Min(spaceLeft, sourceStack.Amount);
                    targetStack.Amount += toMove;
                    sourceStack.Amount -= toMove;

                    OnItemMoved?.Invoke(invOne, sourceSlot, invTwo, targetSlot);

                    if (sourceStack.Amount <= 0)
                    {
                        firstSlotToStack.Remove(sourceSlot);
                    }
                    else if (invOne != invTwo)
                    {
                        // still have leftovers; try to place the remainder in a new slot
                        int empty = GetFirstEmptySlot(invTwo);
                        if (empty != -1)
                        {
                            secondSlotToStack.Add(empty, new ItemStack(sourceStack.ItemType, sourceStack.Amount));
                            firstSlotToStack.Remove(sourceSlot);
                            OnItemMoved?.Invoke(invOne, sourceSlot, invTwo, empty);
                        }
                        // if no empty slot, leave the remainder in the original source slot
                    }
                    return true; // handled by combining, don't swap
                }
                // if no spaceLeft, fall through to swap logic below
            }
            // different item types (or no space to combine) � allow the existing swap code below to run
        }
        if (firstSlotToStack == secondSlotToStack)
        {
            {
                if (!firstSlotToStack.SwapEntries(sourceSlot, targetSlot))
                {
                    Debug.LogError("Failed to swap slotToStack dictionary entries");
                    return false;
                }

                if (OnItemSwapped != null) OnItemSwapped.Invoke(invOne, sourceSlot, invTwo, targetSlot);
            }

            return true;
        }
        else
        {
            //swapping across two different inventories
            if (!firstSlotToStack.ContainsKey(sourceSlot) || !secondSlotToStack.ContainsKey(targetSlot)) return false;

            // If trying to swap the same key, do nothing
            //if (EqualityComparer<TKey>.Default.Equals(first, second)) return true;

            var entryAtSecond = secondSlotToStack[targetSlot];
            var entryAtFirst = firstSlotToStack[sourceSlot];

            firstSlotToStack.Remove(sourceSlot);
            secondSlotToStack.Remove(targetSlot);
            firstSlotToStack.Add(sourceSlot, entryAtSecond);
            secondSlotToStack.Add(targetSlot, entryAtFirst);

            return true;
        }
    }

    private int FindStackableSlot(IInventory inv,ItemSO itemType)
    {
        var slotToStack = inv.SlotToStack;

        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType && kvp.Value.Amount < maxStackSize)
                return kvp.Key;

        return -1;
    }

    private int GetFirstEmptySlot(IInventory inv)
    {
        var slotToStack = inv.SlotToStack;

        for (var i = 0; i < maxInventorySize; i++)
            if (!slotToStack.ContainsKey(i))
                return i;

        return -1;
    }

    private int FindSlotWithItem(IInventory inv,ItemSO itemType)
    {
        var slotToStack = inv.SlotToStack;

        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType)
                return kvp.Key;

        return -1;
    }

    public List<ItemStack> GetStacks(IInventory inv)
    {
        var slotToStack = inv.SlotToStack;
        return slotToStack.Values.ToList();
    }


    public bool HasItem(IInventory inv,ItemSO itemType)
    {
        return FindSlotWithItem(inv, itemType) != -1;
    }

    public int GetItemCount(IInventory inv)
    {
        var slotToStack = inv.SlotToStack;
        return slotToStack.Count;
    }

    public void ClearInventory(IInventory inv)
    {
        var slotToStack = inv.SlotToStack;
        slotToStack.Clear();
        Debug.Log("Inventory cleared.");
    }

    public List<ItemStack> DumpInventory(IInventory inv)
    {
        var slotToStack = inv.SlotToStack;

        // Create a copy of all items in the inventory
        var dumpedItems = new List<ItemStack>();
        foreach (var kvp in slotToStack)
        {
            dumpedItems.Add(new ItemStack(kvp.Value.ItemType, kvp.Value.Amount));
        }

        // Clear the inventory and notify listeners
        var slotsToRemove = slotToStack.Keys.ToList();
        foreach (var slot in slotsToRemove)
        {
            var itemType = slotToStack[slot].ItemType;
            slotToStack.Remove(slot);
            if (OnItemRemoved != null) OnItemRemoved.Invoke(inv, itemType, slot);
        }

        Debug.Log($"Inventory dumped. {dumpedItems.Count} unique item stacks removed.");
        return dumpedItems;
    }

    /// <summary>
    ///     Spawns a drop chest at the specified position with all items from the inventory.
    ///     Clears the inventory and returns the spawned chest GameObject.
    /// </summary>
    public GameObject SpawnDropChest(IInventory inv,Vector3 position)
    {
        if (inventoryDropChestPrefab == null)
        {
            Debug.LogError("InventoryDropChestPrefab is not assigned in Inventory! Cannot spawn drop chest.");
            return null;
        }

        // Dump the inventory and get all items
        var dumpedItems = DumpInventory(inv);

        // Don't spawn chest if inventory is empty
        if (dumpedItems.Count == 0)
        {
            Debug.Log("Inventory is empty, no drop chest spawned.");
            return null;
        }

        // Spawn the chest at the specified position
        var chestObject = Instantiate(inventoryDropChestPrefab, position, Quaternion.identity);

        // Initialize the chest with the dumped items
        var chest = chestObject.GetComponent<InventoryDropChest>();
        if (chest != null)
        {
            chest.Initialize(dumpedItems);
            Debug.Log($"Spawned drop chest at {position} with {dumpedItems.Count} item stacks");
        }
        else
        {
            Debug.LogError("Spawned chest prefab does not have InventoryDropChest component!");
            Destroy(chestObject);
            return null;
        }

        return chestObject;
    }

    /// <summary>
    ///     Convenience method to spawn drop chest at player's current position.
    ///     Typically called on player death.
    /// </summary>
    public GameObject SpawnDropChestAtPlayer(IInventory inv,Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player transform is null! Cannot spawn drop chest.");
            return null;
        }

        return SpawnDropChest(inv,playerTransform.position);
    }
}