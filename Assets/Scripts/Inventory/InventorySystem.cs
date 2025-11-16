using Generals;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///     Represents a stack of items in the inventory with a type and quantity.
/// </summary>
public class ItemStack
{
    public ItemStack(ItemData itemType, int amount)
    {
        ItemType = itemType;
        Amount = amount;
    }

    public ItemData ItemType { get; set; }
    public int Amount { get; set; }
}

/// <summary>
///     Core inventory system managing item storage, stacking, and movement.
///     Handles adding, removing, and organizing items across slots with event notifications.
/// </summary>
public class InventorySystem : Singleton<InventorySystem>, IInventorySystem
{
    public int maxInventorySize = 20;
    public int maxStackSize = 99;
    [SerializeField] private GameObject inventoryDropChestPrefab;

    [SerializeField] InventoryData playerInventory;
    public InventoryData PlayerInventory => playerInventory;


    //AUA AUA FALSCHER PLATZ
    [SerializeField] public LayerMask slotAreaLayer;
    [SerializeField] public LayerMask dropAreaLayer;

    protected override void Awake()
    {
        base.Awake();
        // Register this instance as the IInventorySystem service
        ServiceLocator.Instance.Register<IInventorySystem>(this);
    }

    // Interface property - returns player inventory's SlotToStack
    public Dictionary<int, ItemStack> SlotToStack => (playerInventory as IInventoryData)?.SlotToStack;

    public event Action<IInventoryData,ItemData, int> OnItemAdded;
    public event Action<IInventoryData,ItemData, int> OnItemRemoved;
    public event Action<IInventoryData,int, IInventoryData,int> OnItemMoved;
    public event Action<IInventoryData,int, IInventoryData,int> OnItemSwapped;

    // Interface methods without IInventoryData parameter - operate on playerInventory
    public bool AddItem(ItemData itemType)
    {
        return AddItem(playerInventory, itemType);
    }

    public bool RemoveItem(ItemData itemType)
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

    // Interface methods with IInventoryData parameter
    public bool AddItem(IInventoryData inv, ItemData itemType)
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

    public bool RemoveItem(IInventoryData inv,ItemData itemType)
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

    public bool RemoveItemFromSlot(IInventoryData inv,int slot, int amount = 1)
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
        Debug.Log($"Removed {amount}x {itemType.name} from slot {slot}. Amount remaining: {stack.Amount}");

        if (stack.Amount <= 0) slotToStack.Remove(slot);

        if (OnItemRemoved != null) OnItemRemoved.Invoke(inv, itemType, slot);

        return true;
    }

    public void DropItem(IInventoryData inv, int slot)
    {
        Debug.Log("Trying to drop item");

        // Check if slot contains an item
        if (!inv.SlotToStack.TryGetValue(slot, out var stack))
        {
            Debug.LogWarning($"Cannot drop item: Slot {slot} is empty");
            return;
        }

        var currentItemData = stack.ItemType;

        // Check if item data is valid
        if (currentItemData == null)
        {
            Debug.LogError($"Cannot drop item: ItemType in slot {slot} is null");
            return;
        }

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
        InventoryUI.GetUI(inv).UpdateView();
    }

    public void ForceSetSlot(IInventoryData inv, int slot, ItemData itemType, int amount)
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

    public bool TryMoveItem(IInventoryData invOne,int sourceSlot, IInventoryData invTwo,int targetSlot)
    {
        Debug.Log("TryMoveItem");
        Debug.Log($"InvOne: {invOne}  -  InvTwo: {invTwo}");


        var firstSlotToStack = invOne.SlotToStack;
        var secondSlotToStack = invTwo.SlotToStack;

        if (firstSlotToStack == secondSlotToStack && sourceSlot == targetSlot)
        {
            InventoryUI.GetUI(invOne).UpdateView();
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

            if (OnItemSwapped != null) OnItemSwapped.Invoke(invOne, sourceSlot, invTwo, targetSlot);

            return true;
        }
    }

    private int FindStackableSlot(IInventoryData inv,ItemData itemType)
    {
        var slotToStack = inv.SlotToStack;

        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType && kvp.Value.Amount < maxStackSize)
                return kvp.Key;

        return -1;
    }

    private int GetFirstEmptySlot(IInventoryData inv)
    {
        var slotToStack = inv.SlotToStack;

        for (var i = 0; i < maxInventorySize; i++)
            if (!slotToStack.ContainsKey(i))
                return i;

        return -1;
    }

    private int FindSlotWithItem(IInventoryData inv,ItemData itemType)
    {
        var slotToStack = inv.SlotToStack;

        foreach (var kvp in slotToStack)
            if (kvp.Value.ItemType == itemType)
                return kvp.Key;

        return -1;
    }

    public List<ItemStack> GetStacks(IInventoryData inv)
    {
        var slotToStack = inv.SlotToStack;
        return slotToStack.Values.ToList();
    }


    public bool HasItem(IInventoryData inv,ItemData itemType)
    {
        return FindSlotWithItem(inv, itemType) != -1;
    }

    public int GetItemCount(IInventoryData inv)
    {
        var slotToStack = inv.SlotToStack;
        return slotToStack.Count;
    }

    public void ClearInventory(IInventoryData inv)
    {
        var slotToStack = inv.SlotToStack;
        slotToStack.Clear();
        Debug.Log("Inventory cleared.");
    }

    public List<ItemStack> DumpInventory(IInventoryData inv)
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
    public GameObject SpawnDropChest(IInventoryData inv,Vector3 position)
    {
        if (inventoryDropChestPrefab == null)
        {
            Debug.LogError("DeathChestPrefab is not assigned in Inventory! Cannot spawn drop chest.");
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
        var chest = chestObject.GetComponent<DeathChest>();
        if (chest != null)
        {
            chest.Initialize(dumpedItems);
            Debug.Log($"Spawned drop chest at {position} with {dumpedItems.Count} item stacks");
        }
        else
        {
            Debug.LogError("Spawned chest prefab does not have DeathChest component!");
            Destroy(chestObject);
            return null;
        }

        return chestObject;
    }

    /// <summary>
    ///     Convenience method to spawn drop chest at player's current position.
    ///     Typically called on player death.
    /// </summary>
    public GameObject SpawnDropChestAtPlayer(IInventoryData inv,Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player transform is null! Cannot spawn drop chest.");
            return null;
        }

        return SpawnDropChest(inv,playerTransform.position);
    }
}