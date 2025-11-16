using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple inventory data container implementing IInventoryData.
///     Can be attached to Player, Chests, or any other inventory-holding entity.
/// </summary>
public class InventoryData : MonoBehaviour, IInventoryData
{
    Dictionary<int, ItemStack> slotToStack = new Dictionary<int, ItemStack>();

    // IInventoryData interface
    public Dictionary<int, ItemStack> SlotToStack => slotToStack;
}
