using System;
using System.Collections.Generic;
using System.Linq;
using Generals;
using UnityEngine;

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
public class Inventory : Singleton<Inventory>, IInventoryManagement
{
    public int maxInventorySize = 20;
    public int maxStackSize = 99;
    [SerializeField] private GameObject inventoryDropChestPrefab;
    public Dictionary<int, ItemStack> slotToStack = new();

    protected override void Awake()
    {
        base.Awake();
        // Register this instance as the IInventoryManagement service
        ServiceLocator.Instance.Register<IInventoryManagement>(this);
    }

    // Interface property
    public Dictionary<int, ItemStack> SlotToStack => slotToStack;

    public event Action<ItemSO, int> OnItemAdded;
    public event Action<ItemSO, int> OnItemRemoved;
    public event Action<int, int> OnItemMoved;
    public event Action<int, int> OnItemSwapped;

    public bool AddItem(ItemSO itemType)
    {
        if (slotToStack.Count >= maxInventorySize)
        {
            Debug.Log("Inventory is full!");
            return false;
        }

        var slot = FindStackableSlot(itemType);

        if (slot != -1)
        {
            slotToStack[slot].Amount += 1;
        }
        else
        {
            var emptySlot = GetFirstEmptySlot();

            if (emptySlot >= maxInventorySize || emptySlot == -1) return false;

            var newStack = new ItemStack(itemType, 1);
            slotToStack.Add(emptySlot, newStack);
            slot = emptySlot;
        }

        if (OnItemAdded != null) OnItemAdded.Invoke(itemType, slot);
        Debug.Log($"Added {itemType.name} to inventory at slot {slot}. Stack amount: {slotToStack[slot].Amount}");
        return true;
    }

    public bool RemoveItem(ItemSO itemType)
    {
        var slot = FindSlotWithItem(itemType);

        if (slot != -1)
        {
            slotToStack[slot].Amount -= 1;

            if (slotToStack[slot].Amount <= 0) slotToStack.Remove(slot);
            if (OnItemRemoved != null) OnItemRemoved.Invoke(itemType, slot);
            Debug.Log($"Removed {itemType.name} from inventory. Items remaining: {slotToStack.Count}");
            return true;
        }

        Debug.Log($"{itemType.name} not found in inventory.");
        return false;
    }

    public bool RemoveItemFromSlot(int slot, int amount = 1)
    {
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

        if (OnItemRemoved != null) OnItemRemoved.Invoke(itemType, slot);

        return true;
    }

    public bool TryMoveItem(int sourceSlot, int targetSlot)
    {
        Debug.Log("TryMoveItem");

        if (!slotToStack.ContainsKey(sourceSlot))
        {
            Debug.LogError($"Source slot {sourceSlot} does not contain an item!");
            return false;
        }

        if (!slotToStack.ContainsKey(targetSlot))
        {
            var stack = slotToStack[sourceSlot];
            slotToStack.Add(targetSlot, stack);
            slotToStack.Remove(sourceSlot);

            if (OnItemMoved != null) OnItemMoved.Invoke(sourceSlot, targetSlot);
        }
        else
        {
            if (!slotToStack.SwapEntries(sourceSlot, targetSlot))
            {
                Debug.LogError("Failed to swap slotToStack dictionary entries");
                return false;
            }

            if (OnItemSwapped != null) OnItemSwapped.Invoke(sourceSlot, targetSlot);
        }

        return true;
    }

    private int FindStackableSlot(ItemSO itemType)
    {
        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType && kvp.Value.Amount < maxStackSize)
                return kvp.Key;

        return -1;
    }

    private int GetFirstEmptySlot()
    {
        for (var i = 0; i < maxInventorySize; i++)
            if (!slotToStack.ContainsKey(i))
                return i;

        return -1;
    }

    private int FindSlotWithItem(ItemSO itemType)
    {
        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType)
                return kvp.Key;

        return -1;
    }

    public List<ItemStack> GetStacks()
    {
        return slotToStack.Values.ToList();
    }


    public bool HasItem(ItemSO itemType)
    {
        return FindSlotWithItem(itemType) != -1;
    }

    public int GetItemCount()
    {
        return slotToStack.Count;
    }

    public void ClearInventory()
    {
        slotToStack.Clear();
        Debug.Log("Inventory cleared.");
    }

    public List<ItemStack> DumpInventory()
    {
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
            if (OnItemRemoved != null) OnItemRemoved.Invoke(itemType, slot);
        }

        Debug.Log($"Inventory dumped. {dumpedItems.Count} unique item stacks removed.");
        return dumpedItems;
    }

    /// <summary>
    ///     Spawns a drop chest at the specified position with all items from the inventory.
    ///     Clears the inventory and returns the spawned chest GameObject.
    /// </summary>
    public GameObject SpawnDropChest(Vector3 position)
    {
        if (inventoryDropChestPrefab == null)
        {
            Debug.LogError("InventoryDropChestPrefab is not assigned in Inventory! Cannot spawn drop chest.");
            return null;
        }

        // Dump the inventory and get all items
        var dumpedItems = DumpInventory();

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
    public GameObject SpawnDropChestAtPlayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player transform is null! Cannot spawn drop chest.");
            return null;
        }

        return SpawnDropChest(playerTransform.position);
    }
}